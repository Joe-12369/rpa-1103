using II.RPA.Core.Log;
using II.RPA.Core.Runtime;
using System.Diagnostics;

namespace II.RPA.Core.Talker.Processes
{
    public class ExecutorProcess : EngineProcess
    {
        public ExecutorProcess(ILogger logger, IRpaRuntimeContext runtimeContext) : base(logger, runtimeContext) { }

        protected override string ProcessName => "Executor";

        public void ExpectRetirement() => ExpectCurrentProcessExit();

        protected override ProcessStartInfo GetProcessStartInfo()
        {
            return new ProcessStartInfo
            {
                FileName = DevelopMode ? $"\"{PythonFile}\"" : $"\"{FilePath}\"",
                Arguments = (DevelopMode ? ScriptDev : $"\"{Script}\"") + " " + $"-C {RuntimeContext.ProducedClientId} -P {RuntimeContext.ServerPort}",
                UseShellExecute = false,
                WorkingDirectory = DevelopMode ? Directory.GetParent(EngineProject).FullName : Directory.GetParent(Script).FullName,
                CreateNoWindow = true,
                StandardOutputEncoding = null,
                StandardErrorEncoding = null,
            };
        }
    }
}
