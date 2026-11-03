using II.RPA.Core.Log;
using II.RPA.Core.Runtime;
using System.Diagnostics;

namespace II.RPA.Core.Talker.Processes
{
    public class EventMonitorProcess : BaseProcess
    {
        private readonly string _fileDir;

        private readonly string _filePath;
        public EventMonitorProcess(ILogger logger, IRpaRuntimeContext runtimeContext) : base(logger)
        {
            ArgumentNullException.ThrowIfNull(runtimeContext);
            var configuredPath = runtimeContext.GetConfiguration("-monitor-path", "MonitorPath");
            _filePath = !string.IsNullOrWhiteSpace(configuredPath)
                ? Path.GetFullPath(configuredPath)
                : Path.GetFullPath($@"{string.Join(Path.DirectorySeparatorChar.ToString(), Enumerable.Repeat("..", 2))}\II.RPA.EventMonitor\II.RPA.EventMonitor.exe");
            _fileDir = Directory.GetParent(_filePath).FullName;
        }

        protected override string ProcessName => "EventMonitor";

        protected override Process GetChildProcess(ref bool addChildProcess)
        {
            addChildProcess = false;
            var sameNameProcesses = Process.GetProcessesByName("II.RPA.EventMonitor");
            var conflictingProcessIds = new List<int>();
            foreach (var process in sameNameProcesses)
            {
                try
                {
                    var executablePath = process.MainModule?.FileName;
                    if (!string.IsNullOrWhiteSpace(executablePath) &&
                        string.Equals(
                            Path.GetFullPath(executablePath),
                            _filePath,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var other in sameNameProcesses)
                        {
                            if (!ReferenceEquals(other, process))
                            {
                                other.Dispose();
                            }
                        }
                        return process;
                    }

                    conflictingProcessIds.Add(process.Id);
                }
                catch
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            conflictingProcessIds.Add(process.Id);
                        }
                    }
                    catch
                    {
                    }
                }
                process.Dispose();
            }

            if (conflictingProcessIds.Count > 0)
            {
                throw new InvalidOperationException(
                    $"检测到其他部署目录的 EventMonitor 进程，拒绝接管或重复启动。PID: {string.Join(", ", conflictingProcessIds)}");
            }

            return new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _filePath,
                    WorkingDirectory = _fileDir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
                EnableRaisingEvents = true,
            };
        }
    }
}
