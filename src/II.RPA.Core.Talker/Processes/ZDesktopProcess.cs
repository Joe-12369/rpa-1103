using II.RPA.Core.Log;
using II.RPA.Core.Runtime;
using System.Diagnostics;
using System.Text;

namespace II.RPA.Core.Talker.Processes
{
    public class ZDesktopProcess : BaseProcess
    {
        private readonly string _commandArgs;
        private readonly string _filePath;

        private volatile bool _withAdministrator;

        public ZDesktopProcess(string commandArgs, ILogger logger, IRpaRuntimeContext runtimeContext) : base(logger)
        {
            ArgumentNullException.ThrowIfNull(runtimeContext);
            var configuredPath = runtimeContext.GetConfiguration("-zdesktop-path", "ZDesktopPath");
            _filePath = !string.IsNullOrWhiteSpace(configuredPath)
                ? Path.GetFullPath(configuredPath)
                : Path.GetFullPath($@"{string.Join(Path.DirectorySeparatorChar.ToString(), Enumerable.Repeat("..", 1))}\II.RPA.ZDeskTop\II.RPA.ZDeskTop.exe");
            // The real ZDesktop copies its own command line into CoreServerStartTask.
            _commandArgs = $"{commandArgs} --python_env {ResolvePythonEnvVersion(runtimeContext)}";
        }

        /// <summary>
        /// 小窗侧要使用的 Python 环境版本：优先取虚拟运行时记录下来的期望版本
        /// （此时小窗可能尚未启动，父核来不及把 SwitchPythonEnv 转发给子核），
        /// 没有记录时沿用父核当前的环境版本，保持原有行为。
        /// </summary>
        private static string ResolvePythonEnvVersion(IRpaRuntimeContext runtimeContext)
        {
            var desiredVersion = ZDesktopPythonEnvState.DesiredVersion;
            return string.IsNullOrWhiteSpace(desiredVersion)
                ? runtimeContext.PythonEnvironmentVersion
                : desiredVersion;
        }

        protected override string ProcessName => "II.RPA.ZDesktop";

        protected override Process GetChildProcess(ref bool addChildProcess)
        {
            if (_withAdministrator)
            {
                // runas 提权启动不会继承父进程的进程级环境变量，DOTNET_ROOT 会丢失，
                // 导致提权后的 ZDeskTop 找不到 bundled 运行时。这里用 cmd 包装，
                // 把当前进程的 DOTNET_ROOT/DOTNET_ROOT_X86/DOTNET_ROOT_X64 显式写入提权进程环境。
                var comspec = Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe";
                var cmdArguments = "/d /s /c \"" + BuildElevatedCommandLine(_filePath, _commandArgs) + "\"";
                return new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = comspec,
                        Arguments = cmdArguments,
                        UseShellExecute = true,
                        WorkingDirectory = Directory.GetParent(_filePath).FullName,
                        Verb = "runas",
                        WindowStyle = ProcessWindowStyle.Hidden,
                    },
                    EnableRaisingEvents = true,
                };
            }

            return new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _filePath,
                    Arguments = _commandArgs,
                    UseShellExecute = false,
                    WorkingDirectory = Directory.GetParent(_filePath).FullName,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                },
                EnableRaisingEvents = true,
            };
        }

        /// <summary>
        /// 构建提权启动的命令行：先在提权进程里 set 当前进程的 DOTNET_ROOT 系列变量，再执行目标程序。
        /// </summary>
        private static string BuildElevatedCommandLine(string targetExe, string arguments)
        {
            var command = new StringBuilder();
            foreach (var name in new[] { "DOTNET_ROOT", "DOTNET_ROOT_X86", "DOTNET_ROOT_X64" })
            {
                var value = Environment.GetEnvironmentVariable(name);
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                if (command.Length > 0)
                {
                    command.Append(" && ");
                }

                command.Append("set \"").Append(name).Append('=').Append(value).Append('"');
            }

            if (command.Length > 0)
            {
                command.Append(" && ");
            }

            command.Append('"').Append(targetExe).Append('"');
            if (!string.IsNullOrWhiteSpace(arguments))
            {
                command.Append(' ').Append(arguments);
            }

            return command.ToString();
        }

        internal bool WithAdministrator => _withAdministrator;

        protected override void ProcessExitHandle(bool isInitiativeStop, int? exitCode)
        {
            _withAdministrator = exitCode == 5;
            if (_withAdministrator)
            {
                Logger.LogWarning("ExitCode=5，后续显式启动将改用管理员权限");
            }

            if (!isInitiativeStop && _withAdministrator)
            {
                // ExitCode=5（子会话启用需管理员权限）时沿用基础自动重启框架：
                // 指数退避（500ms 起）、最多 5 次熔断；重启时 GetChildProcess 已读取
                // _withAdministrator，以管理员(runas)方式启动。
                //
                // 与参考实现（ZdesktopGuard）的差异：参考实现在 ExitCode=5 时无限递归重启；
                // 本实现复用 BaseProcess 熔断框架（最多 5 次、UAC 取消不再重试），属有意增强，
                // 防止管理员权限下仍退出 5 时的死循环与 UAC 弹窗轰炸。
                base.ProcessExitHandle(isInitiativeStop, exitCode);
            }
            else if (!isInitiativeStop)
            {
                // 其他异常退出保持现状：ZDesktop 由用户主动拉起，不做守护式自动重启。
                Logger.LogInfo($"{ProcessName} 已退出（exitCode={exitCode}），不执行自动重启");
            }
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
                Logger.LogZdesktop(data);
            }
        }
    }
}
