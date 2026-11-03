using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.IPC.NamedPipe.Framing;

namespace II.RPA.IPC.NamedPipe.Client
{
    public sealed class NamedPipeMessageReceivedEventArgs : EventArgs
    {
        public NamedPipeMessageReceivedEventArgs(byte[] payload)
        {
            Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        }

        public byte[] Payload { get; }
    }

    public sealed class NamedPipeClient : IDisposable
    {
        private readonly string _pipeName;
        private readonly NamedPipeClientOptions _options;
        private readonly object _sync = new object();
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _pingLock = new SemaphoreSlim(1, 1);
        private NamedPipeClientStream _stream;
        private CancellationTokenSource _connectionCancellation;
        private Task _readTask;
        private TaskCompletionSource<bool> _pendingPong;
        private int _connected;
        private int _disconnectSignaled;
        private int _connecting;
        private int _disposed;

        public NamedPipeClient(string pipeName, NamedPipeClientOptions options = null)
        {
            if (string.IsNullOrWhiteSpace(pipeName)) throw new ArgumentException("A pipe name is required.", nameof(pipeName));
            _pipeName = pipeName;
            _options = (options ?? new NamedPipeClientOptions()).CreateValidatedSnapshot();
        }

        public event EventHandler<NamedPipeMessageReceivedEventArgs> MessageReceived;
        public event EventHandler Disconnected;

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (Interlocked.CompareExchange(ref _connecting, 1, 0) != 0) throw new InvalidOperationException("The client is already connected or connecting.");

            var stream = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            try
            {
                int timeoutMilliseconds = checked((int)Math.Min(_options.ConnectTimeout.TotalMilliseconds, int.MaxValue));
                await stream.ConnectAsync(timeoutMilliseconds, cancellationToken).ConfigureAwait(false);
                var connectionCancellation = new CancellationTokenSource();
                lock (_sync)
                {
                    ThrowIfDisposed();
                    _stream = stream;
                    _connectionCancellation = connectionCancellation;
                    Volatile.Write(ref _disconnectSignaled, 0);
                    Volatile.Write(ref _connected, 1);
                    _readTask = ReadLoopAsync(stream, connectionCancellation.Token);
                }
                stream = null;
            }
            finally
            {
                stream?.Dispose();
                if (Volatile.Read(ref _connected) == 0) Volatile.Write(ref _connecting, 0);
            }
        }

        public async Task SendAsync(byte[] payload, CancellationToken cancellationToken)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            NamedPipeClientStream stream = GetConnectedStream();
            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try { await NamedPipeFrameWriter.WriteDataAsync(stream, payload, _options.MaxFrameLength, cancellationToken).ConfigureAwait(false); }
            finally { _writeLock.Release(); }
        }

        public async Task PingAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "The value must be positive.");
            await _pingLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                NamedPipeClientStream stream = GetConnectedStream();
                var pong = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                lock (_sync) _pendingPong = pong;
                try
                {
                    await SendControlAsync(stream, NamedPipeFrameKind.Ping, cancellationToken).ConfigureAwait(false);
                    Task timeoutTask = Task.Delay(timeout, cancellationToken);
                    Task completed = await Task.WhenAny(pong.Task, timeoutTask).ConfigureAwait(false);
                    if (ReferenceEquals(completed, pong.Task)) await pong.Task.ConfigureAwait(false);
                    else
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        throw new TimeoutException("The named pipe peer did not return Pong before the timeout elapsed.");
                    }
                }
                finally
                {
                    lock (_sync) { if (ReferenceEquals(_pendingPong, pong)) _pendingPong = null; }
                }
            }
            finally { _pingLock.Release(); }
        }

        public async Task DisconnectAsync()
        {
            Task readTask;
            lock (_sync)
            {
                _connectionCancellation?.Cancel();
                _stream?.Dispose();
                readTask = _readTask;
            }
            if (readTask != null && !ReferenceEquals(readTask, Task.CurrentId))
            {
                try { await readTask.ConfigureAwait(false); } catch { }
            }
            CompleteDisconnect();
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            DisconnectAsync().GetAwaiter().GetResult();
            _writeLock.Dispose();
            _pingLock.Dispose();
        }

        private async Task ReadLoopAsync(NamedPipeClientStream stream, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    NamedPipeFrame frame = await NamedPipeFrameReader.ReadAsync(stream, _options.MaxFrameLength, cancellationToken).ConfigureAwait(false);
                    if (frame.Kind == NamedPipeFrameKind.Pong)
                    {
                        TaskCompletionSource<bool> pong;
                        lock (_sync) pong = _pendingPong;
                        pong?.TrySetResult(true);
                    }
                    else if (frame.Kind == NamedPipeFrameKind.Ping)
                    {
                        await SendControlAsync(stream, NamedPipeFrameKind.Pong, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        RaiseMessageReceived(frame.Payload);
                    }
                }
            }
            catch (Exception exception) when (IsConnectionTermination(exception, cancellationToken)) { }
            finally { CompleteDisconnect(); }
        }

        private async Task SendControlAsync(NamedPipeClientStream stream, NamedPipeFrameKind kind, CancellationToken cancellationToken)
        {
            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try { await NamedPipeFrameWriter.WriteControlAsync(stream, kind, cancellationToken).ConfigureAwait(false); }
            finally { _writeLock.Release(); }
        }

        private void CompleteDisconnect()
        {
            TaskCompletionSource<bool> pendingPong;
            bool notify = false;
            lock (_sync)
            {
                if (Interlocked.Exchange(ref _connected, 0) != 0)
                {
                    _connectionCancellation?.Cancel();
                    _stream?.Dispose();
                    _stream = null;
                    _connectionCancellation?.Dispose();
                    _connectionCancellation = null;
                    _readTask = null;
                    Volatile.Write(ref _connecting, 0);
                    notify = Interlocked.Exchange(ref _disconnectSignaled, 1) == 0;
                }
                pendingPong = _pendingPong;
                _pendingPong = null;
            }
            pendingPong?.TrySetCanceled();
            if (notify) RaiseDisconnected();
        }

        private NamedPipeClientStream GetConnectedStream()
        {
            ThrowIfDisposed();
            lock (_sync)
            {
                if (Volatile.Read(ref _connected) == 0 || _stream == null) throw new InvalidOperationException("The client is not connected.");
                return _stream;
            }
        }

        private void RaiseMessageReceived(byte[] payload)
        {
            EventHandler<NamedPipeMessageReceivedEventArgs> handler = MessageReceived;
            if (handler == null) return;
            var args = new NamedPipeMessageReceivedEventArgs(payload);
            foreach (EventHandler<NamedPipeMessageReceivedEventArgs> subscriber in handler.GetInvocationList())
            {
                try { subscriber(this, args); } catch { }
            }
        }

        private void RaiseDisconnected()
        {
            EventHandler handler = Disconnected;
            if (handler == null) return;
            foreach (EventHandler subscriber in handler.GetInvocationList())
            {
                try { subscriber(this, EventArgs.Empty); } catch { }
            }
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0) throw new ObjectDisposedException(nameof(NamedPipeClient));
        }

        private static bool IsConnectionTermination(Exception exception, CancellationToken cancellationToken)
        {
            return cancellationToken.IsCancellationRequested || exception is EndOfStreamException || exception is IOException ||
                   exception is ObjectDisposedException || exception is OperationCanceledException;
        }
    }
}
