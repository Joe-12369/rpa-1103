using II.RPA.Core.Log;
using II.RPA.Core.Runtime;
using System.Diagnostics;
using System.Text;

namespace II.RPA.Core.Talker.Processes
{
    public class ZBrainProcess : BaseProcess
    {
        private readonly string _pythonEnv;
        private readonly string _pythonDLLs;
        private readonly string _pythonLib;
        private readonly string _pythonSite;
        private readonly string _pythonWin32Lib;
        private readonly string _fileDir;
        private readonly string _filePath;
        private string _pythonFile;
        private readonly string _pythonVenv;
        private readonly string _brainProject;
        private readonly bool _developMode;
        private readonly string _script;
        private readonly IRpaRuntimeContext _runtimeContext;

        public ZBrainProcess(ILogger logger, IRpaRuntimeContext runtimeContext) : base(logger)
        {
            _runtimeContext = runtimeContext ?? throw new ArgumentNullException(nameof(runtimeContext));
            var brainPath = _runtimeContext.GetConfiguration("-brain-path", "ZBrainPath");
            _filePath = !string.IsNullOrWhiteSpace(brainPath) ? brainPath : Path.GetFullPath($@"{string.Join(Path.DirectorySeparatorChar.ToString(), Enumerable.Repeat("..", 4))}\z-brain\Z-Brain.exe");
            _fileDir = Directory.GetParent(_filePath).FullName;
            var pythonName = Environment.Is64BitProcess ? "Python36" : "Python36-32";
            _pythonEnv = Path.GetFullPath($@"{_fileDir}\{string.Join(Path.DirectorySeparatorChar.ToString(), Enumerable.Repeat("..", 3))}\{pythonName}");
            _pythonDLLs = Path.GetFullPath(Path.Combine(_pythonEnv, @".\DLLs"));
            _pythonLib = Path.GetFullPath(Path.Combine(_pythonEnv, @".\Lib"));
            _pythonSite = Path.GetFullPath(Path.Combine(_pythonLib, @".\site-packages"));
            _pythonWin32Lib = Path.GetFullPath(Path.Combine(_pythonSite, @".\win32\lib"));
            _pythonVenv = _runtimeContext.GetConfiguration("-python-venv", "PythonVenv") ?? string.Empty;
            _brainProject = _runtimeContext.GetConfiguration("-brain-proj", "ZBrainProject") ?? string.Empty;
            _developMode = !string.IsNullOrWhiteSpace(_pythonVenv) && !string.IsNullOrWhiteSpace(_brainProject);
            _pythonFile = $@"{_pythonVenv}\Scripts\python.exe";
            _script = "-m Z-Brain-Plugin.__main__ ";
        }

        protected override string ProcessName => "Z-Brain";

        protected override Process GetChildProcess(ref bool addChildProcess)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var consoleEncoding = Encoding.GetEncoding("GBK");
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _developMode ? _pythonFile : _filePath,
                    Arguments = (_developMode ? _script : string.Empty) + $"-C {_runtimeContext.ProducedClientId} -P {_runtimeContext.RootServerPort}",
                    WorkingDirectory = _developMode ? Directory.GetParent(_brainProject).FullName : _fileDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = consoleEncoding,
                    StandardErrorEncoding = consoleEncoding,
                },
                EnableRaisingEvents = true,
            };

            if (!_developMode)
            {
                process.StartInfo.Environment["PATH"] = _pythonEnv + ";" + process.StartInfo.Environment["PATH"];
                process.StartInfo.Environment["PYTHONHOME"] = _pythonEnv;
                process.StartInfo.Environment["PYTHONIOENCODING"] = "GBK";
                process.StartInfo.Environment["PYTHONPATH"] = $"{_pythonEnv};{_pythonDLLs};{_pythonLib};{_pythonSite};{_pythonWin32Lib}";
                Logger.LogInfo($"PYTHONHOME: {_pythonEnv}");
            }

            return process;
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
                Logger.LogZBrain(data);
            }
        }
    }
}
