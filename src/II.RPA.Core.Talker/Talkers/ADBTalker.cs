using II.RPA.Core.Abstract;
using II.RPA.Core.Contracts.Talker;
using II.RPA.Core.Log;
using II.RPA.Core.Runtime;
using II.RPA.Core.Talker.Models;
using II.RPA.Core.Talker.Processes;
using Newtonsoft.Json.Linq;
using System.Text;

namespace II.RPA.Core.Talker.Talkers
{
    public class ADBTalker : BaseTalker
    {
        private const int StartupFailureThreshold = 3;
        private const int MaxProbeIntervalSeconds = 3600;
        private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan DefaultProbeInterval = TimeSpan.FromSeconds(5);

        private readonly ADBProcess _adbProcess;
        private readonly ITalkerManager? _talkerManager;
        private readonly IAdbStartupSignal? _startupSignal;
        private readonly IRpaRuntimeContext _runtimeContext;
        private readonly bool _ownsServer;
        private readonly TimeSpan _probeInterval;
        private readonly object _lifecycleGate = new();

        private CancellationTokenSource? _loopCancellation;
        private Task? _loopTask;
        private TaskCompletionSource<bool>? _initializationCompletion;
        private int _adbReadyMarked;
        private int _successfulProbeCount;
        private long _lastSuccessfulProbeUtcTicks;

        public ADBTalker(
            ILogger logger,
            IRpaRuntimeContext runtimeContext,
            ITalkerManager? talkerManager = null,
            IAdbStartupSignal? startupSignal = null) : base(logger)
        {
            _runtimeContext = runtimeContext ?? throw new ArgumentNullException(nameof(runtimeContext));
            _talkerManager = talkerManager;
            _startupSignal = startupSignal;
            _adbProcess = new ADBProcess(logger, runtimeContext);
            _ownsServer = string.Equals(
                runtimeContext.GetConfiguration("-adb-own-server", "AdbOwnServer"),
                "true",
                StringComparison.OrdinalIgnoreCase);
            _probeInterval = ResolveProbeInterval(runtimeContext);
        }

        public override string Name => "ADB";

        public string AdbExecutablePath => _adbProcess.FilePath;

        public int? ServerPort => _adbProcess.ServerPort;

        public int SuccessfulProbeCount => Volatile.Read(ref _successfulProbeCount);

        public DateTimeOffset? LastSuccessfulProbeAt
        {
            get
            {
                var ticks = Interlocked.Read(ref _lastSuccessfulProbeUtcTicks);
                return ticks == 0 ? null : new DateTimeOffset(ticks, TimeSpan.Zero);
            }
        }

        public override async Task Start()
        {
            Task<bool> initializationTask;
            lock (_lifecycleGate)
            {
                if (_loopTask is { IsCompleted: false })
                {
                    initializationTask = _initializationCompletion?.Task ?? Task.FromResult(IsReady);
                }
                else
                {
                    try
                    {
                        _adbProcess.EnsureInstalled();
                    }
                    catch (Exception ex)
                    {
                        TransitionTo(TalkerLifecycleState.Faulted, ex);
                        MarkAdbReadyOnce();
                        throw;
                    }

                    TransitionTo(TalkerLifecycleState.Starting);
                    _loopCancellation?.Dispose();
                    _loopCancellation = new CancellationTokenSource();
                    _initializationCompletion = new TaskCompletionSource<bool>(
                        TaskCreationOptions.RunContinuationsAsynchronously);
                    _loopTask = Task.Run(() => RunLoopAsync(
                        _initializationCompletion,
                        _loopCancellation.Token));
                    initializationTask = _initializationCompletion.Task;
                }
            }

            // A successful first `adb devices` probe makes the Talker Ready. After a bounded
            // number of failures the loop remains alive and Faulted, while the one-time proxy
            // gate is released so ZBrain/Engine cannot be blocked for the entire Core lifetime.
            await initializationTask.ConfigureAwait(false);
        }

