using II.RPA.Core.Log;
using II.RPA.Core.Runtime;
using II.RPA.Core.Talker.Talkers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Core.Talker
{
    /// <summary>
    /// Application-facing RuntimeHost controller. It owns orchestration policy while the Talker
    /// remains responsible for one process/connection and its wire protocol.
    /// </summary>
    public sealed class RuntimeHostController : IRuntimeHostController
    {
        private readonly RuntimeHostTalker _talker;
        private readonly ILogger _logger;
        private readonly bool _automaticRestartEnabled;
        private readonly TimeSpan _maximumUptime;

        public RuntimeHostController(
            RuntimeHostTalker talker,
            ILogger logger,
            IRpaRuntimeContext runtimeContext,
            bool? automaticRestartEnabled = null,
            TimeSpan? maximumUptime = null)
        {
            _talker = talker ?? throw new ArgumentNullException(nameof(talker));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ArgumentNullException.ThrowIfNull(runtimeContext);
            _automaticRestartEnabled = automaticRestartEnabled ??
                runtimeContext.GetConfiguration(
                    "-runtimehost-auto-restart",
                    "RuntimehostAutoRestartEnabled") != "false";

            if (maximumUptime.HasValue)
            {
                _maximumUptime = maximumUptime.Value;
            }
            else if (double.TryParse(runtimeContext.GetConfiguration(
                "-runtimehost-max-uptime-hours",
                "RuntimehostMaxUptimeHours"), out var configuredHours) && configuredHours > 0)
            {
                _maximumUptime = TimeSpan.FromHours(configuredHours);
            }
            else
            {
                _maximumUptime = TimeSpan.FromHours(2);
            }
        }

        public string? CurrentPackagePath => _talker.CurrentPackagePath;

        public string? ZBrainServicePath { get; set; }

        public bool IsInitialized => _talker.IsInited;

        public bool IsIpaInitialized { get; set; }

        public async Task<object> InitializeAsync(
            string packagePath,
            string installationPath,
            CancellationToken cancellationToken = default)
            => await _talker.InitUnaryAsync(
                packagePath,
                installationPath,
                cancellationToken).ConfigureAwait(false);

        public Task<object> RunAsync(
            RuntimeHostRunRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            return _talker.RunUnaryAsync(
                request.PackagePath,
                request.PluginName,
                request.Action,
                request.JsonParameters,
                request.RunParameters,
                request.CancelRuntimeOnTimeout,
                request.AutoInitialize,
                request.RunMode,
                request.Options,
                cancellationToken);
        }

        public async Task SetParametersAsync(
            RuntimeHostSetParametersRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.AutoInitialize &&
                (!_talker.IsInited || !string.Equals(
                    _talker.CurrentPackagePath,
                    request.PackagePath,
                    StringComparison.OrdinalIgnoreCase)))
            {
                await _talker.InitUnaryAsync(
                    request.PackagePath,
                    string.Empty,
                    cancellationToken).ConfigureAwait(false);
            }

            await _talker.SetParam(
                request.JsonParameters,
                request.PackagePath,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<object> DisposeAsync(
            string packagePath,
            CancellationToken cancellationToken = default)
            => await _talker.DisposeUnaryAsync(packagePath, cancellationToken).ConfigureAwait(false);

        public bool ExtendTimeout(RuntimeHostTimeoutExtension extension)
            => _talker.ResetPluginLoaderRunTimeout(extension);

        public async Task RestartAsync(
            bool dueToTimeout = false,
            CancellationToken cancellationToken = default)
        {
            IsIpaInitialized = false;
            await _talker.RestartRuntimeAsync(dueToTimeout, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> TryRestartAfterRunEndAsync(CancellationToken cancellationToken = default)
        {
            if (!_automaticRestartEnabled || !_talker.IsReady || !_talker.ConnectedAt.HasValue)
            {
                return false;
            }

            var uptime = DateTimeOffset.UtcNow - _talker.ConnectedAt.Value;
            if (uptime < _maximumUptime)
            {
                return false;
            }

            _logger.LogInfo($"RuntimeHost[{_talker.Id}] 已连续在线 {uptime}，运行结束后执行定期重启");
            await RestartAsync(dueToTimeout: false, cancellationToken).ConfigureAwait(false);
            return true;
        }
    }
}
