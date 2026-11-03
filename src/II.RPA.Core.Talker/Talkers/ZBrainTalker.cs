using II.RPA.Core.Log;
using II.RPA.Core.Contracts.Talker;
using II.RPA.Core.Contracts.Talker.ZBrain;
using II.RPA.Core.Talker.Processes;
using II.RPA.Core.Talker.Models;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace II.RPA.Core.Talker.Talkers
{
    /// <summary>
    /// ZBrain 的显式状态与请求实现。状态只由进程事件、gRPC 注册和应用事件推进。
    /// </summary>
    public class ZBrainTalker : BaseTalker
    {
        private readonly ZBrainProcess _zbrainProcess;
        private readonly ConcurrentDictionary<string, byte> _packagePaths = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, byte> _loadingPaths = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _stateGate = new();
        private readonly SemaphoreSlim _restartGate = new(1, 1);
        private TaskCompletionSource<bool> _onlineCompletion = NewOnlineCompletion();
        private int _isLoading;
        private ZBrainState _state = ZBrainState.Idle;

        public ZBrainTalker(
            ITalkerManager talkerManager,
            ILogger logger,
            II.RPA.Core.Runtime.IRpaRuntimeContext runtimeContext) : base(logger, talkerManager)
        {
            _zbrainProcess = new ZBrainProcess(logger, runtimeContext);
            _zbrainProcess.OnProcessStatusChangedEvent += OnProcessStatusChanged;
        }

        public override string Name => "z_brain";

        public bool IsLoading => Volatile.Read(ref _isLoading) == 1;
        public bool LoadFromPackage => !_packagePaths.IsEmpty;
        public bool PluginLoaded => !_packagePaths.IsEmpty;
        public bool IsChildSession { get; set; }
        public string? LatestPackagePath { get; internal set; }
        public ProcessStatus ProcessStatus => _zbrainProcess.Status;
        public ZBrainState State
        {
            get
            {
                lock (_stateGate)
                {
                    return _state;
                }
            }
        }

        public override Task Start()
        {
            SetState(ZBrainState.Starting);
            _zbrainProcess.Start();
            return Task.CompletedTask;
        }

        public override async Task Stop()
        {
            SetState(ZBrainState.Stopping);
            await _zbrainProcess.StopAsync().ConfigureAwait(false);
            MarkOffline(new OperationCanceledException("ZBrain 已停止。"));
            await base.Stop().ConfigureAwait(false);
        }

        public async Task RestartAsync(CancellationToken cancellationToken = default)
        {
            await _restartGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await Stop().ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                _packagePaths.Clear();
                _loadingPaths.Clear();
                LatestPackagePath = null;
                Interlocked.Exchange(ref _isLoading, 0);
                await Start().ConfigureAwait(false);
            }
            finally
            {
                _restartGate.Release();
            }
        }

        protected override bool ChangeTalkerStatusToReady()
        {
            var ready = base.ChangeTalkerStatusToReady();
            if (ready)
            {
                SetState(ZBrainState.Online);
                _onlineCompletion.TrySetResult(true);
            }
            return ready;
        }

        protected override Task HandleFrameStream(FrameMessage frameMessage)
        {
            if (frameMessage is FrameMessage<ZBrainAppEvent> appEvent)
            {
                switch (appEvent.Payload.Type)
                {
                    case ZBrainAppEventType.Loading:
                        SetState(ZBrainState.Loading);
                        break;
                    case ZBrainAppEventType.Ready:
                        SetState(ZBrainState.Loaded);
                        break;
                    case ZBrainAppEventType.Broken:
                        SetState(ZBrainState.Broken);
                        break;
                }
            }
            return Task.CompletedTask;
        }

        protected override void OnConnectionClosed(Exception? exception)
        {
            ZBrainState nextState;
            lock (_stateGate)
            {
                if (_state is ZBrainState.Stopping or ZBrainState.Terminated)
                {
                    return;
                }

                _onlineCompletion = NewOnlineCompletion();
                nextState = _zbrainProcess.Status == ProcessStatus.Started
                    ? ZBrainState.Started
                    : ZBrainState.Terminated;
                _state = nextState;
            }
            Logger.LogInfo($"ZBrainTalker[{Id}] connection closed; state changed to {nextState}");
        }

        public async Task<(bool IsSuccess, string Code, string Message)> CheckPlugin(
            string packagePath,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);
            if (_packagePaths.ContainsKey(packagePath))
            {
                return (true, "-1", "命中流程包路径缓存");
            }

            await EnsureOnlineAsync(cancellationToken).ConfigureAwait(false);
            var command = new FrameCommand
            {
                Payload = new ZBrainCommand
                {
                    Type = ZBrainCommandType.CHECK_PLUGIN,
                    Arguments = new[] { packagePath }
                }
            };
            var response = await SendAsync(command, cancellationToken).ConfigureAwait(false) as FrameMessage<Reply<ReplyDict>>;
            if (response?.Payload?.Body == null || response.Code != 200)
            {
                return (false, "-3", "请求失败");
            }
            return (response.Payload.Body.Code == "-1", response.Payload.Body.Code, response.Payload.Body.Message);
        }

        public virtual async Task<(bool IsSuccess, string Body)> LoadPlugin(
            string packagePath,
            bool scan = false,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);
            await EnsureOnlineAsync(cancellationToken).ConfigureAwait(false);
            if (Interlocked.Exchange(ref _isLoading, 1) == 1)
            {
                Logger.LogWarning("已有 ZBrain 插件加载任务正在执行。");
            }

            _loadingPaths.TryAdd(packagePath, 0);
            LatestPackagePath = packagePath;
            try
            {
                SetState(ZBrainState.Loading);
                var command = new FrameCommand
                {
                    Payload = new ZBrainCommand
                    {
                        Type = ZBrainCommandType.LOAD_PLUGIN,
                        Arguments = new[] { packagePath, JsonConvert.SerializeObject(new { scan }) }
                    }
                };
                var response = await SendAsync(command, cancellationToken).ConfigureAwait(false) as FrameMessage<Reply<ReplyDict>>;
                var success = response?.Code == 200 && response.Payload?.Body != null;
                if (success)
                {
                    _packagePaths.TryAdd(packagePath, 0);
                }
                return (success, response?.Payload?.Body?.Message ?? string.Empty);
            }
            finally
            {
                Interlocked.Exchange(ref _isLoading, 0);
                _loadingPaths.TryRemove(packagePath, out _);
            }
        }

        public void AddPackageCache(string packagePath)
        {
            if (!string.IsNullOrWhiteSpace(packagePath))
            {
                _packagePaths.TryAdd(packagePath, 0);
            }
        }

        public virtual async Task<FrameMessage<Reply<string>>?> ProxyTalk(FrameMessage<ZBrainProxyRequest> request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            await EnsureOnlineAsync(cancellationToken).ConfigureAwait(false);
            return await SendAsync(request, cancellationToken).ConfigureAwait(false) as FrameMessage<Reply<string>>;
        }

        public virtual async Task<FrameMessage<Reply<string>>?> ProxyTalk(
            FrameRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            await EnsureOnlineAsync(cancellationToken).ConfigureAwait(false);
            return await SendAsync(request, cancellationToken).ConfigureAwait(false) as FrameMessage<Reply<string>>;
        }

        private async Task EnsureOnlineAsync(CancellationToken cancellationToken = default)
        {
            if (State is ZBrainState.Online or ZBrainState.Loading or ZBrainState.Loaded)
            {
                return;
            }
            await _onlineCompletion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        private void OnProcessStatusChanged(object? sender, ProcessStatus status)
        {
            if (status == ProcessStatus.Started)
            {
                SetState(ZBrainState.Started);
            }
            else if (status == ProcessStatus.Stopped)
            {
                MarkOffline(new IOException("ZBrain 进程已退出。"));
            }
        }

        private void MarkOffline(Exception exception)
        {
            TaskCompletionSource<bool> previousCompletion;
            lock (_stateGate)
            {
                if (_state == ZBrainState.Terminated)
                {
                    return;
                }

                _state = ZBrainState.Terminated;
                previousCompletion = _onlineCompletion;
                _onlineCompletion = NewOnlineCompletion();
            }
            Logger.LogInfo($"ZBrainTalker[{Id}] state changed to {ZBrainState.Terminated}");
            previousCompletion.TrySetException(exception);
        }

        private void SetState(ZBrainState state)
        {
            lock (_stateGate)
            {
                if (_state == state)
                {
                    return;
                }
                _state = state;
            }
            Logger.LogInfo($"ZBrainTalker[{Id}] 状态变更为 {state}");
        }

        private static TaskCompletionSource<bool> NewOnlineCompletion() =>
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
