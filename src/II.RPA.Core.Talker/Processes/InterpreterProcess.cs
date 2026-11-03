using II.RPA.Core.Log;
using II.RPA.Core.Runtime;
using System.Diagnostics;

namespace II.RPA.Core.Talker.Processes
{
    public class InterpreterProcess : EngineProcess
    {
        public InterpreterProcess(ILogger logger, IRpaRuntimeContext runtimeContext) : base(logger, runtimeContext) { }

        protected override string ProcessName => "Interpreter";

        protected override ProcessStartInfo GetProcessStartInfo()
        {
            return new ProcessStartInfo
            {
                FileName = DevelopMode ? $"\"{PythonFile}\"" : $"\"{FilePath}\"",
                Arguments = (DevelopMode ? ScriptDev : $"\"{Script}\"") + " " + $"-C {RuntimeContext.ProducedClientId} -P {RuntimeContext.ServerPort}" + " -I",
                UseShellExecute = false,
                WorkingDirectory = DevelopMode ? Directory.GetParent(EngineProject).FullName : Directory.GetParent(Script).FullName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = null,
                StandardErrorEncoding = null,
            };
        }

        protected override void HandleOutputDataReceived(string data)
        {
            HandleStreamDataReceived(data);
        }

        protected override void HandleErrorDataReceived(string data)
        {
            HandleStreamDataReceived(data);
        }

        private void HandleStreamDataReceived(string data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                Logger.LogInterpreter(data);
            }
        }
    }
}
