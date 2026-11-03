using System.Collections.Concurrent;
using Grpc.Core;
using II.RPA.Communication.Abstractions;
using II.RPA.Communication.Exceptions;
using II.RPA.Communication.Timeout;

namespace II.RPA.Communication.Stream.Infrastructure;

public sealed class RequestReplyStreamSession<TRequest, TResponse> :
    IRequestReplyStreamSession<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    private readonly AsyncDuplexStreamingCall<TRequest, TResponse> _call;
    private readonly Func<TRequest, string> _requestCorrelationIdSelector;
    private readonly Func<TResponse, string> _responseCorrelationIdSelector;
    private readonly TimeoutResolver _timeoutResolver;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<TResponse>> _pendingRequests = new(
        StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly CancellationTokenSource _disposeCts = new();
    private readonly Task _readLoop;
    private bool _disposed;

    public RequestReplyStreamSession(
        AsyncDuplexStreamingCall<TRequest, TResponse> call,
        Func<TRequest, string> requestCorrelationIdSelector,
        Func<TResponse, string> responseCorrelationIdSelector,
        TimeoutResolver timeoutResolver)
    {
        _call = call ?? throw new ArgumentNullException(nameof(call));
        _requestCorrelationIdSelector = requestCorrelationIdSelector ??
            throw new ArgumentNullException(nameof(requestCorrelationIdSelector));
        _responseCorrelationIdSelector = responseCorrelationIdSelector ??
            throw new ArgumentNullException(nameof(responseCorrelationIdSelector));
        _timeoutResolver = timeoutResolver ?? throw new ArgumentNullException(nameof(timeoutResolver));
        _readLoop = Task.Run(ReadResponsesAsync);
    }

    public async Task<TResponse> SendAsync(
        TRequest request,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(request);

        var correlationId = _requestCorrelationIdSelector(request);
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException("Request correlation id cannot be empty.", nameof(request));
        }

        var effectiveTimeout = _timeoutResolver.Resolve(timeout);
        var pending = new TaskCompletionSource<TResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pendingRequests.TryAdd(correlationId, pending))
        {
            throw new InvalidOperationException(
                $"A pending stream request with correlation id '{correlationId}' already exists.");
        }

        using var timeoutCts = new CancellationTokenSource(effectiveTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCts.Token,
            _disposeCts.Token);

        try
        {
            await _writeLock.WaitAsync(linkedCts.Token).ConfigureAwait(false);
            try
            {
                await _call.RequestStream.WriteAsync(request).ConfigureAwait(false);
            }
            finally
            {
                _writeLock.Release();
            }

            return await pending.Task.WaitAsync(linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (timeoutCts.IsCancellationRequested &&
                                                           !cancellationToken.IsCancellationRequested)
        {
            _pendingRequests.TryRemove(correlationId, out _);
            throw new GRPCTimeoutException(
                $"Stream request '{correlationId}' timed out after {effectiveTimeout}.",
                exception);
        }
        catch
        {
            _pendingRequests.TryRemove(correlationId, out _);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _disposeCts.Cancel();

        try
        {
            await _call.RequestStream.CompleteAsync().ConfigureAwait(false);
        }
        catch
        {
            // Disposal should not mask caller-side failures.
        }

        _call.Dispose();

        try
        {
            await _readLoop.WaitAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        }
        catch
        {
            // The call may already be disposed; pending requests are failed below.
        }

        FailAll(new ObjectDisposedException(nameof(RequestReplyStreamSession<TRequest, TResponse>)));
        _disposeCts.Dispose();
        _writeLock.Dispose();
    }

    private async Task ReadResponsesAsync()
    {
        try
        {
            while (await _call.ResponseStream.MoveNext(_disposeCts.Token).ConfigureAwait(false))
            {
                var response = _call.ResponseStream.Current;
                var correlationId = _responseCorrelationIdSelector(response);
                if (string.IsNullOrWhiteSpace(correlationId))
                {
                    continue;
                }

                if (_pendingRequests.TryRemove(correlationId, out var pending))
                {
                    pending.TrySetResult(response);
                }
            }
        }
        catch (OperationCanceledException) when (_disposeCts.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            FailAll(exception);
        }
    }

    private void FailAll(Exception exception)
    {
        foreach (var item in _pendingRequests)
        {
            if (_pendingRequests.TryRemove(item.Key, out var pending))
            {
                pending.TrySetException(exception);
            }
        }
    }
}
