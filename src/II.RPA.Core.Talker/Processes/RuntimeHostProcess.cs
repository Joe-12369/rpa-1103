using II.RPA.Core.Log;
using II.RPA.Core.Runtime;
using System.Diagnostics;

namespace II.RPA.Core.Talker.Processes
{
    internal class RuntimeHostProcess : BaseProcess
    {
        private readonly string _runTimeHostStartupPath;
        private readonly string _runTimeHostStartupParams;
        private readonly IRpaRuntimeContext _runtimeContext;

        public RuntimeHostProcess(ILogger logger, IRpaRuntimeContext runtimeContext, string? pipeName = null) : base(logger)
        {
            _runtimeContext = runtimeContext ?? throw new ArgumentNullException(nameof(runtimeContext));
            PipeName = string.IsNullOrWhiteSpace(pipeName) ? Guid.NewGuid().ToString() : pipeName;
            var configuredPath = runtimeContext.GetConfiguration("-runtimehost-path", "RuntimeHostPath");
            _runTimeHostStartupPath = !string.IsNullOrWhiteSpace(configuredPath)
                ? Path.GetFullPath(configuredPath)
                : ".\\II.RPA.RuntimeHost.exe";
            _runTimeHostStartupParams = "-p {0} -c {1} -n {2} -r {3} -port {4}";
        }

        public string PipeName { get; }

        protected override string ProcessName => "II.RPA.RuntimeHost";

        protected override Process GetChildProcess(ref bool addChildProcess)
        {
            return new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo
                {
                    FileName = _runTimeHostStartupPath,
                    Arguments = string.Format(_runTimeHostStartupParams, _runtimeContext.CurrentProcessId, ((int)_runtimeContext.ClientKind) + 1, PipeName, IsRestart, _runtimeContext.ServerPort),
                    UseShellExecute = false,
                },
            };
        }
    }
}
