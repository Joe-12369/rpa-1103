using II.RPA.Core.Log;
using II.RPA.Core.Runtime;
using II.RPA.Core.Talker.IO;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace II.RPA.Core.Talker.Processes
{
    public class MCPAgentProcess : BaseProcess
    {
        private readonly Action<string>? _outputHandler;
        private readonly Action<string>? _errorHandler;
        private readonly string _mcpAgentPackage;
        private readonly string _fileDir;
        private readonly string _filePath;

        public MCPAgentProcess(
            ILogger logger,
            IRpaRuntimeContext runtimeContext,
            Action<string>? outputHandler = null,
            Action<string>? errorHandler = null) : base(logger)
        {
            ArgumentNullException.ThrowIfNull(runtimeContext);
            _outputHandler = outputHandler;
            _errorHandler = errorHandler;
            Environment = new Dictionary<string, string>();
            var regex = new Regex(@"^iirpa_mcp_agent-(?<version>\d+\.\d+\.\d+)-py3-none-any\.whl$", RegexOptions.Compiled);
            _filePath = Path.GetFullPath($@"{string.Join(Path.DirectorySeparatorChar.ToString(), Enumerable.Repeat("..", 4))}\support\envTools\uv.exe");
            _fileDir = Directory.GetParent(_filePath).FullName;
            var uvPath = runtimeContext.GetConfiguration("-uv-path", "UVPath");
            _filePath = !string.IsNullOrWhiteSpace(uvPath) ? uvPath : _filePath;
            var packagePath = runtimeContext.GetConfiguration("-mcp-agent-package", "MCPAgentPackage");
            if (string.IsNullOrWhiteSpace(packagePath))
            {
                var mcpAgentPackageDir = Path.GetFullPath($@"{string.Join(Path.DirectorySeparatorChar.ToString(), Enumerable.Repeat("..", 4))}\support\mcpAgent");
                _mcpAgentPackage = FileSystemUtility.SearchFiles(mcpAgentPackageDir, regex).FirstOrDefault();
            }
            else
            {
                _mcpAgentPackage = FileSystemUtility.SearchFiles(packagePath, regex).FirstOrDefault();
            }
        }

        public string CWD { get; set; }

        public Dictionary<string, string> Environment { get; set; }

        protected override string ProcessName => "MCPAgent";

        protected override Process GetChildProcess(ref bool addChildProcess)
        {
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _filePath,
                    Arguments = $"tool run --from \"{_mcpAgentPackage}\" iirpa-mcp-agent",
                    WorkingDirectory = CWD == default ? _fileDir : CWD,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                },
                EnableRaisingEvents = true,
            };

            foreach (var item in Environment)
            {
                process.StartInfo.Environment[item.Key] = item.Value;
            }
            process.StartInfo.Environment["PYTHONUTF8"] = "1";
            process.StartInfo.Environment["UV_INDEX_URL"] = "https://pypi.tuna.tsinghua.edu.cn/simple";
            process.StartInfo.Environment["npm_config_registry"] = "https://registry.npm.tuna.tsinghua.edu.cn/";
            process.StartInfo.Environment["II_UV_PATH"] = _filePath;

            return process;
        }

        protected override void HandleOutputDataReceived(string data)
        {
            HandleStreamDataReceived(data);
            _outputHandler?.Invoke(data);
        }

        protected override void HandleErrorDataReceived(string data)
        {
            HandleStreamDataReceived(data);
            _errorHandler?.Invoke(data);
        }

        private void HandleStreamDataReceived(string data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                Logger.LogMCPAgent(data);
            }
        }

        protected override void ProcessExitHandle(bool isInitiativeStop, int? exitCode)
        {
            // MCP Agent is a one-shot operation. Completion must not restart the process.
        }
    }
}
