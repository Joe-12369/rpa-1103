using II.RPA.Communication.Endpoints;
using II.RPA.Communication.Hosting;
using II.RPA.Communication.Options;
using II.RPA.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using System.Threading;
using Microsoft.Win32;
using II.RPA.MobileProvider.Service;
using Rpa.Core.Mobileplugin;
using System.Diagnostics;
using System.IO;
using II.RPA.Logging.Definitions;
using System.Text.RegularExpressions;
using II.RPA.WinAutomation.CommonUtility;

namespace II.RPA.MobileProvider
{
    internal static class Program
    {
        private readonly static ILogger _logger = Logger.OfContext();
        private static GrpcServer _server;
        private static MobileHostApplicationContext _applicationContext;
        private static DateTime currentLogDate = DateTime.Now.Date;

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Logger.ConfigWithRole(CommandParamsHelper.ClientType == ClientType.Factory ? LoggingRoles.Factory :
                CommandParamsHelper.ClientType == ClientType.Bot ? LoggingRoles.Bot : LoggingRoles.Default);
            Task.Run(() =>
            {
                while (true)
                {
                    if (DateTime.Now.Date != currentLogDate)
                    {
                        currentLogDate = DateTime.Now.Date;
                        Logger.ConfigWithRole(CommandParamsHelper.ClientType == ClientType.Factory ? LoggingRoles.Factory :
                            CommandParamsHelper.ClientType == ClientType.Bot ? LoggingRoles.Bot : LoggingRoles.Default);
                    }
                    Thread.Sleep(1000 * 60 * 10);
                }
            });
            _logger.LogInformation("启动手机provider");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);


            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            Application.ThreadException += Application_ThreadException;


            Application.Idle += Application_Idle;
            Application.ApplicationExit += Application_Exit;

