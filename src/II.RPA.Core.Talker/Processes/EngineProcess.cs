using II.RPA.Core.Log;
using II.RPA.Core.Runtime;
using System.Diagnostics;

namespace II.RPA.Core.Talker.Processes
{
    public abstract class EngineProcess : BaseProcess
    {
        private readonly string _configuredFilePath;

        public EngineProcess(ILogger logger, IRpaRuntimeContext runtimeContext) : base(logger)
        {
            RuntimeContext = runtimeContext ?? throw new ArgumentNullException(nameof(runtimeContext));
            ScriptDev = "-m rpa-engine.__main__";
            var enginePath = RuntimeContext.GetConfiguration("-engine-path", "EnginePath");
            _configuredFilePath = enginePath ?? string.Empty;
            PythonVenv = RuntimeContext.GetConfiguration("-python-venv", "PythonVenv") ?? string.Empty;
            EngineProject = RuntimeContext.GetConfiguration("-engine-proj", "EngineProject") ?? string.Empty;
            DevelopMode = !string.IsNullOrWhiteSpace(PythonVenv) && !string.IsNullOrWhiteSpace(EngineProject);
            PythonFile = $@"{PythonVenv}\Scripts\pythonw.exe";
            var engineScript = RuntimeContext.GetConfiguration("-engine-script", "EngineScript");
            Script = !string.IsNullOrWhiteSpace(engineScript)
                ? Path.GetFullPath(engineScript)
                : Path.GetFullPath($@"{string.Join(Path.DirectorySeparatorChar.ToString(), Enumerable.Repeat("..", 3))}\__main__.py");
        }

        public string PythonVenv { get; }
        public string EngineProject { get; }
        public bool DevelopMode { get; }
        public string FileDir => Directory.GetParent(FilePath)?.FullName ?? string.Empty;
        public string FilePath => !string.IsNullOrWhiteSpace(_configuredFilePath)
            ? _configuredFilePath
            : RuntimeContext.EngineExecutablePath;
        public string PythonFile { get; }
        public string ScriptDev { get; }
        public string Script { get; }

        protected IRpaRuntimeContext RuntimeContext { get; }

        protected abstract ProcessStartInfo GetProcessStartInfo();

        protected override Process GetChildProcess(ref bool addChildProcess)
        {
            var process = new Process()
            {
                StartInfo = GetProcessStartInfo(),
                EnableRaisingEvents = true,
            };

            if (process.StartInfo.Environment.ContainsKey("PYTHONHOME"))
            {
                process.StartInfo.Environment.Remove("PYTHONHOME");
            }

            if (process.StartInfo.Environment.ContainsKey("PYTHONPATH"))
            {
                process.StartInfo.Environment.Remove("PYTHONPATH");
            }
            return process;
        }
    }
}
