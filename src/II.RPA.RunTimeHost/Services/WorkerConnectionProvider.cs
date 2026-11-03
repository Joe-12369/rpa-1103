using Grpc.Core;
using II.RPA.Communication.Endpoints;
using II.RPA.Communication.Hosting;
using II.RPA.Communication.Options;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using static Rpa.Core.Worker.WorkerService;

namespace II.RPA.RuntimeHost.Services
{
    public class WorkerConnectionProvider : IDisposable
    {
        private static readonly ILogger _logger = Logger.OfContext();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _cts;
        private WorkerConnection _streamConnection;
        private GrpcClient _client;
        private readonly int _pid;
        private readonly int _serverPort;
        private readonly TimeSpan _reconnectDelay;

        public WorkerConnectionProvider() : this(
            CommandParamsHelper.ServerPort,
            CommandParamsHelper.CurrentProcessId,
            TimeSpan.FromSeconds(1))
        {
        }

        internal WorkerConnectionProvider(int serverPort, int pid, TimeSpan reconnectDelay)
        {
            _pid = pid;
            _serverPort = serverPort;
            _reconnectDelay = reconnectDelay;
        }

        // Creating a duplex call does not mean Worker registration succeeded.
        // Recreate the call (and channel) when reading ends, including startup failures.
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var connection = await ConnectAsync(cancellationToken).ConfigureAwait(false);
                    RuntimeMessageService.Bridge(connection);
                    await connection.Completion.WaitAsync(cancellationToken).ConfigureAwait(false);
                    _logger.LogWarning("Worker stream closed; reconnecting to CoreServer");
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(
                        _logger, ex, "Worker connection failed; reconnecting to CoreServer");
                }
                finally
                {
                    try
                    {
                        await DisconnectAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(
                            _logger, ex, "Failed to clean up Worker connection");
                    }
                }

                try
                {
                    await Task.Delay(_reconnectDelay, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        public async Task<WorkerConnection> ConnectAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await DisconnectCoreAsync().ConfigureAwait(false);

                var address = $"127.0.0.1:{_serverPort}";
                _logger.LogInformation($"Opening Worker stream to gRPC server at {address}");
                _client = new GrpcClient(new ClientOptions()
                {
                    Endpoint = new TcpEndpoint("127.0.0.1", _serverPort),
                    Timeout = new TimeoutOptions() { DefaultRequestTimeout = TimeSpan.FromSeconds(5) }
                });

                var headers = new Metadata
                {
                    { "pid", $"{_pid}" },
                    { "role", "runtimehost" },
                    { "name", "runtimehost" }
                };
                _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var callOptions = new CallOptions(headers, cancellationToken: _cts.Token);
                var workerServiceClient = _client.CreateClient(invoker => new WorkerServiceClient(invoker));
                var stream = workerServiceClient.Talk(callOptions);
                _streamConnection = new WorkerConnection(stream);
                return _streamConnection;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task DisconnectAsync()
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                await DisconnectCoreAsync().ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        // Called only while holding _semaphore. Never await CompleteAsync on a failed stream.
        private async Task DisconnectCoreAsync()
        {
            var cts = _cts;
            var connection = _streamConnection;
            var client = _client;
            _cts = null;
            _streamConnection = null;
            _client = null;
            try
            {
                cts?.Cancel();
            }
            finally
            {
                try
                {
                    connection?.Dispose();
                }
                finally
                {
                    cts?.Dispose();
                    if (client != null)
                    {
                        await client.DisposeAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        public void Dispose()
        {
            DisconnectAsync().GetAwaiter().GetResult();
            _semaphore.Dispose();
        }
    }
}
