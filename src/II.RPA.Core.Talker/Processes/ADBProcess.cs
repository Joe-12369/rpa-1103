using II.RPA.Core.Log;
using II.RPA.Core.Runtime;
using II.RPA.Core.Talker.IO;
using System.Diagnostics;

namespace II.RPA.Core.Talker.Processes
{
    public class ADBProcess : BaseProcess
    {
        private readonly string _fileDir;
        private readonly string _filePath;
        private readonly string _sourceDir;
        private readonly int? _serverPort;
        private string _arguments = string.Empty;

        public ADBProcess(ILogger logger, IRpaRuntimeContext? runtimeContext = null) : base(logger)
        {
            var configuredPath = runtimeContext?.GetConfiguration("-adb-path", "AdbPath");
            _filePath = !string.IsNullOrWhiteSpace(configuredPath)
                ? Path.GetFullPath(configuredPath)
                : Path.GetFullPath(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "adb",
                    "adb.exe"));
            _fileDir = Path.GetDirectoryName(_filePath)
                ?? throw new InvalidOperationException($"无法确定 adb 可执行文件目录: {_filePath}");

            var configuredSourceDir = runtimeContext?.GetConfiguration("-adb-source-dir", "AdbSourceDir");
            _sourceDir = !string.IsNullOrWhiteSpace(configuredSourceDir)
                ? Path.GetFullPath(configuredSourceDir)
                : Path.GetFullPath($@"{string.Join(Path.DirectorySeparatorChar.ToString(), Enumerable.Repeat("..", 4))}\rpa-engine\resource\adb");

            var configuredServerPort = runtimeContext?.GetConfiguration("-adb-server-port", "AdbServerPort");
            if (!string.IsNullOrWhiteSpace(configuredServerPort))
            {
                if (!int.TryParse(configuredServerPort, out var serverPort) || serverPort is <= 0 or > 65535)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(runtimeContext),
                        configuredServerPort,
                        "AdbServerPort 必须是 1 到 65535 之间的整数。");
                }
                _serverPort = serverPort;
            }
        }

        public string FilePath => _filePath;

        public int? ServerPort => _serverPort;

        public event Action<string>? OutputDataReceived;

        public event Action<string>? ErrorDataReceived;

        // Retain the optional parameter for compatibility; routine lifecycle logging is always quiet.
        public void Start(string arguments, bool quietLifecycleLogging = false)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(arguments);
            _arguments = arguments;
            Start();
        }

        public virtual void EnsureInstalled()
        {
            if (File.Exists(_filePath))
            {
                return;
            }

            if (!Directory.Exists(_sourceDir))
            {
                throw new DirectoryNotFoundException($"未找到 adb 资源目录: {_sourceDir}");
            }

            Directory.CreateDirectory(_fileDir);
            FileSystemUtility.CopyDirectory(_sourceDir, _fileDir);
            if (!File.Exists(_filePath))
            {
                throw new FileNotFoundException("adb 安装后仍未找到 adb.exe", _filePath);
            }
        }

        protected override string ProcessName => "adb.exe";

        // ADB commands run frequently. Keep routine lifecycle messages at trace, but
        // retain debug diagnostics for nonzero or unavailable exit codes.
        protected override bool LogLifecycleAsDebug => false;

        protected override bool LogExitAsDebug(int? exitCode) => exitCode != 0;

        protected override Process GetChildProcess(ref bool addChildProcess)
        {
            var arguments = _serverPort.HasValue
                ? $"-P {_serverPort.Value} {_arguments}"
                : _arguments;
            var startInfo = new ProcessStartInfo
            {
                FileName = _filePath,
                Arguments = arguments,
                UseShellExecute = false,
                WorkingDirectory = _fileDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = null,
                StandardErrorEncoding = null,
            };
            if (_serverPort.HasValue)
            {
                startInfo.Environment["ANDROID_ADB_SERVER_PORT"] = _serverPort.Value.ToString();
            }

            return new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };
        }

        protected override void HandleErrorDataReceived(string data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                ErrorDataReceived?.Invoke(data);
            }
            base.HandleErrorDataReceived(data);
        }

        protected override void HandleOutputDataReceived(string data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                OutputDataReceived?.Invoke(data);
            }
            base.HandleOutputDataReceived(data);
        }

        protected override void ProcessExitHandle(bool isInitiativeStop, int? exitCode)
        {
            // ADB 的 devices 与 logcat 都由 Talker 循环显式调度，不能让通用守护逻辑并发重启。
        }
    }
}
