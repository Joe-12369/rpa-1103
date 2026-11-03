using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.IPC.NamedPipe.Abstractions;
using II.RPA.IPC.NamedPipe.Connections;
using II.RPA.IPC.NamedPipe.Framing;
using II.RPA.IPC.NamedPipe.Security;

namespace II.RPA.IPC.NamedPipe.Server
{
    public sealed class NamedPipeServer : IDisposable
    {
        private readonly INamedPipeRequestHandler _handler;
        private readonly NamedPipeServerOptions _options;
        private readonly CancellationTokenSource _serverCancellation = new CancellationTokenSource();
        private readonly SemaphoreSlim _serverRequests;
        private readonly ConcurrentDictionary<long, NamedPipeServerConnection> _connections = new ConcurrentDictionary<long, NamedPipeServerConnection>();
        private readonly ConcurrentDictionary<long, Task> _connectionTasks = new ConcurrentDictionary<long, Task>();
        private readonly TaskCompletionSource<bool> _ready = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly object _listenerSync = new object();
        private NamedPipeServerStream _pendingListener;
        private Task _acceptTask;
        private int _started;
        private int _stopped;

        public NamedPipeServer(string pipeName, INamedPipeRequestHandler handler, NamedPipeServerOptions options = null)
        {
            if (string.IsNullOrWhiteSpace(pipeName)) throw new ArgumentException("A pipe name is required.", nameof(pipeName));
            PipeName = pipeName;
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _options = (options ?? new NamedPipeServerOptions()).CreateValidatedSnapshot();
            _serverRequests = new SemaphoreSlim(_options.MaxPendingRequests, _options.MaxPendingRequests);
        }

        public string PipeName { get; }
        public Task Ready => _ready.Task;
        public Task Completion => _completion.Task;

        public async Task StartAsync()
        {
            if (Interlocked.CompareExchange(ref _started, 1, 0) != 0) throw new InvalidOperationException("The server has already been started.");
            _acceptTask = AcceptLoopAsync(_serverCancellation.Token);
            await Ready.ConfigureAwait(false);
        }

        public async Task StopAsync()
        {
            if (Interlocked.Exchange(ref _stopped, 1) != 0)
            {
                await Completion.ConfigureAwait(false);
                return;
            }

            _serverCancellation.Cancel();
            lock (_listenerSync) { _pendingListener?.Dispose(); }
            foreach (NamedPipeServerConnection connection in _connections.Values) connection.Close();
            if (_acceptTask != null)
            {
                try { await _acceptTask.ConfigureAwait(false); }
                catch when (_serverCancellation.IsCancellationRequested) { }
            }
            Task[] workers = _connectionTasks.Values.ToArray();
            if (workers.Length != 0) await Task.WhenAll(workers).ConfigureAwait(false);
            _completion.TrySetResult(true);
        }

        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
        }

        private async Task AcceptLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    NamedPipeServerStream listener = CreateListener();
                    lock (_listenerSync) _pendingListener = listener;
                    Task waitForConnection = listener.WaitForConnectionAsync(cancellationToken);
                    _ready.TrySetResult(true);
                    try { await waitForConnection.ConfigureAwait(false); }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { listener.Dispose(); break; }
                    catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested) { break; }
                    finally { lock (_listenerSync) { if (ReferenceEquals(_pendingListener, listener)) _pendingListener = null; } }

                    if (_connections.Count >= _options.MaxConnections)
                    {
                        listener.Dispose();
                        continue;
                    }

                    var connection = new NamedPipeServerConnection(listener, _options.MaxFrameLength);
                    if (!_connections.TryAdd(connection.ConnectionId, connection))
                    {
                        connection.Close();
                        continue;
                    }
                    Task worker = RunConnectionAsync(connection, cancellationToken);
                    _connectionTasks[connection.ConnectionId] = worker;
                    Task connectionCleanup = worker.ContinueWith(_ => { Task ignored; _connectionTasks.TryRemove(connection.ConnectionId, out ignored); }, TaskScheduler.Default);
                }
            }
            catch (Exception exception)
            {
                _ready.TrySetException(exception);
                _completion.TrySetException(exception);
                _serverCancellation.Cancel();
                foreach (NamedPipeServerConnection connection in _connections.Values) connection.Close();
            }
        }

        private NamedPipeServerStream CreateListener()
        {
#if NETFRAMEWORK
            return new NamedPipeServerStream(PipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 0, 0, CurrentUserPipeSecurity.Create());
#else
            return NamedPipeServerStreamAcl.Create(PipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 0, 0, CurrentUserPipeSecurity.Create());
#endif
        }

        private async Task RunConnectionAsync(NamedPipeServerConnection connection, CancellationToken serverCancellation)
        {
            var connectionRequests = new SemaphoreSlim(_options.MaxPendingRequestsPerConnection, _options.MaxPendingRequestsPerConnection);
            var requests = new HashSet<Task>();
            var requestsSync = new object();
            using (var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(serverCancellation, connection.ConnectionCancellation))
            {
                CancellationToken token = linkedCancellation.Token;
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        NamedPipeFrame frame = await connection.ReadAsync(token).ConfigureAwait(false);
                        if (frame.Kind == NamedPipeFrameKind.Ping)
                        {
                            await connection.SendControlAsync(NamedPipeFrameKind.Pong, token).ConfigureAwait(false);
                            continue;
                        }
                        if (frame.Kind == NamedPipeFrameKind.Pong) continue;

                        bool connectionPermit = false;
                        bool serverPermit = false;
                        try
                        {
                            await connectionRequests.WaitAsync(token).ConfigureAwait(false);
                            connectionPermit = true;
                            await _serverRequests.WaitAsync(token).ConfigureAwait(false);
                            serverPermit = true;
                        }
                        catch
                        {
                            if (serverPermit) _serverRequests.Release();
                            if (connectionPermit) connectionRequests.Release();
                            throw;
                        }

                        Task requestTask = ProcessRequestAsync(connection, frame.Payload, connectionRequests, token);
                        lock (requestsSync) requests.Add(requestTask);
                        Task requestCleanup = requestTask.ContinueWith(completed => { lock (requestsSync) requests.Remove(completed); }, TaskScheduler.Default);
                    }
                }
                catch (Exception exception) when (IsConnectionTermination(exception, token)) { }
                catch { connection.Close(); }
                finally
                {
                    NamedPipeServerConnection ignored;
                    _connections.TryRemove(connection.ConnectionId, out ignored);
                    connection.Close();
                    Task[] active;
                    lock (requestsSync) active = requests.ToArray();
                    try { await Task.WhenAll(active).ConfigureAwait(false); } catch { }
                }
            }
        }

        private async Task ProcessRequestAsync(NamedPipeServerConnection connection, byte[] request, SemaphoreSlim connectionRequests, CancellationToken token)
        {
            try
            {
                var context = new NamedPipeRequestContext(connection.ConnectionId, connection.ConnectionCancellation);
                byte[] response = await _handler.HandleRequestAsync(context, request, token).ConfigureAwait(false);
                if (response != null) await connection.SendAsync(response, token).ConfigureAwait(false);
            }
            catch { connection.Close(); }
            finally
            {
                _serverRequests.Release();
                connectionRequests.Release();
            }
        }

        private static bool IsConnectionTermination(Exception exception, CancellationToken token)
        {
            return token.IsCancellationRequested || exception is EndOfStreamException || exception is IOException || exception is ObjectDisposedException || exception is OperationCanceledException;
        }
    }
}