        public override async Task Stop()
        {
            Task? loopTask;
            CancellationTokenSource? loopCancellation;
            lock (_lifecycleGate)
            {
                loopCancellation = _loopCancellation;
                loopCancellation?.Cancel();
                loopTask = _loopTask;
            }

            await _adbProcess.StopAsync().ConfigureAwait(false);
            if (loopTask != null)
            {
                try
                {
                    await loopTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }

            if (_ownsServer && File.Exists(_adbProcess.FilePath))
            {
                try
                {
                    await RunAdbCommandAsync(
                        "kill-server",
                        line => Logger.LogDebug($"adb kill-server: {line}"),
                        CancellationToken.None,
                        TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("停止独立 adb server 失败", ex);
                }
            }

            lock (_lifecycleGate)
            {
                if (ReferenceEquals(_loopCancellation, loopCancellation))
                {
                    _loopCancellation = null;
                    _loopTask = null;
                    _initializationCompletion = null;
                }
            }
            loopCancellation?.Dispose();
            await base.Stop().ConfigureAwait(false);
        }

        protected override Task StartReadStream() => Task.CompletedTask;

        internal void MarkAdbReadyOnce()
        {
            if (_startupSignal != null && Interlocked.Exchange(ref _adbReadyMarked, 1) == 0)
            {
                _startupSignal.MarkReady();
            }
        }

        private async Task RunLoopAsync(
            TaskCompletionSource<bool> initializationCompletion,
            CancellationToken cancellationToken)
        {
            var tag = _runtimeContext.ClientKind == RpaClientKind.Bot ? "ZBOT_SMS" : "ZFACTORY_SMS";
            var startupFailures = 0;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var output = new List<string>();
                        await RunAdbCommandAsync(
                            "devices",
                            line => output.Add(line),
                            cancellationToken,
                            CommandTimeout,
                            quiet: true).ConfigureAwait(false);

                        Interlocked.Increment(ref _successfulProbeCount);
                        Interlocked.Exchange(ref _lastSuccessfulProbeUtcTicks, DateTimeOffset.UtcNow.UtcTicks);
                        if (!IsReady)
                        {
                            EstablishConnection();
                        }
                        MarkAdbReadyOnce();
                        initializationCompletion.TrySetResult(true);

                        var outputs = output.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
                        if (outputs.Length > 1 && !string.IsNullOrWhiteSpace(outputs[1]))
                        {
                            Logger.LogInfo($"{outputs[1]} 已链接");
                            var message = new StringBuilder();
                            await RunAdbCommandAsync(
                                $"logcat [{tag}]:W *:S",
                                line => ParseLine(line, message),
                                cancellationToken).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        MarkDisconnected(ex);
                        Logger.LogError("ADB 探测或日志监听失败，将继续重试", ex);
                        if (!initializationCompletion.Task.IsCompleted &&
                            ++startupFailures >= StartupFailureThreshold)
                        {
                            MarkAdbReadyOnce();
                            initializationCompletion.TrySetResult(false);
                        }
                    }

                    await Task.Delay(_probeInterval, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            finally
            {
                if (!initializationCompletion.Task.IsCompleted)
                {
                    initializationCompletion.TrySetCanceled(cancellationToken);
                }
            }
        }

        private async Task RunAdbCommandAsync(
            string arguments,
            Action<string> onOutput,
            CancellationToken cancellationToken,
            TimeSpan? timeout = null,
            bool quiet = false)
        {
            var completed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var errors = new List<string>();
            void HandleOutput(string line) => onOutput(line);
            void HandleError(string line) => errors.Add(line);
            void HandleStatus(object? _, ProcessStatus status)
            {
                if (status == ProcessStatus.Stopped)
                {
                    completed.TrySetResult(true);
                }
            }

            _adbProcess.OutputDataReceived += HandleOutput;
            _adbProcess.ErrorDataReceived += HandleError;
            _adbProcess.OnProcessStatusChangedEvent += HandleStatus;
            try
            {
                _adbProcess.Start(arguments, quiet);
                try
                {
                    if (timeout.HasValue)
                    {
                        await completed.Task.WaitAsync(timeout.Value, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await completed.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (TimeoutException)
                {
                    await _adbProcess.StopAsync().ConfigureAwait(false);
                    throw new TimeoutException($"adb {arguments} 在 {timeout.Value.TotalSeconds:0} 秒内未退出。");
                }

                if (_adbProcess.LastExitCode is { } exitCode && exitCode != 0)
                {
                    throw new InvalidOperationException(
                        $"adb {arguments} 退出码为 {exitCode}: {string.Join(Environment.NewLine, errors)}");
                }
            }
            finally
            {
                _adbProcess.OutputDataReceived -= HandleOutput;
                _adbProcess.ErrorDataReceived -= HandleError;
                _adbProcess.OnProcessStatusChangedEvent -= HandleStatus;
            }
        }

        private static TimeSpan ResolveProbeInterval(IRpaRuntimeContext runtimeContext)
        {
            // Every probe spawns a short-lived adb.exe child process, so on small hosts the default
            // cadence mostly produces log noise. Operators can stretch it without a rebuild.
            var configured = runtimeContext.GetConfiguration("-adb-probe-interval", "AdbProbeInterval");
            if (string.IsNullOrWhiteSpace(configured))
            {
                return DefaultProbeInterval;
            }

            if (!int.TryParse(configured, out var seconds) || seconds <= 0 || seconds > MaxProbeIntervalSeconds)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(runtimeContext),
                    configured,
                    "AdbProbeInterval 必须是 1 到 3600 之间的整数（秒）。");
            }

            return TimeSpan.FromSeconds(seconds);
        }

        private void ParseLine(string line, StringBuilder message)
        {
            Logger.LogInfo($"logOutputs:{line}");
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            var raw = line.Trim();
            if (new[]
            {
                "--------- beginning of system",
                "--------- beginning of main",
                "--------- beginning of crash",
            }.Contains(raw) || raw.Contains("SMS-START"))
            {
                return;
            }

            var splits = raw.Split(':');
            if (splits.Length > 1)
            {
                message.Append(splits[^1].Replace(" ", string.Empty).Replace("\r", string.Empty));
            }

            if (raw.Contains("SMS-END"))
            {
                var content = message.ToString();
                const string smsEndTag = "SMS-END";
                if (content.StartsWith(smsEndTag))
                {
                    content = content.Substring(smsEndTag.Length);
                }
                if (content.EndsWith(smsEndTag))
                {
                    content = content.Substring(0, content.Length - smsEndTag.Length);
                }
                Logger.LogInfo($"获得数据: {content}");
                _talkerManager?.WriteShareChannel(PredefineChannel.MobileMessageQueueName, JObject.FromObject(new
                {
                    data = content
                }));
                message.Clear();
            }
        }
    }
}
