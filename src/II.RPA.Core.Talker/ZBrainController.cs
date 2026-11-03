using II.RPA.Core.Log;
using II.RPA.Core.Contracts.Talker.ZBrain;
using II.RPA.Core.Runtime;
using II.RPA.Core.Talker.Talkers;

namespace II.RPA.Core.Talker
{
    /// <summary>
    /// Owns ZBrain worker-selection and package-switching policy for the new Talker architecture.
    /// </summary>
    public sealed class ZBrainController : IZBrainController
    {
        private readonly ITalkerManager _talkerManager;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _packageSwitchGate = new(1, 1);

        public ZBrainController(ITalkerManager talkerManager, ILogger logger)
        {
            _talkerManager = talkerManager ?? throw new ArgumentNullException(nameof(talkerManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ZBrainWorkerSnapshot? GetCurrentWorker(bool childSession = false)
        {
            var talker = SelectTalker(childSession);
            return talker == null ? null : CreateSnapshot(talker);
        }

        public IReadOnlyCollection<ZBrainWorkerSnapshot> GetWorkers()
        {
            return _talkerManager
                .GetTalkers<ZBrainTalker>(readyOnly: false)
                .Select(CreateSnapshot)
                .ToArray();
        }

        public async Task<ZBrainPackageSwitchResult> SwitchPackageAsync(
            string packagePath,
            bool scan = true,
            bool childSession = false,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);
            await _packageSwitchGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var talker = SelectTalker(childSession);
                if (talker == null)
                {
                    return ZBrainPackageSwitchResult.Failure("-1", "无Z-Brain工作进程");
                }

                if (talker.State == ZBrainState.Terminated)
                {
                    return ZBrainPackageSwitchResult.Failure("-1", "当前Z-Brain工作进程已退出");
                }

                var checkResult = await talker
                    .CheckPlugin(packagePath, cancellationToken)
                    .ConfigureAwait(false);
                if (checkResult.IsSuccess)
                {
                    talker.AddPackageCache(packagePath);
                    return ZBrainPackageSwitchResult.Success();
                }

                if (!string.Equals(checkResult.Code, "0", StringComparison.Ordinal))
                {
                    return ZBrainPackageSwitchResult.Failure(
                        checkResult.Code,
                        checkResult.Message);
                }

                if (talker.PluginLoaded)
                {
                    _logger.LogInfo($"ZBrain[{talker.Id}] switching package; restart before loading {packagePath}");
                    await talker.RestartAsync(cancellationToken).ConfigureAwait(false);
                }

                var loadResult = await talker
                    .LoadPlugin(packagePath, scan, cancellationToken)
                    .ConfigureAwait(false);
                if (!loadResult.IsSuccess)
                {
                    return ZBrainPackageSwitchResult.Failure(
                        "-3",
                        string.IsNullOrWhiteSpace(loadResult.Body) ? "插件加载失败" : loadResult.Body);
                }

                return ZBrainPackageSwitchResult.Success();
            }
            finally
            {
                _packageSwitchGate.Release();
            }
        }

        public async Task<string?> ProxyAsync(
            ZBrainProxyRequestData request,
            bool childSession = false,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            var talker = SelectTalker(childSession);
            if (talker == null)
            {
                throw new InvalidOperationException("无Z-Brain工作进程");
            }

            if (talker.State == ZBrainState.Terminated)
            {
                throw new InvalidOperationException("当前Z-Brain工作进程已退出");
            }

            var response = await talker.ProxyTalk(new FrameRequest
            {
                Payload = new ZBrainRequest
                {
                    Method = request.Method,
                    Path = request.Path,
                    ContentType = request.ContentType,
                    Headers = request.Headers.ToDictionary(pair => pair.Key, pair => pair.Value),
                    Body = request.Body
                }
            }, cancellationToken).ConfigureAwait(false);
            return response?.Payload?.Body;
        }

        public async Task StopWorkerAsync(
            int? pid = null,
            bool childSession = false,
            CancellationToken cancellationToken = default)
        {
            var talker = pid.HasValue
                ? _talkerManager
                    .GetTalkers<ZBrainTalker>(readyOnly: false)
                    .FirstOrDefault(candidate => candidate.Pid == pid.Value)
                : SelectTalker(childSession);
            if (talker == null)
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();
            await talker.Stop().ConfigureAwait(false);
        }

        internal ZBrainTalker? SelectTalker(bool childSession)
        {
            return _talkerManager
                .GetTalkers<ZBrainTalker>(readyOnly: false)
                .Where(talker => talker.IsChildSession == childSession)
                .OrderByDescending(talker => talker.IsReady)
                .ThenBy(talker => talker.State == ZBrainState.Terminated)
                .ThenBy(talker => talker.StateChangedAt)
                .FirstOrDefault();
        }

        private static ZBrainWorkerSnapshot CreateSnapshot(ZBrainTalker talker)
        {
            return new ZBrainWorkerSnapshot
            {
                Id = talker.Id,
                Pid = talker.Pid > 0 ? talker.Pid : null,
                State = talker.State.ToString(),
                ProcessStatus = talker.ProcessStatus.ToString(),
                IsReady = talker.IsReady,
                IsChildSession = talker.IsChildSession,
                PluginLoaded = talker.PluginLoaded
            };
        }
    }
}
