using Grpc.Core;
using II.RPA.Communication.Endpoints;
using II.RPA.Communication.Exceptions;
using II.RPA.Communication.Hosting;
using II.RPA.Communication.Options;
using II.RPA.Core.Log;
using II.RPA.Core.Runtime;
using II.RPA.Core.Talker.Models;
using II.RPA.Core.Talker.Processes;
using Rpa.Core.Mobileplugin;
using System.Diagnostics;

namespace II.RPA.Core.Talker.Talkers
{
    public class MobileProviderTalker : BaseTalker, IMobileProviderController
    {
        private static readonly string MobileChannelFileName = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Z-Core", "Z-Mobile.txt");
        private static readonly TimeSpan DefaultStartupTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan DefaultStartupProbeInterval = TimeSpan.FromMilliseconds(200);
        private static readonly TimeSpan DefaultHeartbeatInterval = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan DefaultHeartbeatTimeout = TimeSpan.FromSeconds(3);
        private const int DefaultHeartbeatFailureThreshold = 3;
        private static readonly TimeSpan MobileActionTimeoutGrace = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan LongRunningMobileActionTimeout = TimeSpan.FromHours(24);

        private readonly string _channelName;
        private readonly IMobileProviderProcessHandle _mobileProviderProcess;
        private readonly bool _startProcess;
        private readonly object _clientReadyGate = new();
        private readonly SemaphoreSlim _cleanupGate = new(1, 1);
        private readonly TimeSpan _startupTimeout;
        private readonly TimeSpan _startupProbeInterval;
        private readonly TimeSpan _heartbeatInterval;
        private readonly TimeSpan _heartbeatTimeout;
        private readonly int _heartbeatFailureThreshold;

        private TaskCompletionSource<bool> _clientReady = CreateClientReadySource();
        private CancellationTokenSource? _startupCancellation;
        private CancellationTokenSource? _heartbeatCancellation;
        private Task? _heartbeatTask;
        private GrpcClient? _grpcClient;
        private MobilePluginService.MobilePluginServiceClient? _mobileServiceClient;

        public MobileProviderTalker(ILogger logger, IRpaRuntimeContext runtimeContext)
            : this(logger, runtimeContext, null, true)
        {
        }

        internal MobileProviderTalker(
            ILogger logger,
            IRpaRuntimeContext runtimeContext,
            string? channelName,
            bool startProcess,
            TimeSpan? startupTimeout = null,
            TimeSpan? startupProbeInterval = null,
            TimeSpan? heartbeatInterval = null,
            TimeSpan? heartbeatTimeout = null,
            int? heartbeatFailureThreshold = null,
            IMobileProviderProcessHandle? mobileProviderProcess = null) : base(logger)
        {
            _channelName = string.IsNullOrWhiteSpace(channelName) ? Guid.NewGuid().ToString() : channelName;
            _mobileProviderProcess = mobileProviderProcess ??
                new MobileeProviderProcess(_channelName, logger, runtimeContext);
            _startProcess = startProcess;
            _startupTimeout = EnsurePositive(startupTimeout ?? DefaultStartupTimeout, nameof(startupTimeout));
            _startupProbeInterval = EnsurePositive(
                startupProbeInterval ?? DefaultStartupProbeInterval,
                nameof(startupProbeInterval));
            _heartbeatInterval = EnsurePositive(
                heartbeatInterval ??
                ResolveConfiguredSeconds(runtimeContext, "-mobile-heartbeat-interval", "MobileHeartbeatInterval") ??
                DefaultHeartbeatInterval,
                nameof(heartbeatInterval));
            _heartbeatTimeout = EnsurePositive(
                heartbeatTimeout ??
                ResolveConfiguredSeconds(runtimeContext, "-mobile-heartbeat-timeout", "MobileHeartbeatTimeout") ??
                DefaultHeartbeatTimeout,
                nameof(heartbeatTimeout));
            _heartbeatFailureThreshold = ResolveHeartbeatFailureThreshold(runtimeContext, heartbeatFailureThreshold);
            EnsureHeartbeatTimeoutWithinInterval(_heartbeatTimeout, _heartbeatInterval);
            _mobileProviderProcess.OnProcessStatusChangedEvent += (_, status) =>
            {
                if (status == Models.ProcessStatus.Stopped && !_mobileProviderProcess.IsInitiativeStop)
                {
                    ResetClientReadyForConnection();
                    MarkDisconnected(new IOException("MobileProvider 进程意外退出。"));
                }
            };
        }

