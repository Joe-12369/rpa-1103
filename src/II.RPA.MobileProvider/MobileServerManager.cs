using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider
{
    public class MobileServerManager
    {
        private readonly static ILogger _logger = Logger.OfContext();
        private static readonly object _appiumProcessLock = new object();
        private static Process _appiumProcess;
        private static string _appiumProcessPort = string.Empty;
        private static volatile bool _isShuttingDown;
        private static int _appiumRestartInProgress;
        private const int AppiumRestartMaxAttempts = 3;
        private const int AppiumRestartBaseDelayMilliseconds = 1000;
        private string appiumPort;

        /// <summary>
        /// 标记 Appium 服务进程进入主动关闭流程（宿主退出时调用），
        /// 用于让 Exited 事件区分"计划内关闭"与"异常退出"，避免误触发自动重启。
        /// </summary>
        public static void MarkShuttingDown()
        {
            _isShuttingDown = true;
            Process process;
            lock (_appiumProcessLock)
            {
                process = _appiumProcess;
            }
            StopUnreadyAppiumProcess(process);
        }

        public void InitMobileServer()
        {
            try
            {
                if (IsInstallMobileTools())
                {
                    //logger.LogInformation($"初始化手机流程服务");
                    appiumPort = GetMobileServerPort();
                    GlobleConfig.AppiumServerPort = appiumPort;
                }
                else
                {
                    GlobleConfig.AppiumServerPort = "";
                    //logger.LogWarning($"手机流程插件未安装！");
                }
            }
            catch (Exception ex)
            {
                GlobleConfig.AppiumServerPort = "";
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "初始化 Appium 服务失败");
            }
        }

        public bool CheckNodeIsClosed()
        {
            return string.IsNullOrEmpty(GlobleConfig.AppiumServerPort)
                || !IsOwnedAppiumProcessReady(GlobleConfig.AppiumServerPort);
        }

        private static bool IsOwnedAppiumProcessReady(string port)
        {
            lock (_appiumProcessLock)
            {
                try
                {
                    return _appiumProcess != null
                        && !_appiumProcess.HasExited
                        && string.Equals(_appiumProcessPort, port, StringComparison.Ordinal)
                        && IsAppiumPortListening(port);
                }
                catch
                {
                    return false;
                }
            }
        }

        public static bool IsAppiumPortListening(string port, int timeoutMilliseconds = 500)
        {
            if (!int.TryParse(port, out var portNumber))
            {
                return false;
            }

            try
            {
                using (var tcpClient = new TcpClient())
                {
                    var connectTask = tcpClient.ConnectAsync(IPAddress.Loopback, portNumber);
                    return connectTask.Wait(timeoutMilliseconds) && tcpClient.Connected;
                }
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> WaitForAppiumReadyAsync(string port, int retryCount = 30, int delayMilliseconds = 200)
        {
            for (int i = 0; i < retryCount; i++)
            {
                if (IsAppiumPortListening(port))
                {
                    return true;
                }
                await Task.Delay(delayMilliseconds);
            }
            return false;
        }

        private Process StartMobileServer(string port)
        {
            lock (_appiumProcessLock)
            {
                if (_isShuttingDown)
                {
                    return null;
                }
                if (_appiumProcess != null && !_appiumProcess.HasExited)
                {
                    if (string.Equals(_appiumProcessPort, port, StringComparison.Ordinal))
                    {
                        return _appiumProcess;
                    }
                    throw new InvalidOperationException($"Appium 服务已由当前进程启动在端口[{_appiumProcessPort}]，不能重复启动到端口[{port}]");
                }
                if (IsAppiumPortListening(port))
                {
                    throw new InvalidOperationException($"端口[{port}]已被其他进程占用，不能启动 Appium 服务");
                }

                string path = Path.Combine(Environment.GetEnvironmentVariable("ALLUSERSPROFILE"), "II.RPA", "mobile");
                string androidHome = Path.Combine(path, "AndroidSDK");
                string javaHome = Path.Combine(path, "JavaSDK", "jdk1.8.0_241");
                string appiumHome = Path.Combine(path, "appium");
                string appiumPath = Path.Combine(appiumHome, "node_modules\\appium\\build\\lib\\main.js");
                List<string> env = new List<string>();
                env.Add("%JAVA_HOME%\\bin");
                env.Add("%ANDROID_HOME%\\platform-tools");
                env.Add("%ANDROID_HOME%\\build-tools\\29.0.3");
                Environment.SetEnvironmentVariable("ANDROID_HOME", androidHome);
                Environment.SetEnvironmentVariable("JAVA_HOME", javaHome);
                Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + string.Join(";", env));
                Process process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(appiumHome, "node.exe"),
                    Arguments = $"{appiumPath} -p {port}",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = appiumHome,
                    StandardErrorEncoding = Encoding.UTF8,
                    StandardOutputEncoding = Encoding.UTF8
                };
                process.EnableRaisingEvents = true;
                process.OutputDataReceived += Process_OutputDataReceived;
                process.ErrorDataReceived += Process_ErrorDataReceived;
                process.Exited += Process_Exited;
                process.Start();
                _appiumProcess = process;
                _appiumProcessPort = port;
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                process.StandardInput.AutoFlush = false;
                return process;
            }
        }
        public string GetMobileServerPort()
        {
            lock (_appiumProcessLock)
            {
                if (_appiumProcess != null && !_appiumProcess.HasExited)
                {
                    return _appiumProcessPort;
                }
            }

            string port = GetAvailablePort();
            _logger.LogInformation($"将在端口[{port}]启动当前 Provider 所属的 Appium 服务");
            StartMobileServer(port);
            return port;
        }

        private string GetAvailablePort()
        {
            const int firstPort = 4723;
            const int candidateCount = 10;
            for (int index = 0; index < candidateCount; index++)
            {
                int port = firstPort + index * 2;
                if (IsPortAvailable(port))
                {
                    return port.ToString();
                }
            }

            throw new InvalidOperationException($"未找到可用的 Appium 服务端口，已检查[{firstPort}-{firstPort + (candidateCount - 1) * 2}]");
        }

        private static bool IsPortAvailable(int port)
        {
            TcpListener listener = null;
            try
            {
                listener = new TcpListener(IPAddress.Any, port)
                {
                    ExclusiveAddressUse = true
                };
                listener.Start();
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            finally
            {
                listener?.Stop();
            }
        }
        public bool IsInstallMobileTools()
        {
            string androidSdkPath = System.IO.Path.Combine(Environment.GetEnvironmentVariable("ALLUSERSPROFILE"), "II.RPA\\mobile\\AndroidSDK\\platform-tools\\adb.exe");
            string appiumPath = System.IO.Path.Combine(Environment.GetEnvironmentVariable("ALLUSERSPROFILE"), "II.RPA\\mobile\\appium\\node.exe");
            string javaSdkPath = System.IO.Path.Combine(Environment.GetEnvironmentVariable("ALLUSERSPROFILE"), "II.RPA\\mobile\\JavaSDK");
            if (File.Exists(androidSdkPath) && File.Exists(appiumPath) && Directory.Exists(javaSdkPath))
            {
                return true;
            }
            return false;
        }


        /// <summary>
        /// 消费 Appium 的标准输出，避免丢失服务端诊断信息。
        /// </summary>
        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _logger.LogDebug($"Appium 输出: {e.Data}");
            }
        }

        /// <summary>
        /// 消费 Appium 的错误输出，同时保留诊断信息。
        /// </summary>
        private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _logger.LogWarning($"Appium 错误输出: {e.Data}");
            }
        }

        private static void Process_Exited(object sender, EventArgs e)
        {
            bool isCurrentProcess;
            lock (_appiumProcessLock)
            {
                isCurrentProcess = ReferenceEquals(_appiumProcess, sender as Process);
                if (isCurrentProcess)
                {
                    _appiumProcess = null;
                    _appiumProcessPort = string.Empty;
                }
            }
            if (!isCurrentProcess)
            {
                return;
            }
            if (_isShuttingDown)
            {
                _logger.LogInformation("Appium 服务进程已按计划关闭");
                return;
            }
            _logger.LogError("Appium 服务进程异常退出，将尝试自动重启");
            Provider.MobileSessionCache.MarkAllSessionsRequiresRecovery("AppiumNodeExited");
            StartAppiumRestartLoop();
        }

        private static void StartAppiumRestartLoop()
        {
            if (_isShuttingDown || Interlocked.CompareExchange(ref _appiumRestartInProgress, 1, 0) != 0)
            {
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    string port = GlobleConfig.AppiumServerPort;
                    if (string.IsNullOrEmpty(port))
                    {
                        return;
                    }

                    for (int attempt = 1; attempt <= AppiumRestartMaxAttempts && !_isShuttingDown; attempt++)
                    {
                        await Task.Delay(AppiumRestartBaseDelayMilliseconds * attempt);
                        if (IsOwnedAppiumProcessReady(port))
                        {
                            return;
                        }

                        _logger.LogWarning($"检测到 Appium 服务进程异常退出，正在端口 {port} 进行第 {attempt} 次重启...");
                        try
                        {
                            Process process = new MobileServerManager().StartMobileServer(port);
                            if (await new MobileServerManager().WaitForAppiumReadyAsync(port))
                            {
                                _logger.LogInformation($"Appium 服务已在端口 {port} 恢复");
                                return;
                            }
                            StopUnreadyAppiumProcess(process);
                        }
                        catch (Exception ex)
                        {
                            Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, $"第 {attempt} 次重启 Appium 服务失败");
                        }
                    }

                    if (!_isShuttingDown && !IsOwnedAppiumProcessReady(port))
                    {
                        _logger.LogError($"Appium 服务连续 {AppiumRestartMaxAttempts} 次重启失败，等待后续连接流程恢复");
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref _appiumRestartInProgress, 0);
                }
            });
        }
        private static void StopUnreadyAppiumProcess(Process process)
        {
            if (process == null)
            {
                return;
            }

            lock (_appiumProcessLock)
            {
                if (ReferenceEquals(_appiumProcess, process))
                {
                    _appiumProcess = null;
                    _appiumProcessPort = string.Empty;
                }
            }

            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, ex, "清理未就绪的 Appium 服务进程失败");
            }
            finally
            {
                process.Dispose();
            }
        }
    }
}
