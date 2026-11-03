using II.RPA.Core.Log;
using II.RPA.Core.Runtime;
using System.Diagnostics;

namespace II.RPA.Core.Talker.Processes
{
    internal class MobileeProviderProcess : BaseProcess, IMobileProviderProcessHandle
    {
        private readonly string _channelName;
        private readonly string _startupPath;
        private readonly IRpaRuntimeContext _runtimeContext;

        public MobileeProviderProcess(string channelName, ILogger logger, IRpaRuntimeContext runtimeContext) : base(logger)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(channelName);
            _channelName = channelName;
            _runtimeContext = runtimeContext ?? throw new ArgumentNullException(nameof(runtimeContext));
            var configuredPath = runtimeContext.GetConfiguration("-mobile-provider-path", "MobileProviderPath");
            _startupPath = !string.IsNullOrWhiteSpace(configuredPath)
                ? Path.GetFullPath(configuredPath)
                : ".\\II.RPA.MobileProvider.exe";
        }

        protected override string ProcessName => "II.RPA.MobileProvider";

        protected override Process GetChildProcess(ref bool addChildProcess)
        {
            // MobileProvider owns its lifecycle and may outlive CoreServer.
            addChildProcess = false;
            return new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo
                {
                    FileName = _startupPath,
                    Arguments = $"-c {(_runtimeContext.ClientId.Equals("Z-Factory", StringComparison.OrdinalIgnoreCase) ? 1 : 2)} -p {_runtimeContext.CurrentProcessId} -n {_channelName}",
                    WorkingDirectory = Path.GetDirectoryName(_startupPath) ?? AppContext.BaseDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = false,
                },
            };
        }
    }
}