        public override string Name => "mobileprovider";

        public bool IsEnabled => true;

        public int? ProcessId => _mobileProviderProcess.ProcessId;

        public ProcessStatus ProcessStatus => _mobileProviderProcess.Status;

        public override async Task Start()
        {
            await base.Start().ConfigureAwait(false);
            ResetClientReadyForConnection();

            var startupCancellation = new CancellationTokenSource();
            var previousStartupCancellation = Interlocked.Exchange(
                ref _startupCancellation,
                startupCancellation);
            previousStartupCancellation?.Cancel();
            previousStartupCancellation?.Dispose();

            try
            {
                if (_startProcess)
                {
                    var channelDirectory = Path.GetDirectoryName(MobileChannelFileName)!;
                    Directory.CreateDirectory(channelDirectory);
                    File.WriteAllText(MobileChannelFileName, _channelName);
                }

                _grpcClient = new GrpcClient(new ClientOptions
                {
                    Endpoint = new NamedPipeEndpoint(_channelName),
                    NamedPipeConnectionTimeoutMilliseconds = 1000
                });
                _mobileServiceClient = _grpcClient.CreateClient(invoker =>
                    new MobilePluginService.MobilePluginServiceClient(invoker));

                if (_startProcess)
                {
                    _mobileProviderProcess.Start();
                }

                await WaitUntilServiceReadyAsync(startupCancellation.Token).ConfigureAwait(false);
                if (!EstablishConnection())
                {
                    throw LastFault ?? new InvalidOperationException("MobileProvider Talker 未能建立连接。");
                }

                CompleteClientReady();
                Logger.LogInfo("MobileProvider named-pipe service is ready");

                var heartbeatCancellation = new CancellationTokenSource();
                var previousHeartbeatCancellation = Interlocked.Exchange(
                    ref _heartbeatCancellation,
                    heartbeatCancellation);
                var previousHeartbeatTask = Interlocked.Exchange(ref _heartbeatTask, null);
                if (previousHeartbeatCancellation != null)
                {
                    previousHeartbeatCancellation.Cancel();
                    if (previousHeartbeatTask != null)
                    {
                        // The previous loop may still be waiting on that token (PeriodicTimer registers
                        // on it), so it has to finish before the source gets disposed.
                        try
                        {
                            await previousHeartbeatTask.ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                        }
                    }

                    previousHeartbeatCancellation.Dispose();
                }

                _heartbeatTask = RunHeartbeatAsync(heartbeatCancellation.Token);
            }
            catch (OperationCanceledException) when (startupCancellation.IsCancellationRequested)
            {
                CancelClientReady();
                await CleanupTransportAsync().ConfigureAwait(false);
                await base.Stop().ConfigureAwait(false);
                throw;
            }
            catch (Exception ex)
            {
                FailClientReady(ex);
                await CleanupTransportAsync().ConfigureAwait(false);
                MarkDisconnected(ex);
                throw;
            }
            finally
            {
                if (ReferenceEquals(
                    Interlocked.CompareExchange(ref _startupCancellation, null, startupCancellation),
                    startupCancellation))
                {
                    startupCancellation.Dispose();
                }
            }
        }

        public async Task<MobilePluginInvocationResult> InvokeAsync(
            string action,
            string input,
            int timeout,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(action);
            var clientReady = GetClientReadyTask();
            if (cancellationToken.CanBeCanceled)
            {
                await clientReady.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await clientReady.ConfigureAwait(false);
            }

            var client = _mobileServiceClient
                ?? throw new InvalidOperationException("MobileProvider Talker 尚未启动或已经停止。");
            var deadline = DateTime.UtcNow.Add(ResolveMobileActionCallTimeout(timeout));
            var response = await client.PluginActionInvokeUnaryAsync(new MobileRequest
            {
                Action = action,
                InParameter = input ?? string.Empty,
                TimeOut = timeout
            }, deadline: deadline, cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false);

            return new MobilePluginInvocationResult
            {
                Code = response.Code,
                Message = response.Message,
                Info = response.Info
            };
        }