            _applicationContext = new MobileHostApplicationContext();
            Application.Run(_applicationContext);

        }
        private static GrpcServer Server
        {
            get => _server;
        }
        private static void NamedPipeServer_Error(Exception e)
        {
            // _logger.LogError(e.Error);
        }

        private static void Application_Idle(object sender, EventArgs e)
        {
            Application.Idle -= Application_Idle;
            MobileHostApplicationContext.SynchronizationContext = SynchronizationContext.Current;
            _logger.LogInformation($"NamedPipeServer is starting...");
            _logger.LogInformation($"管道名：{CommandParamsHelper.PipeName}");
            InitZbrianServiceUrl(CommandParamsHelper.ClientType);

            var mobilePluginService = new MobilePluginGRPCService();
            _server = new GrpcServer(new ServerOptions
            {
                Endpoint = new NamedPipeEndpoint(CommandParamsHelper.PipeName),
                ErrorHandler = NamedPipeServer_Error
            }, services => services.Register(
                MobilePluginService.BindService(mobilePluginService),
                binder => MobilePluginService.BindService(binder, mobilePluginService)));
            _server.StartAsync().GetAwaiter().GetResult();


            ParentMonitorTask(CommandParamsHelper.Pid);

        }
        private static void Application_Exit(object sender, EventArgs e)
        {
            //todo:Shutdown or clear sth
            ServerEnding();
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            UnhandledException(e.Exception);
            e.SetObserved();
        }
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.IsTerminating)
            {
                _logger.LogError($"Fatal Error: CLR is terminating!");
            }
            if (e.ExceptionObject is Exception exception)
            {
                UnhandledException(exception);
            }
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            UnhandledException(e.Exception);
        }

        private static void UnhandledException(Exception e)
        {
            if (e != null)
            {
                _logger.LogError(e);
            }
        }
        private static void ServerEnding()
        {
            //标记 Appium 服务进程为主动关闭，避免 Exited 误判为异常退出而触发自动重启
            MobileServerManager.MarkShuttingDown();
            //将拉起的手机appium服务关闭
            try
            {
                #region 先留着
                //_logger.LogDebug($"当前的手机自动化服务端口号：{GlobleConfig.AppiumServerPort}");
                //string matchResult = DosHelper.ExcuteCMD($"netstat -ano |findstr {GlobleConfig.AppiumServerPort}");
                //if (!string.IsNullOrEmpty(matchResult))
                //{
                //    MatchCollection portMatches = Regex.Matches(matchResult, @"(\d+)(?!.*\d)");
                //    int nodeProcid = int.Parse(portMatches[0].Value);
                //    _logger.LogDebug($"关闭手机服务进程，进程id【{nodeProcid}】");
                //    DosHelper.ExcuteCMD($"taskkill /pid {nodeProcid} -t -f");
                //}
                #endregion
                var pids = ProcessHelper.GetListChildProcessesId(Process.GetCurrentProcess().Id);
                foreach (var pid in pids)
                {
                    try
                    {
                        _logger.LogDebug($"关闭子进程，pid为【{pid}】");
                        using (Process process = Process.GetProcessById(pid))
                        {
                            process.Kill();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"关闭子进程失败，pid为【{pid}】", ex);
                    }
                }
                //当当前的手机node服务不是当前进程拉起时，进行通过端口关闭退出
                if (ProcessHelper.ExistProcess("node"))
                {
                    _logger.LogDebug($"当前的手机自动化服务端口号：{GlobleConfig.AppiumServerPort}");
                    string matchResult = DosHelper.ExcuteCMD($"netstat -ano |findstr {GlobleConfig.AppiumServerPort}");
                    if (!string.IsNullOrEmpty(matchResult))
                    {
                        MatchCollection portMatches = Regex.Matches(matchResult, @"(\d+)(?!.*\d)");
                        int nodeProcid = int.Parse(portMatches[0].Value);
                        _logger.LogDebug($"关闭手机服务进程，进程id【{nodeProcid}】");
                        DosHelper.ExcuteCMD($"taskkill /pid {nodeProcid} -t -f");
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, $"注销手机appium服务异常");
            }
            //管道服务注销
            _server?.StopAsync().GetAwaiter().GetResult();
            CleanupMobileAdbIfLastProvider();
        }

        private static bool IsMobileAdbProcess(Process process)
        {
            string executablePath = process.MainModule?.FileName;
            return !string.IsNullOrEmpty(executablePath)
                && string.Equals(Path.GetFullPath(executablePath), Path.GetFullPath(AdbDeviceHelper.adbPath), StringComparison.OrdinalIgnoreCase);
        }

        private static void CleanupMobileAdbIfLastProvider()
        {
            try
            {
                using (Process current = Process.GetCurrentProcess())
                {
                    bool hasOtherProvider = false;
                    foreach (Process provider in Process.GetProcessesByName(current.ProcessName))
                    {
                        using (provider)
                        {
                            if (provider.Id != current.Id && !provider.HasExited)
                                hasOtherProvider = true;
                        }
                    }
                    if (hasOtherProvider)
                    {
                        _logger.LogInformation("仍有其他MobileProvider运行，保留手机ADB进程");
                        return;
                    }
                }

                foreach (Process adb in Process.GetProcessesByName("adb"))
                {
                    using (adb)
                    {
                        try
                        {
                            if (!adb.HasExited && IsMobileAdbProcess(adb))
                            {
                                _logger.LogInformation($"最后一个MobileProvider退出，清理手机ADB进程，PID[{adb.Id}]，路径[{AdbDeviceHelper.adbPath}]");
                                adb.Kill();
                            }
                        }
                        catch (Exception ex)
                        {
                            Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, ex, "检查或清理手机ADB进程失败");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, ex, "退出时检查MobileProvider和清理ADB失败");
            }
        }

        /// <summary>
        /// 监视父进程
        /// </summary>
        /// <param name="aParentPid"></param>
        private static void ParentMonitorTask(int aParentPid)
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    try
                    {
                        //此条记录主要作用为日志文件会隔天被释放
                        //_logger.LogInformation($"监测父进程 {aParentPid} 是否存在");
                        Process.GetProcessById(aParentPid);
                        await Task.Delay(500);
                        continue;
                    }
                    catch (ArgumentException)
                    {
                        _logger.LogInformation($"父进程 {aParentPid} 不存在，MobileHost自动退出!");
                        ServerEnding();
                        _logger.LogDebug($"执行MobileHost退出!");
                        Environment.Exit(500);
                    }
                    catch (Exception ex)
                    {
                        ///能其他异常情况、增加日志记录异常信息
                        _logger.LogWarning($"父进程 {aParentPid} 不存在，MobileHost自动退出! {ex.GetType()}:{ex.Message}");
                        ServerEnding();
                        Environment.Exit(500);
                    }
                    break;
                }
            }, TaskCreationOptions.LongRunning)
                .ConfigureAwait(false)
                .GetAwaiter()
                .OnCompleted(() => { });
        }
        public static void InitZbrianServiceUrl(ClientType clientType)
        {
            if (clientType == ClientType.Factory)
            {
                GlobleConfig.ZbrainServiceUrl = "http://127.0.0.1:6440/";
            }
            else
            {
                GlobleConfig.ZbrainServiceUrl = "http://127.0.0.1:6441/";
            }
        }

    }
}

