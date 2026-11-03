using Google.Protobuf;
using II.RPA.Communication.Endpoints;
using II.RPA.Communication.Hosting;
using II.RPA.Communication.Options;
using II.RPA.Logging;
using II.RPA.Logging.Definitions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Services;
using II.RPA.RuntimeHost.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Rpa.Core.Runtimehost;
using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace II.RPA.RuntimeHost
{
    internal static class Program
    {
        private readonly static ILogger _logger = Logger.OfContext();
        private static GrpcServer _server;
        private static RunTimeHostApplicationContext _applicationContext;
        private static DateTime _startTime;
        private static RunTimeServiceProvider _runTimeServiceProvider;
        private static Thread monForQuitThread = MonForQuitThread.StartMonForQuit();
        private static DateTime currentLogDate = DateTime.Now.Date;
        private static readonly CancellationTokenSource _workerConnectionLifetime = new CancellationTokenSource();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static async Task Main(string[] args)
        {
            _startTime = DateTime.Now;
            try
            {
                AttachConsole(ATTACH_PARENT_PROCESS);
                var role = CommandParamsHelper.ClientType == ClientType.Factory ? LoggingRoles.Factory :
                            CommandParamsHelper.ClientType == ClientType.Bot ? LoggingRoles.Bot :
                            LoggingRoles.Default;

                Logger.ConfigWithRole(role);
                Task.Run(() =>
                {
                    while (true)
                    {
                        if (DateTime.Now.Date != currentLogDate)
                        {
                            currentLogDate = DateTime.Now.Date;
                            Logger.ConfigWithRole(role);
                        }
                        Thread.Sleep(1000 * 60 * 10);
                    }
                });
                _logger.LogInformation($"==================RunTimeHost Startup：{_startTime.ToString("yyyy-MM-dd HH:mm:ss fff")}==================");
                _logger.LogInformation($"{Application.ProductName}-{Application.ProductVersion}:{Application.StartupPath}");
                _logger.LogInformation($"args:{string.Join(" ", args)}");
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
                Application.ThreadException += Application_ThreadException;

                _applicationContext = new RunTimeHostApplicationContext();
                AppManager.Default.Initialize(_applicationContext);

                Application.Idle += Application_Idle;
                Application.Run(_applicationContext);
                if (_runTimeServiceProvider != null)
                {
                    await _runTimeServiceProvider.SaveAndCleanCoreAsync();
                }
                ServerEnding();
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.ToString());
            }
            finally 
            {
                FreeConsole();
            }
        }

        private static GrpcServer Server
        {
            get => _server;
        }
        private static void Application_Idle(object sender, EventArgs e)
        {
            Application.Idle -= Application_Idle;
            _logger.LogInformation($"NamedPipeServer is starting...");

            var disposable = Observable.Interval(TimeSpan.FromSeconds(10))
                .Throttle(TimeSpan.FromSeconds(10))
                .Subscribe(_ => 
                {
                    Console.WriteLine($"I am alive for {(DateTime.Now - _startTime).TotalSeconds}");
                });

            ProcessServiceClientProvider processServiceClient = new ProcessServiceClientProvider();
            _runTimeServiceProvider = new RunTimeServiceProvider(_applicationContext.CurrentApplication, processServiceClient, CommandParamsHelper.IsRestart);
            _server = new GrpcServer(new ServerOptions()
            {
                Endpoint = new NamedPipeEndpoint(CommandParamsHelper.PipeName),
                NamedPipeDiagnosticLog = message => _logger.LogDebug($"[RuntimeHostPipe] pipe={CommandParamsHelper.PipeName} {message}"),
                ErrorHandler = ex => LoggingExtensions.LogError(_logger, ex, "RuntimeHost named pipe transport error")
            }, service => service.Register(RunTimeService.BindService(_runTimeServiceProvider)));
         
            Task.Run(async () =>
            {
                try
                {
                    await _runTimeServiceProvider.AutoInitOnStartupAsync();
                    await _server.StartAsync();
                    _logger.LogInformation($"NamedPipeServer is running!");
                    _logger.LogInformation($"RunTimeHost startup overhead:{(DateTime.Now - _startTime).TotalMilliseconds} ms");
                    using (var workerConnectionProvider = new WorkerConnectionProvider())
                    {
                        await workerConnectionProvider.RunAsync(_workerConnectionLifetime.Token).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    LoggingExtensions.LogError(_logger, ex, $"RunTimeHost startup failed!");
                }
            });

            ParentMonitorTask(CommandParamsHelper.Pid);
            GlobleConfigurations.ClientType = CommandParamsHelper.ClientType;

            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
            SystemEvents.SessionEnding += SystemEvents_SessionEnding;
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
            SystemEvents.DisplaySettingsChanging += SystemEvents_DisplaySettingsChanging;

            _logger.LogInformation($"CommandParam.ClientType:{CommandParamsHelper.ClientType}");
            _logger.LogInformation($"CommandParam.ParentPid:{CommandParamsHelper.Pid}");
            _logger.LogDebug($"CommandParam.PipeName:{CommandParamsHelper.PipeName}");
            _logger.LogInformation($"CommandParam.IsRestart:{CommandParamsHelper.IsRestart}");
            _logger.LogInformation($"CommandParam.ServerPort:{CommandParamsHelper.ServerPort}");
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
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, e, "UnhandledException");
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
                        Process.GetProcessById(aParentPid);
                        await Task.Delay(500);
                        continue;
                    }
                    catch (ArgumentException)
                    {
                        _logger.LogInformation($"父进程 {aParentPid} 不存在，RuntimeHost自动退出!");
                        ServerEnding();
                        Environment.Exit(500);
                    }
                    catch (Exception ex)
                    {
                        ///能其他异常情况、增加日志记录异常信息
                        _logger.LogWarning($"父进程 {aParentPid} 不存在，RuntimeHost自动退出! {ex.GetType()}:{ex.Message}");
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
        private static void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            _logger.LogInformation($"SystemEvents_PowerModeChanged:{e?.Mode}");
        }

        private static void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            _logger.LogInformation($"SystemEvents_SessionSwitch:{e?.Reason}");
        }

        private static void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
        {
            _logger.LogInformation($"SystemEvents_SessionEnding:{e?.Reason}");
        }

        private static void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            _logger.LogInformation($"SystemEvents_DisplaySettingsChanged");
        }

        private static void SystemEvents_DisplaySettingsChanging(object sender, EventArgs e)
        {
            _logger.LogInformation($"SystemEvents_DisplaySettingsChanging");
        }

        private static void ServerEnding()
        {
            _workerConnectionLifetime.Cancel();
            _server?.StartAsync();
            SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
            SystemEvents.SessionEnding -= SystemEvents_SessionEnding;
            SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
            SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
            SystemEvents.DisplaySettingsChanging -= SystemEvents_DisplaySettingsChanging;
        }

        internal const uint ATTACH_PARENT_PROCESS = 4294967295U;

        [DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        static extern bool AttachConsole(uint dwProcessId);

        [DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        static extern bool FreeConsole();
    }

}