        public override async Task Stop()
        {
            Volatile.Read(ref _startupCancellation)?.Cancel();
            CancelClientReady();

            var heartbeatCancellation = Interlocked.Exchange(ref _heartbeatCancellation, null);
            heartbeatCancellation?.Cancel();
            var heartbeatTask = Interlocked.Exchange(ref _heartbeatTask, null);
            if (heartbeatTask != null)
            {
                try
                {
                    await heartbeatTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }
            heartbeatCancellation?.Dispose();

            await CleanupTransportAsync().ConfigureAwait(false);
            await base.Stop().ConfigureAwait(false);
        }

        private async Task WaitUntilServiceReadyAsync(CancellationToken cancellationToken)
        {
            var deadline = DateTimeOffset.UtcNow + _startupTimeout;
            Exception? lastException = null;
            while (DateTimeOffset.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var response = await CheckHeartbeatAsync(cancellationToken).ConfigureAwait(false);
                    if (string.Equals(response.Code, "0", StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    lastException = new InvalidOperationException(
                        $"MobileProvider 心跳返回失败，code={response.Code}, message={response.Message}");
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }

                await Task.Delay(_startupProbeInterval, cancellationToken).ConfigureAwait(false);
            }

            throw new TimeoutException(
                $"等待 MobileProvider 命名管道服务就绪超时（{_startupTimeout.TotalSeconds:0} 秒）。",
                lastException);
        }

        private async Task RunHeartbeatAsync(CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(_heartbeatInterval);
            var consecutiveFailures = 0;
            try
            {
                while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
                {
                    var stopwatch = Stopwatch.StartNew();
                    try
                    {
                        var response = await CheckHeartbeatAsync(cancellationToken).ConfigureAwait(false);
                        if (!string.Equals(response.Code, "0", StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException(
                                $"MobileProvider 心跳返回失败，code={response.Code}, message={response.Message}");
                        }

                        consecutiveFailures = 0;
                        if (!IsReady && EstablishConnection())
                        {
                            CompleteClientReady();
                            Logger.LogInfo("MobileProvider heartbeat recovered; Talker is ready again");
                        }
                    }
                    catch (Exception) when (cancellationToken.IsCancellationRequested)
                    {
                        // Stop()/restart cancels the loop and the transport reports that as a cancelled
                        // call; a shutdown must never be logged as a heartbeat fault.
                        break;
                    }
                    catch (Exception ex)
                    {
                        consecutiveFailures++;
                        var disconnecting = IsImmediateDisconnect(ex) ||
                                            consecutiveFailures >= _heartbeatFailureThreshold;
                        if (disconnecting)
                        {
                            ResetClientReadyForConnection();
                            MarkDisconnected(ex);
                        }

                        LogHeartbeatFailure(ex, consecutiveFailures, stopwatch.Elapsed, disconnecting);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
        }

        private void LogHeartbeatFailure(Exception exception, int attempt, TimeSpan elapsed, bool disconnecting)
        {
            var status = exception is RpcException rpc ? $" status={rpc.Status}" : string.Empty;
            var message =
                $"MobileProvider heartbeat check failed (attempt={attempt}, elapsed={elapsed.TotalMilliseconds:0}ms{status}): {exception.Message}";

            if (!disconnecting)
            {
                // Host scheduling jitter can push a single heartbeat past its deadline, and the next tick
                // repairs readiness, so a transient miss stays a one-line warning without a stack trace.
                Logger.LogWarning(message);
                return;
            }

            Logger.LogError(
                $"{message} -> MobileProvider talker is marked disconnected (threshold={_heartbeatFailureThreshold})",
                exception);
        }

        private static bool IsImmediateDisconnect(Exception exception)
        {
            // A timeout is the jitter-tolerant case and has to survive the consecutive-failure threshold;
            // anything else (pipe/transport unavailable, server-side fault, unexpected error) means the
            // service is really broken and disconnects right away.
            return !GRPCExceptionHelper.IsTimeout(exception);
        }

        private Task<MobileResponse> CheckHeartbeatAsync(CancellationToken cancellationToken)
        {
            var client = _mobileServiceClient
                ?? throw new InvalidOperationException("MobileProvider gRPC 客户端尚未创建。");
            return client.CheckServiceHeartbeatUnaryAsync(
                new MobileRequest(),
                deadline: DateTime.UtcNow.Add(_heartbeatTimeout),
                cancellationToken: cancellationToken).ResponseAsync;
        }

        private static TimeSpan ResolveMobileActionCallTimeout(int timeout)
        {
            if (timeout <= 0)
            {
                return LongRunningMobileActionTimeout;
            }

            return TimeSpan.FromMilliseconds(timeout).Add(MobileActionTimeoutGrace);
        }

        private async Task CleanupTransportAsync()
        {
            await _cleanupGate.WaitAsync().ConfigureAwait(false);
            try
            {
                // MobileProvider owns its lifecycle and must survive CoreServer transport shutdown.
                _mobileServiceClient = null;
                var grpcClient = Interlocked.Exchange(ref _grpcClient, null);
                if (grpcClient != null)
                {
                    await grpcClient.DisposeAsync().ConfigureAwait(false);
                }

                try
                {
                    if (_startProcess &&
                        File.Exists(MobileChannelFileName) &&
                        string.Equals(File.ReadAllText(MobileChannelFileName), _channelName, StringComparison.Ordinal))
                    {
                        File.Delete(MobileChannelFileName);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogDebug("清理 MobileProvider 通道文件失败", ex);
                }
            }
            finally
            {
                _cleanupGate.Release();
            }
        }

        private static TaskCompletionSource<bool> CreateClientReadySource() =>
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private Task GetClientReadyTask()
        {
            lock (_clientReadyGate)
            {
                return _clientReady.Task;
            }
        }

        private void ResetClientReadyForConnection()
        {
            lock (_clientReadyGate)
            {
                if (_clientReady.Task.IsCompleted)
                {
                    _clientReady = CreateClientReadySource();
                }
            }
        }

        private void CompleteClientReady()
        {
            lock (_clientReadyGate)
            {
                _clientReady.TrySetResult(true);
            }
        }

        private void CancelClientReady()
        {
            lock (_clientReadyGate)
            {
                _clientReady.TrySetCanceled();
            }
        }

        private void FailClientReady(Exception exception)
        {
            lock (_clientReadyGate)
            {
                _clientReady.TrySetException(exception);
            }
        }

        /// <summary>
        /// Reads a positive duration (expressed in seconds) from the runtime configuration so the
        /// heartbeat cadence and budget can be tuned on site. Returns null when nothing is configured.
        /// </summary>
        private static TimeSpan? ResolveConfiguredSeconds(
            IRpaRuntimeContext runtimeContext,
            string configurationKey,
            string settingKey)
        {
            var configured = runtimeContext.GetConfiguration(configurationKey, settingKey);
            if (string.IsNullOrWhiteSpace(configured))
            {
                return null;
            }

            if (!int.TryParse(configured, out var seconds) || seconds <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    settingKey,
                    configured,
                    $"{settingKey} 必须是大于 0 的整数（秒）。");
            }

            return TimeSpan.FromSeconds(seconds);
        }

        private static int ResolveHeartbeatFailureThreshold(
            IRpaRuntimeContext runtimeContext,
            int? configuredThreshold)
        {
            if (configuredThreshold.HasValue)
            {
                return EnsurePositiveCount(configuredThreshold.Value, nameof(configuredThreshold));
            }

            var configured = runtimeContext.GetConfiguration(
                "-mobile-heartbeat-failure-threshold",
                "MobileHeartbeatFailureThreshold");
            if (string.IsNullOrWhiteSpace(configured))
            {
                return DefaultHeartbeatFailureThreshold;
            }

            if (!int.TryParse(configured, out var threshold))
            {
                throw new ArgumentOutOfRangeException(
                    "MobileHeartbeatFailureThreshold",
                    configured,
                    "MobileHeartbeatFailureThreshold 必须是大于 0 的整数。");
            }

            return EnsurePositiveCount(threshold, "MobileHeartbeatFailureThreshold");
        }

        private static int EnsurePositiveCount(int value, string parameterName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "The heartbeat failure threshold must be positive.");
            }

            return value;
        }

        private static void EnsureHeartbeatTimeoutWithinInterval(TimeSpan timeout, TimeSpan interval)
        {
            if (timeout >= interval)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeout),
                    timeout,
                    $"心跳超时必须小于心跳间隔（timeout={timeout}，interval={interval}），否则一次探测会跨过下一次 tick。");
            }
        }

        private static TimeSpan EnsurePositive(TimeSpan value, string parameterName)
        {
            if (value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "The interval must be positive.");
            }

            return value;
        }
    }
}
