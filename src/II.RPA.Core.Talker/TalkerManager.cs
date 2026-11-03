using Grpc.Core;
using II.RPA.Core.Abstract;
using II.RPA.Core.Log;
using II.RPA.Core.Runtime;
using II.RPA.Core.Talker.Talkers;
using Newtonsoft.Json.Linq;
using Rpa.Core.Worker;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;

namespace II.RPA.Core.Talker
{
    public class TalkerManager : ITalkerManager, IWorkerTalkerRegistrar, IRuntimeStateAccessor
    {
        public event Action<HostTalker>? HostConnected;

        private readonly string _processFileName;
        private readonly string _clientFileName;
        private readonly ConcurrentDictionary<string, ITalker> _talkersById = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, ITalker>> _talkersByType = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ITalker>> _talkersByRole =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, byte> _registrationReservations = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _registrationGates = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<Guid, ShareSubscription> _shareSubscriptions = new();
        private readonly ConcurrentDictionary<string, JObject> _share = new();
        private readonly ConcurrentDictionary<string, Channel<JObject>> _shareChannels = new();
        private readonly CancellationTokenSource _lifecycleCancellation = new();
        private readonly ILogger _logger;
        private readonly IRpaRuntimeContext _runtimeContext;
        private readonly IZBrainChildProxyLauncher? _childZBrainProxyLauncher;
        private readonly ZBrainController _zBrainController;
        private int _initializationStarted;
        private int _stopped;

        public TalkerManager(ILogger logger, IRpaRuntimeContext runtimeContext)
            : this(logger, runtimeContext, null, null)
        {
        }

        public TalkerManager(
            ILogger logger,
            IRpaRuntimeContext runtimeContext,
            IAdbStartupSignal? startupSignal)
            : this(logger, runtimeContext, startupSignal, null)
        {
        }

        public TalkerManager(
            ILogger logger,
            IRpaRuntimeContext runtimeContext,
            IAdbStartupSignal? startupSignal,
            IZBrainChildProxyLauncher? childZBrainProxyLauncher)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _runtimeContext = runtimeContext ?? throw new ArgumentNullException(nameof(runtimeContext));
            _childZBrainProxyLauncher = childZBrainProxyLauncher;
            _processFileName = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Z-Pid", "Z-Desktop.txt");
            _clientFileName = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Z-Core", "Client.txt");

            AddTalker(new HostTalker(this, logger, runtimeContext));
            var runtimeHostTalker = new RuntimeHostTalker(this, logger, runtimeContext);
            AddTalker(runtimeHostTalker);
            RuntimeHostController = new RuntimeHostController(runtimeHostTalker, logger, runtimeContext);

            var (zBrainCount, executorCount, interpreterCount) = GetConfiguredWorkerCounts();
            for (var index = 0; index < zBrainCount; index++)
            {
                AddTalker(new ZBrainTalker(this, logger, runtimeContext));
            }
            _zBrainController = new ZBrainController(this, logger);
            ZBrainController = _zBrainController;
            for (var index = 0; index < executorCount; index++)
            {
                AddTalker(new ExecutorTalker(this, logger, runtimeContext));
            }
            for (var index = 0; index < interpreterCount; index++)
            {
                AddTalker(new InterpreterTalker(this, logger, runtimeContext));
            }

            if (runtimeContext.GetConfiguration("-host-mobile", "HostMobile") != "false")
            {
                var mobileProviderTalker = new MobileProviderTalker(logger, runtimeContext);
                AddTalker(mobileProviderTalker);
                MobileProviderController = mobileProviderTalker;
            }
            else
            {
                MobileProviderController = DisabledMobileProviderController.Instance;
            }

