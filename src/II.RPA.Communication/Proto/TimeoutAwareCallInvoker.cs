using Grpc.Core;
using II.RPA.Communication.Timeout;

namespace II.RPA.Communication.Proto;

public sealed class TimeoutAwareCallInvoker : CallInvoker
{
    /// <summary>
    /// CancellationTokenSource.CancelAfter 能接受的最大延时（约 49.7 天）。
    /// 超过该值的超时无法用定时器表达，只能按“无限超时”处理。
    /// </summary>
    private static readonly TimeSpan MaxSupportedCancelAfter = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

    private readonly CallInvoker _inner;
    private readonly TimeoutResolver _timeoutResolver;

    public TimeoutAwareCallInvoker(CallInvoker inner, TimeoutResolver timeoutResolver)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _timeoutResolver = timeoutResolver ?? throw new ArgumentNullException(nameof(timeoutResolver));
    }

    public override TResponse BlockingUnaryCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method,
        string? host,
        CallOptions options,
        TRequest request)
    {
        var timeout = ResolveTimeout(options);
        using var timeoutCts = CreateTimeoutCancellationTokenSource(options, timeout);
        var effectiveOptions = ApplyDeadlineAndCancellation(options, timeout, timeoutCts.Token);
        var callTask = Task.Run(
            () => _inner.BlockingUnaryCall(method, host, effectiveOptions, request),
            CancellationToken.None);

        try
        {
            return callTask.WaitAsync(timeout, options.CancellationToken).GetAwaiter().GetResult();
        }
        catch (TimeoutException exception)
        {
            throw CreateDeadlineExceeded(timeout, exception);
        }
        catch (OperationCanceledException exception) when (timeoutCts.IsCancellationRequested &&
                                                           !options.CancellationToken.IsCancellationRequested)
        {
            throw CreateDeadlineExceeded(timeout, exception);
        }
        catch (RpcException exception) when (IsOwnTimeout(exception, timeoutCts, options.CancellationToken))
        {
            // The transport can surface our own deadline as a Cancelled (or DeadlineExceeded) status
            // before WaitAsync's timer gets a chance to fire. Both flavours mean the same thing, so
            // they are reported as the single timeout shape produced by CreateDeadlineExceeded.
            throw CreateDeadlineExceeded(timeout, exception);
        }
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method,
        string? host,
        CallOptions options,
        TRequest request)
    {
        var timeout = ResolveTimeout(options);
        var timeoutCts = CreateTimeoutCancellationTokenSource(options, timeout);
        var effectiveOptions = ApplyDeadlineAndCancellation(options, timeout, timeoutCts.Token);
        var call = _inner.AsyncUnaryCall(method, host, effectiveOptions, request);
        var responseAsync = AwaitUnaryResponseAsync(call, timeout, timeoutCts, options.CancellationToken);

        return new AsyncUnaryCall<TResponse>(
            responseAsync,
            call.ResponseHeadersAsync,
            call.GetStatus,
            call.GetTrailers,
            () =>
            {
                call.Dispose();
                timeoutCts.Dispose();
            });
    }

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method,
        string? host,
        CallOptions options,
        TRequest request)
    {
        return _inner.AsyncServerStreamingCall(method, host, options, request);
    }

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method,
        string? host,
        CallOptions options)
    {
        return _inner.AsyncClientStreamingCall<TRequest, TResponse>(method, host, options);
    }

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method,
        string? host,
        CallOptions options)
    {
        return _inner.AsyncDuplexStreamingCall<TRequest, TResponse>(method, host, options);
    }

    private TimeSpan ResolveTimeout(CallOptions options)
    {
        if (!options.Deadline.HasValue)
        {
            return _timeoutResolver.Resolve(null);
        }

        // DateTime.MaxValue 是 Grpc.Core 约定的“无限超时”（Timespec.InfFuture），
        // 不能换算成约 9999 年的 TimeSpan 交给 CancelAfter，否则会抛 ArgumentOutOfRangeException。
        if (options.Deadline.Value == DateTime.MaxValue)
        {
            return System.Threading.Timeout.InfiniteTimeSpan;
        }

        var timeout = options.Deadline.Value - DateTime.UtcNow;
        if (timeout <= TimeSpan.Zero)
        {
            return TimeSpan.FromTicks(1);
        }

        return timeout > MaxSupportedCancelAfter ? System.Threading.Timeout.InfiniteTimeSpan : timeout;
    }

    private CallOptions ApplyDeadlineAndCancellation(
        CallOptions options,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = options.Deadline ?? (timeout == System.Threading.Timeout.InfiniteTimeSpan
            ? (DateTime?)null
            : DateTime.UtcNow.Add(timeout));
        var effectiveOptions = deadline.HasValue ? options.WithDeadline(deadline.Value) : options;
        return effectiveOptions.WithCancellationToken(cancellationToken);
    }

    private static CancellationTokenSource CreateTimeoutCancellationTokenSource(
        CallOptions options,
        TimeSpan timeout)
    {
        var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(options.CancellationToken);
        if (timeout != System.Threading.Timeout.InfiniteTimeSpan)
        {
            timeoutCts.CancelAfter(timeout);
        }

        return timeoutCts;
    }

    private static async Task<TResponse> AwaitUnaryResponseAsync<TResponse>(
        AsyncUnaryCall<TResponse> call,
        TimeSpan timeout,
        CancellationTokenSource timeoutCts,
        CancellationToken callerCancellationToken)
    {
        try
        {
            return await call.ResponseAsync
                .WaitAsync(timeout, callerCancellationToken)
                .ConfigureAwait(false);
        }
        catch (TimeoutException exception)
        {
            call.Dispose();
            throw CreateDeadlineExceeded(timeout, exception);
        }
        catch (OperationCanceledException exception) when (timeoutCts.IsCancellationRequested &&
                                                           !callerCancellationToken.IsCancellationRequested)
        {
            call.Dispose();
            throw CreateDeadlineExceeded(timeout, exception);
        }
        catch (RpcException exception) when (IsOwnTimeout(exception, timeoutCts, callerCancellationToken))
        {
            call.Dispose();
            throw CreateDeadlineExceeded(timeout, exception);
        }
        finally
        {
            if (callerCancellationToken.IsCancellationRequested &&
                !timeoutCts.IsCancellationRequested)
            {
                await timeoutCts.CancelAsync().ConfigureAwait(false);
            }

            timeoutCts.Dispose();
        }
    }

    /// <summary>
    /// DeadlineExceeded always means a deadline expired; Cancelled only means a timeout when it was
    /// this invoker's own timer that cancelled the call (a caller cancellation must stay a cancellation).
    /// </summary>
    private static bool IsOwnTimeout(
        RpcException exception,
        CancellationTokenSource timeoutCts,
        CancellationToken callerCancellationToken)
    {
        if (exception.StatusCode == StatusCode.DeadlineExceeded)
        {
            return true;
        }

        return exception.StatusCode == StatusCode.Cancelled &&
               timeoutCts.IsCancellationRequested &&
               !callerCancellationToken.IsCancellationRequested;
    }

    private static RpcException CreateDeadlineExceeded(TimeSpan timeout, Exception innerException)
    {
        // The exception message is what reaches the logs, while Status.Detail is what reaches the peer,
        // so the effective timeout has to be part of the message: the previous version only carried the
        // inner message ("The operation has timed out.") and dropped the duration entirely.
        var detail = $"Unary call timed out after {timeout}.";
        return new RpcException(
            new Status(StatusCode.DeadlineExceeded, detail),
            $"{detail} (underlying: {Describe(innerException)})");
    }

    private static string Describe(Exception exception)
    {
        return exception is RpcException rpc ? rpc.Status.ToString() : exception.Message;
    }
}
