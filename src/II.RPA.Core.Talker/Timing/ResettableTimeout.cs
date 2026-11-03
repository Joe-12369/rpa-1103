using II.RPA.Core.Log;
using System.Diagnostics;

namespace II.RPA.Core.Talker.Timing;

internal sealed class ResettableTimeout
{
    private readonly object _gate = new();
    private readonly Stopwatch _stopwatch = new();
    private readonly ILogger _logger;
    private CancellationTokenSource? _executionCancellation;

    public ResettableTimeout(int milliseconds, ILogger logger)
        : this(TimeSpan.FromMilliseconds(milliseconds), logger)
    {
    }

    public ResettableTimeout(TimeSpan timeout, ILogger logger, string? uniqueId = null)
    {
        Timeout = timeout;
        _logger = logger;
        UniqueId = string.IsNullOrWhiteSpace(uniqueId)
            ? Guid.NewGuid().ToString()
            : uniqueId;
    }

    public TimeSpan Timeout { get; set; }

    public bool IsTimeout { get; private set; }

    public bool HasReset { get; private set; }

    public string UniqueId { get; }

    public string? TimeoutMessage { get; private set; }

    public long ElapsedMilliseconds
    {
        get
        {
            lock (_gate)
            {
                return _stopwatch.ElapsedMilliseconds;
            }
        }
    }

    public void Reset(TimeSpan timeout, string? timeoutMessage = null)
    {
        lock (_gate)
        {
            if (_executionCancellation == null)
            {
                throw new InvalidOperationException("Timeout execution has not started.");
            }

            TimeoutMessage = timeoutMessage;
            Timeout = timeout;
            if (_executionCancellation.IsCancellationRequested)
            {
                _logger.LogDebug("Execution has already completed; timeout reset was ignored.");
                return;
            }

            _stopwatch.Restart();
            HasReset = true;
            _logger.LogDebug($"Reset timeout to {timeout.TotalMilliseconds} ms");
        }
    }

    public void Reset(long milliseconds, string? timeoutMessage = null)
    {
        Reset(TimeSpan.FromMilliseconds(milliseconds), timeoutMessage);
    }

    public async Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> action,
        Func<TimeSpan, Task> onTimeoutAsync)
    {
        ArgumentNullException.ThrowIfNull(action);
        return await ExecuteAsync(
                _ => action(),
                CancellationToken.None,
                onTimeoutAsync)
            .ConfigureAwait(false);
    }

    public async Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> action,
        CancellationToken cancellationToken,
        Func<TimeSpan, Task> onTimeoutAsync)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(onTimeoutAsync);

        CancellationTokenSource executionCancellation;
        lock (_gate)
        {
            if (_executionCancellation != null)
            {
                throw new InvalidOperationException("A timeout instance can execute only once.");
            }

            executionCancellation = new CancellationTokenSource();
            _executionCancellation = executionCancellation;
            _stopwatch.Restart();
        }

        // Keep transport cancellation in step with both the caller and the resettable budget.
        using var actionCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            executionCancellation.Token);

        try
        {
            var actionTask = action(actionCancellation.Token);
            var timeoutTask = WaitForTimeoutAsync(executionCancellation.Token);
            var completed = await Task.WhenAny(actionTask, timeoutTask).ConfigureAwait(false);

            if (completed == actionTask)
            {
                var result = await actionTask.ConfigureAwait(false);
                if (IsExpired())
                {
                    await ThrowTimeoutAsync(onTimeoutAsync).ConfigureAwait(false);
                }

                return result;
            }

            await actionCancellation.CancelAsync().ConfigureAwait(false);
            await ThrowTimeoutAsync(onTimeoutAsync).ConfigureAwait(false);
            throw new TimeoutException();
        }
        finally
        {
            lock (_gate)
            {
                _stopwatch.Stop();
                executionCancellation.Cancel();
                executionCancellation.Dispose();
            }
        }
    }

    private async Task WaitForTimeoutAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (IsExpired())
            {
                return;
            }

            long remainingMilliseconds;
            lock (_gate)
            {
                remainingMilliseconds = (long)Math.Max(
                    1,
                    Timeout.TotalMilliseconds - _stopwatch.ElapsedMilliseconds);
            }

            try
            {
                await Task.Delay(
                        (int)Math.Min(500, remainingMilliseconds),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private bool IsExpired()
    {
        lock (_gate)
        {
            return _stopwatch.ElapsedMilliseconds >= Timeout.TotalMilliseconds;
        }
    }

    private async Task ThrowTimeoutAsync(Func<TimeSpan, Task> onTimeoutAsync)
    {
        IsTimeout = true;
        var elapsed = TimeSpan.FromMilliseconds(ElapsedMilliseconds);
        _logger.LogWarning(
            $"ExecuteAsync wall-clock timeout after {elapsed.TotalMilliseconds} ms " +
            $"(limit {Timeout.TotalMilliseconds} ms)");
        await onTimeoutAsync(elapsed).ConfigureAwait(false);
        throw new TimeoutException();
    }
}