            var shouldStartAdb = runtimeContext.LoopAdb &&
                runtimeContext.GetConfiguration("-loop-adb", "LoopAdb") != "false";
            if (shouldStartAdb)
            {
                AddTalker(new ADBTalker(logger, runtimeContext, this, startupSignal));
            }
            else
            {
                // ZBrain/Engine proxies share this one-time startup gate with ADB. When ADB is
                // intentionally disabled there is nothing to wait for, otherwise those proxies
                // would remain blocked for the entire Core lifetime.
                startupSignal?.MarkReady();
            }

            WorkerControlController = new CoreWorkerControlController(this, logger, runtimeContext);
        }

        public IRuntimeHostController RuntimeHostController { get; }

        public IZBrainController ZBrainController { get; }

        public IMobileProviderController MobileProviderController { get; }

        public IWorkerControlController WorkerControlController { get; }

        public void Initialization()
        {
            _ = InitializationAsync();
        }

        public async Task InitializationAsync(CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _initializationStarted, 1) == 1)
            {
                return;
            }

            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                _lifecycleCancellation.Token);

            var talkers = _talkersById.Values.ToArray();
            await Task.WhenAll(talkers.Select(talker => StartTalkerAsync(talker, linkedCancellation.Token)))
                .ConfigureAwait(false);
        }

        public void SetShare(string key, JObject value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(value);

            _logger.LogDebug($"SetShare:{key}");
            var changed = false;
            _share.AddOrUpdate(
                key,
                _ =>
                {
                    changed = true;
                    return value;
                },
                (_, oldValue) =>
                {
                    changed = !JToken.DeepEquals(oldValue, value);
                    return value;
                });

            if (changed)
            {
                PublishShareChange(new ShareChange(key, ShareChangeType.Set));
            }
        }

        public bool TryGetShare(string key, out JObject value)
        {
            return _share.TryGetValue(key, out value!);
        }

        public async Task<JObject> WaitShareAsync(
            string key,
            string? role = null,
            string? name = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (_share.TryGetValue(key, out var value))
                {
                    return (JObject)value.DeepClone();
                }

                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
            }
        }

        public IDisposable SubscribeShare(
            string? key,
            string? role,
            string? name,
            Action<ShareChange> callback)
        {
            ArgumentNullException.ThrowIfNull(callback);

            var subscriptionId = Guid.NewGuid();
            var subscription = new ShareSubscription(
                subscriptionId,
                key,
                role,
                name,
                callback,
                () => _shareSubscriptions.TryRemove(subscriptionId, out _));
            if (!_shareSubscriptions.TryAdd(subscriptionId, subscription))
            {
                throw new InvalidOperationException($"Share subscription [{subscriptionId}] registration failed.");
            }

            return subscription;
        }

        public JObject? RemoveShare(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            if (!_share.TryRemove(key, out var value))
            {
                return null;
            }

            PublishShareChange(new ShareChange(key, ShareChangeType.Unset));
            return value;
        }

        public IReadOnlyCollection<string> GetShareKeys()
            => _share.Keys.ToArray();

        public bool ContainsShare(string key)
            => _share.ContainsKey(key);

        private void PublishShareChange(ShareChange change)
        {
            foreach (var subscription in _shareSubscriptions.Values)
            {
                if (!subscription.Matches(change))
                {
                    continue;
                }

                try
                {
                    subscription.Callback(change);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Share subscription callback failed: key={change.Key}", ex);
                }
            }
        }

        internal JObject CreateShareSnapshot()
        {
            var snapshot = new JObject();
            foreach (var pair in _share)
            {
                snapshot[pair.Key] = pair.Value.DeepClone();
            }
            return snapshot;
        }

        bool IRuntimeStateAccessor.TryGetSharedJson(string key, out string json)
        {
            if (_share.TryGetValue(key, out var value))
            {
                json = value.ToString(Newtonsoft.Json.Formatting.None);
                return true;
            }

            json = string.Empty;
            return false;
        }

        public void WriteShareChannel(string key, JObject value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(value);
            var channel = _shareChannels.GetOrAdd(key, _ => System.Threading.Channels.Channel.CreateUnbounded<JObject>(
                new UnboundedChannelOptions { SingleReader = false, SingleWriter = false }));
            if (!channel.Writer.TryWrite(value))
            {
                throw new InvalidOperationException($"无法写入 ShareChannel [{key}]");
            }

            PublishShareChange(new ShareChange(key, ShareChangeType.Write));
        }

        public JObject? ReadShareChannel(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            var channel = _shareChannels.GetOrAdd(key, _ => System.Threading.Channels.Channel.CreateUnbounded<JObject>(
                new UnboundedChannelOptions { SingleReader = false, SingleWriter = false }));
            if (!channel.Reader.TryRead(out var value))
            {
                return null;
            }

            PublishShareChange(new ShareChange(key, ShareChangeType.Read));
            return value;
        }

        public int CountShareChannel(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            var channel = _shareChannels.GetOrAdd(key, _ => System.Threading.Channels.Channel.CreateUnbounded<JObject>(
                new UnboundedChannelOptions { SingleReader = false, SingleWriter = false }));
            return channel.Reader.Count;
        }

        public async ValueTask<JObject> ReadShareChannelAsync(string key, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            var channel = _shareChannels.GetOrAdd(key, _ => System.Threading.Channels.Channel.CreateUnbounded<JObject>(
                new UnboundedChannelOptions { SingleReader = false, SingleWriter = false }));
            var value = await channel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            PublishShareChange(new ShareChange(key, ShareChangeType.Read));
            return value;
        }

        public async Task RegisterTalker(
            IAsyncStreamReader<FrameStream> requestStream,
            IServerStreamWriter<FrameStream> responseStream,
            ServerCallContext context)
        {
            ArgumentNullException.ThrowIfNull(requestStream);
            ArgumentNullException.ThrowIfNull(responseStream);
            ArgumentNullException.ThrowIfNull(context);

            var meta = ParseWorkerMeta(context.RequestHeaders);
            var registrationRole = NormalizeRegistrationRole(meta.Role, meta.Name);
            var peer = TryGetPeer(context);
            _logger.LogInfo($"来自[{peer}]的Worker注册请求, 请求头\n{FormatHeaders(context.RequestHeaders)}");

            if (string.IsNullOrWhiteSpace(registrationRole))
            {
                throw NotFound("Worker 注册请求缺少 role");
            }

            var roleGate = _registrationGates.GetOrAdd(registrationRole, _ => new SemaphoreSlim(1, 1));
            ITalker talker;
            var reserved = false;
            await roleGate.WaitAsync(context.CancellationToken).ConfigureAwait(false);
            try
            {
                talker = ResolveTalker(meta, registrationRole, out reserved);
                try
                {
                    if (talker is BaseTalker baseTalker)
                    {
                        baseTalker.PrepareRegistration(meta.Pid);
                    }
                    ConfigureTalkerFromWorker(talker, meta);

                    _logger.LogInfo($"向Talker [{talker.Id}] [{talker.Name}] [{meta.Pid}] 注册连接");
                    await context.WriteResponseHeadersAsync(new Metadata
                    {
                        { "pid", Process.GetCurrentProcess().Id.ToString() },
                        { "id", talker.Id }
                    }).ConfigureAwait(false);
                }
                catch
                {
                    if (reserved)
                    {
                        _registrationReservations.TryRemove(talker.Id, out _);
                    }
                    throw;
                }
            }
            finally
            {
                roleGate.Release();
            }

            Task registrationTask;
            try
            {
                // RegisterTalker establishes readiness synchronously before its read-loop awaits.
                registrationTask = talker.RegisterTalker(requestStream, responseStream, context);
                if (talker is HostTalker host && host.IsReady)
                {
                    try
                    {
                        HostConnected?.Invoke(host);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Host connection observer failed", ex);
                    }
                }
                if (talker is ZBrainTalker zBrainTalker && zBrainTalker.IsChildSession)
                {
                    _ = InheritMainZbrainPackageAsync(zBrainTalker);
                }
            }
            catch
            {
                if (reserved)
                {
                    _registrationReservations.TryRemove(talker.Id, out _);
                }
                throw;
            }

            if (reserved)
            {
                _registrationReservations.TryRemove(talker.Id, out _);
            }

            var registrationGeneration = talker is BaseTalker registeredBaseTalker
                ? registeredBaseTalker.ConnectionGeneration
                : 0;

            try
            {
                await registrationTask.ConfigureAwait(false);
            }
            finally
            {
                var isCurrentConnection = talker is not BaseTalker baseTalker ||
                    baseTalker.ConnectionGeneration == registrationGeneration;
                if (meta.Temporary && isCurrentConnection)
                {
                    RemoveTalker(talker);
                    await SafeStopTalkerAsync(talker).ConfigureAwait(false);
                }
            }
        }

        Task IWorkerTalkerRegistrar.RegisterTalker(
            object requestStream,
            object responseStream,
            ServerCallContext context)
        {
            if (requestStream is not IAsyncStreamReader<FrameStream> typedRequestStream ||
                responseStream is not IServerStreamWriter<FrameStream> typedResponseStream)
            {
                throw new ArgumentException("Worker stream type must be FrameStream.");
            }

            return RegisterTalker(typedRequestStream, typedResponseStream, context);
        }

        Task IWorkerTalkerRegistrar.RegisterChunkTalker(
            object requestStream,
            object responseStream,
            ServerCallContext context)
        {
            if (requestStream is not IAsyncStreamReader<ChunkFrameStream> typedRequestStream ||
                responseStream is not IServerStreamWriter<ChunkFrameStream> typedResponseStream)
            {
                throw new ArgumentException("Worker stream type must be ChunkFrameStream.");
            }

            return RegisterTalker(
                new ChunkFrameStreamReader(typedRequestStream),
                new ChunkFrameStreamWriter(typedResponseStream),
                context);
        }

        public T GetTalker<T>() where T : ITalker
        {
            if (TryGetTalker<T>(out var talker))
            {
                return talker;
            }

            throw new InvalidOperationException($"{typeof(T).Name}未注册或尚未准备完成");
        }

        public bool TryGetTalker<T>(out T talker) where T : ITalker
        {
            talker = GetTalkers<T>()
                .OrderBy(candidate => candidate.StateChangedAt)
                .FirstOrDefault()!;
            return talker != null;
        }

        public IReadOnlyCollection<T> GetTalkers<T>(bool readyOnly = true) where T : ITalker
        {
            return _talkersById.Values
                .OfType<T>()
                .Where(talker => !readyOnly || talker.IsReady)
                .OrderBy(talker => talker.StateChangedAt)
                .ToArray();
        }

        public async Task<T> WaitTalker<T>(TimeSpan timeout, CancellationToken cancellationToken = default) where T : ITalker
        {
            var startedAt = Stopwatch.GetTimestamp();
            while (Stopwatch.GetElapsedTime(startedAt) < timeout)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (TryGetTalker<T>(out var talker))
                {
                    return talker;
                }
                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
            }

            throw new TimeoutException($"等待 Talker [{typeof(T).Name}] 就绪超时：{timeout}");
        }

        public async Task<T> WaitTalker<T>(Predicate<T> predicate, CancellationToken cancellationToken = default) where T : ITalker
        {
            ArgumentNullException.ThrowIfNull(predicate);
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var talker = GetTalkers<T>(readyOnly: false)
                    .OrderByDescending(candidate => candidate.IsReady)
                    .ThenBy(candidate => candidate.StateChangedAt)
                    .FirstOrDefault(candidate => predicate(candidate));
                if (talker != null)
                {
                    return talker;
                }

                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
            }
        }

        public void StartChildZBrainProxy()
        {
            if (_childZBrainProxyLauncher == null)
            {
                _logger.LogWarning("未注入 IZBrainChildProxyLauncher，跳过子会话ZBrain代理启动");
                return;
            }

            _logger.LogInfo("启动子会话ZBrain代理");
            _childZBrainProxyLauncher.StartChildProxy();
        }

        public void StopChildZBrainProxy()
        {
            if (_childZBrainProxyLauncher == null)
            {
                _logger.LogWarning("未注入 IZBrainChildProxyLauncher，跳过子会话ZBrain代理停止");
                return;
            }

            _logger.LogInfo("停止子会话ZBrain代理");
            _childZBrainProxyLauncher.StopChildProxy();
        }

        public void AddTalker(ITalker talker)
        {
            ArgumentNullException.ThrowIfNull(talker);
            if (!_talkersById.TryAdd(talker.Id, talker))
            {
                throw new InvalidOperationException($"Talker Id [{talker.Id}] 已存在");
            }

            var typeMap = _talkersByType.GetOrAdd(talker.GetType(), _ =>
                new ConcurrentDictionary<string, ITalker>(StringComparer.OrdinalIgnoreCase));
            var roleMap = _talkersByRole.GetOrAdd(NormalizeRole(talker.Name), _ =>
                new ConcurrentDictionary<string, ITalker>(StringComparer.OrdinalIgnoreCase));

            if (!typeMap.TryAdd(talker.Id, talker) || !roleMap.TryAdd(talker.Id, talker))
            {
                _talkersById.TryRemove(talker.Id, out _);
                typeMap.TryRemove(talker.Id, out _);
                roleMap.TryRemove(talker.Id, out _);
                throw new InvalidOperationException($"Talker [{talker.Name}][{talker.Id}] 注册索引失败");
            }
        }

        public bool RemoveTalker(ITalker talker)
        {
            ArgumentNullException.ThrowIfNull(talker);
            var removed = _talkersById.TryRemove(talker.Id, out _);
            if (_talkersByType.TryGetValue(talker.GetType(), out var typeMap))
            {
                typeMap.TryRemove(talker.Id, out _);
            }
            if (_talkersByRole.TryGetValue(NormalizeRole(talker.Name), out var roleMap))
            {
                roleMap.TryRemove(talker.Id, out _);
            }
            _registrationReservations.TryRemove(talker.Id, out _);
            return removed;
        }

        public async Task StopAsync()
        {
            if (Interlocked.Exchange(ref _stopped, 1) == 1)
            {
                return;
            }

            _lifecycleCancellation.Cancel();
            var talkers = _talkersById.Values.ToArray();
            await Task.WhenAll(talkers.Select(SafeStopTalkerAsync)).ConfigureAwait(false);

            _talkersById.Clear();
            _talkersByType.Clear();
            _talkersByRole.Clear();
            _registrationReservations.Clear();
            foreach (var subscription in _shareSubscriptions.Values)
            {
                subscription.Dispose();
            }
            _shareSubscriptions.Clear();
            foreach (var channel in _shareChannels.Values)
            {
                channel.Writer.TryComplete();
            }
            _shareChannels.Clear();
        }

        public (bool, int) CheckZDesktopAllow()
        {
            var pid = -1;
            try
            {
                if (File.Exists(_processFileName) &&
                    int.TryParse(File.ReadAllText(_processFileName), out var processId))
                {
                    using var process = Process.GetProcessById(processId);
                    pid = process.Id;
                }

                if (pid > 0 && File.Exists(_clientFileName))
                {
                    var clientString = File.ReadAllText(_clientFileName);
                    if (!string.IsNullOrEmpty(clientString) && clientString != _runtimeContext.ClientId)
                    {
                        return (false, pid);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("检查 ZDesktop 占用状态失败", ex);
            }
            return (true, 0);
        }

        private ITalker ResolveTalker(WorkerRegistrationMeta meta, string registrationRole, out bool reserved)
        {
            reserved = false;
            if (meta.TalkerId != Guid.Empty)
            {
                if (!_talkersById.TryGetValue(meta.TalkerId.ToString(), out var matchedById) ||
                    !TalkerMatchesRegistrationRole(matchedById, registrationRole))
                {
                    throw NotFound($"无法找到 talker-id=[{meta.TalkerId}] 对应的 Talker");
                }
                return matchedById;
            }

            if (_talkersByRole.TryGetValue(registrationRole, out var roleTalkers))
            {
                var candidates = roleTalkers.Values
                    .Where(talker => !_registrationReservations.ContainsKey(talker.Id))
                    .ToArray();

                var candidate = candidates.FirstOrDefault(talker => meta.Pid > 0 && talker.Pid == meta.Pid)
                    ?? candidates.FirstOrDefault(talker => !talker.IsReady);
                if (candidate != null)
                {
                    reserved = _registrationReservations.TryAdd(candidate.Id, 0);
                    if (reserved)
                    {
                        return candidate;
                    }
                }
            }

            var created = CreateDynamicTalker(registrationRole);
            if (created == null)
            {
                throw NotFound($"无法找到 role=[{meta.Role}] name=[{meta.Name}] 对应的可用 Talker");
            }

            AddTalker(created);
            reserved = _registrationReservations.TryAdd(created.Id, 0);
            return created;
        }

        private ITalker? CreateDynamicTalker(string registrationRole)
        {
            return registrationRole switch
            {
                "host" => new HostTalker(this, _logger, _runtimeContext),
                "hostcoreserver" => new HostCoreServerClientTalker(this, _logger),
                "executor" => new ExecutorTalker(this, _logger, _runtimeContext),
                "interpreter" => new InterpreterTalker(this, _logger, _runtimeContext),
                "z_brain" => new ZBrainTalker(this, _logger, _runtimeContext),
                _ => null
            };
        }

        internal void ConfigureTalkerFromWorker(ITalker talker, WorkerRegistrationMeta meta)
        {
            if (talker is not ZBrainTalker zBrainTalker || meta.Pid <= 0)
            {
                return;
            }

            try
            {
                using var process = Process.GetProcessById(meta.Pid);
                zBrainTalker.IsChildSession = meta.IsChild || _runtimeContext.SessionId != process.SessionId;
            }
            catch (Exception ex)
            {
                zBrainTalker.IsChildSession = meta.IsChild;
                _logger.LogDebug($"读取 ZBrain[{meta.Pid}] Session 失败", ex);
            }

            if (zBrainTalker.IsChildSession)
            {
                StartChildZBrainProxy();
            }
        }

        internal async Task InheritMainZbrainPackageAsync(ZBrainTalker childZBrain)
        {
            string? packagePath = null;
            try
            {
                var mainZBrain = _zBrainController.SelectTalker(childSession: false);
                if (mainZBrain == null)
                {
                    _logger.LogDebug("未找到主会话ZBrain，跳过子会话插件包继承");
                    return;
                }

                packagePath = mainZBrain.LatestPackagePath;
                if (string.IsNullOrWhiteSpace(packagePath))
                {
                    _logger.LogDebug("主会话ZBrain尚未加载插件包，跳过继承");
                    return;
                }

                _logger.LogInfo($"子会话ZBrain[{childZBrain.Id}]继承主ZBrain插件包:{packagePath}");
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await childZBrain.LoadPlugin(packagePath, cancellationToken: timeout.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"子会话ZBrain[{childZBrain.Id}]继承主ZBrain插件包失败，packagePath={packagePath}", ex);
            }
        }

        private async Task StartTalkerAsync(ITalker talker, CancellationToken cancellationToken)
        {
            const int maximumAttempts = 3;
            for (var attempt = 1; attempt <= maximumAttempts; attempt++)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await talker.Start().ConfigureAwait(false);
                    return;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex) when (IsMissingExecutable(ex))
                {
                    _logger.LogError($"启动 Talker [{talker.Name}][{talker.Id}] 失败：找不到可执行文件，放弃重试", ex);
                    return;
                }
                catch (Exception ex)
                {
                    if (attempt == maximumAttempts)
                    {
                        _logger.LogError($"启动 Talker [{talker.Name}][{talker.Id}] 失败，已达最大重试次数", ex);
                        return;
                    }

                    var retryDelay = TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt - 1));
                    _logger.LogWarning(
                        $"启动 Talker [{talker.Name}][{talker.Id}] 失败，{retryDelay.TotalMilliseconds:0}ms 后进行第 {attempt + 1} 次尝试",
                        ex);
                    await Task.Delay(retryDelay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private static bool IsMissingExecutable(Exception exception)
        {
            for (var current = exception; current != null; current = current.InnerException)
            {
                if (current is FileNotFoundException or DirectoryNotFoundException)
                {
                    return true;
                }

                if (current is System.ComponentModel.Win32Exception win32Exception &&
                    win32Exception.NativeErrorCode is 2 or 3)
                {
                    return true;
                }
            }

            return false;
        }

        private async Task SafeStopTalkerAsync(ITalker talker)
        {
            try
            {
                await talker.Stop().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError($"停止 Talker [{talker.Name}][{talker.Id}] 失败", ex);
            }
        }

        private static WorkerRegistrationMeta ParseWorkerMeta(Metadata headers)
        {
            int.TryParse(headers.FirstOrDefault(kv => kv.Key == "pid")?.Value, out var pid);
            Guid.TryParse(headers.FirstOrDefault(kv => kv.Key == "talker-id")?.Value, out var talkerId);
            return new WorkerRegistrationMeta(
                pid,
                talkerId,
                headers.FirstOrDefault(kv => kv.Key == "role")?.Value,
                headers.FirstOrDefault(kv => kv.Key == "name")?.Value,
                headers.FirstOrDefault(kv => kv.Key == "shadow")?.Value == "true",
                headers.FirstOrDefault(kv => kv.Key == "temporary")?.Value == "true",
                headers.FirstOrDefault(kv => kv.Key == "child")?.Value == "true");
        }

        private static string NormalizeRegistrationRole(string? role, string? name)
        {
            if (string.Equals(name, "hostcoreserver", StringComparison.OrdinalIgnoreCase))
            {
                return "hostcoreserver";
            }
            return NormalizeRole(role);
        }

        private static string NormalizeRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return string.Empty;
            }

            return role.Trim().ToLowerInvariant() switch
            {
                "z_desktop" => "zdesktop",
                "zbrain" => "z_brain",
                var normalized => normalized
            };
        }

        private static bool TalkerMatchesRegistrationRole(ITalker talker, string registrationRole)
            => string.Equals(NormalizeRole(talker.Name), registrationRole, StringComparison.OrdinalIgnoreCase);

        private static string TryGetPeer(ServerCallContext context)
        {
            try
            {
                return context.Peer ?? "unknown";
            }
            catch
            {
                return "unknown";
            }
        }

        private static RpcException NotFound(string message)
            => new(new Status(StatusCode.NotFound, message));

        private static string FormatHeaders(Metadata headers)
        {
            return string.Join("\n", headers.Select(header =>
            {
                try
                {
                    return header.IsBinary ? $"{header.Key}: <binary>" : $"{header.Key}: {header.Value}";
                }
                catch
                {
                    return $"{header.Key}: <unavailable>";
                }
            }));
        }

        private (int ZBrain, int Executor, int Interpreter) GetConfiguredWorkerCounts()
        {
            var zBrain = 0;
            var executor = 0;
            var interpreter = 0;

            if (_runtimeContext.ClientKind == RpaClientKind.Factory)
            {
                zBrain = executor = interpreter = 1;
            }
            else if (_runtimeContext.ClientKind == RpaClientKind.Bot)
            {
                zBrain = executor = 1;
            }
            else if (_runtimeContext.ClientKind == RpaClientKind.Desktop)
            {
                zBrain = executor = 1;
                interpreter = string.Equals(_runtimeContext.RootClientId, "Z-Factory", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            }

            zBrain = ReadConfiguredCount("-brain-num", "ZBrainNum", zBrain);
            executor = ReadConfiguredCount("-executor-num", "ExecutorNum", executor);
            interpreter = ReadConfiguredCount("-interpreter-num", "InterpreterNum", interpreter);
            return (zBrain, executor, interpreter);
        }

        private int ReadConfiguredCount(string commandLineName, string configName, int fallback)
        {
            return int.TryParse(_runtimeContext.GetConfiguration(commandLineName, configName), out var configured)
                ? Math.Max(0, configured)
                : fallback;
        }

        internal sealed record WorkerRegistrationMeta(
            int Pid,
            Guid TalkerId,
            string? Role,
            string? Name,
            bool Shadow,
            bool Temporary,
            bool IsChild);

        private sealed class ShareSubscription : IDisposable
        {
            private readonly string? _key;
            private readonly string? _role;
            private readonly string? _name;
            private readonly Action _remove;
            private int _disposed;

            public ShareSubscription(
                Guid id,
                string? key,
                string? role,
                string? name,
                Action<ShareChange> callback,
                Action remove)
            {
                Id = id;
                _key = key;
                _role = role;
                _name = name;
                Callback = callback;
                _remove = remove;
            }

            public Guid Id { get; }

            public Action<ShareChange> Callback { get; }

            public bool Matches(ShareChange change)
            {
                if (!string.IsNullOrWhiteSpace(_key) &&
                    !string.Equals(_key, change.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(_role) &&
                    !string.Equals(_role, change.Role, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(_name) &&
                    !string.Equals(_name, change.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                return Volatile.Read(ref _disposed) == 0;
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                {
                    _remove();
                }
            }
        }

        private sealed class DisabledMobileProviderController : IMobileProviderController
        {
            public static DisabledMobileProviderController Instance { get; } = new();

            public bool IsEnabled => false;

            public Task<MobilePluginInvocationResult> InvokeAsync(
                string action,
                string input,
                int timeout,
                CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("MobileProvider 已通过 HostMobile=false 禁用。");
            }
        }

        private sealed class ChunkFrameStreamReader : IAsyncStreamReader<FrameStream>
        {
            private readonly IAsyncStreamReader<ChunkFrameStream> _reader;
            private readonly Queue<FrameStream> _pendingFrames = new();

            public ChunkFrameStreamReader(IAsyncStreamReader<ChunkFrameStream> reader)
            {
                _reader = reader;
            }

            public FrameStream Current { get; private set; } = null!;

            public async Task<bool> MoveNext(CancellationToken cancellationToken)
            {
                while (_pendingFrames.Count == 0)
                {
                    if (!await _reader.MoveNext(cancellationToken).ConfigureAwait(false))
                    {
                        return false;
                    }

                    foreach (var frame in _reader.Current.Frames)
                    {
                        _pendingFrames.Enqueue(frame);
                    }
                }

                Current = _pendingFrames.Dequeue();
                return true;
            }
        }

        private sealed class ChunkFrameStreamWriter : IServerStreamWriter<FrameStream>
        {
            private readonly IServerStreamWriter<ChunkFrameStream> _writer;

            public ChunkFrameStreamWriter(IServerStreamWriter<ChunkFrameStream> writer)
            {
                _writer = writer;
            }

            public WriteOptions? WriteOptions
            {
                get => _writer.WriteOptions;
                set => _writer.WriteOptions = value;
            }

            public Task WriteAsync(FrameStream message)
            {
                var chunk = new ChunkFrameStream();
                chunk.Frames.Add(message);
                return _writer.WriteAsync(chunk);
            }
        }
    }
}
