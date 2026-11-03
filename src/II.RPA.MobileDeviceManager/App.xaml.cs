using Castle.Core.Logging;
using II.RPA.Communication.Endpoints;
using II.RPA.Communication.Hosting;
using II.RPA.Communication.Options;
using II.RPA.Logging;
using II.RPA.Logging.Definitions;
using II.RPA.MobileDeviceManager.Common;
using II.RPA.MobileDeviceManager.GrpcService.Interface;
using II.RPA.MobileDeviceManager.GrpcService.Service;
using II.RPA.MobileDeviceManager.Log;
using II.RPA.MobileDeviceManager.Models;
using II.RPA.MobileDeviceManager.ViewModels;
using II.RPA.MobileDeviceManager.Views;
using Newtonsoft.Json;
using Rpa.Core.Mobile;
using Rpa.Core.Mobileplugin;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Unity;
using Wpf.Ui;
using Wpf.Ui.Controls;
using ILogger = II.RPA.MobileDeviceManager.Log.ILogger;

namespace II.RPA.MobileDeviceManager
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private static GrpcServer _server;
        public new static App Current => (App)Application.Current;
        public IUnityContainer ServiceContainer { get; set; }
        private static ILogger m_Logger;
        private static System.Threading.Mutex AppMutex;
        private MobilePluginService.MobilePluginServiceClient _mobileServiceClient;
        private GrpcClient _mobileGrpcClient;
        private string _channelName;
        private readonly SemaphoreSlim _providerConnectionLock = new SemaphoreSlim(1, 1);
        private static readonly string _mobileChannelFileName = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Z-Core\\Z-Mobile.txt";


        public App()
        {
            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            //工作目录
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            //动画帧数 默认60
            Timeline.DesiredFrameRateProperty.OverrideMetadata(
               typeof(Timeline),
               new FrameworkPropertyMetadata { DefaultValue = 60 }
               );

            //渲染模式
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
        }
        protected override async void OnStartup(StartupEventArgs e)
        {
            string MyAppID = "II.RPA.MobileManager.App";//运行进程互斥标识名称 
            AppMutex = new System.Threading.Mutex(true, MyAppID);
            if (AppMutex.WaitOne(0, false))
            {
                try
                {
                    var role = CommandParamsHelper.ClientType == ClientType.Factory ? LoggingRoles.Factory :
                            CommandParamsHelper.ClientType == ClientType.Bot ? LoggingRoles.Bot :
                            LoggingRoles.Default;

                    Logger.ConfigWithRole(role);

                    ServiceContainer = new UnityContainer()
                    .RegisterSingleton<Subject<GlobalEventSubModel>>()
                    .RegisterSingleton<Subject<GlobalExceptionSubModel>>()
                    .RegisterSingleton<Subject<ConnectEventSubModel>>()
                    .RegisterSingleton<ILogger, AdaptedLogger>()
                    .RegisterSingleton<CheckConnectMobileService>()
                    .RegisterSingleton<SyncMobilePageSourceService>()
                    .RegisterSingleton<SyncMobileConnectExService>()
                    .RegisterSingleton<IContentDialogService, ContentDialogService>()
                    .RegisterSingleton<MainViewModel>()
                    .RegisterSingleton<MainWindow>()
                    .RegisterSingleton<DeviceScreenView>()
                    .RegisterSingleton<DeviceScreenViewModel>()
                    .RegisterSingleton(typeof(ObjectRef<>), typeof(ObjectRef<>));

                    m_Logger = ServiceContainer.Resolve<ILogger>();
                    m_Logger.LogInfo($"启动手机应用");
                    var globalEventSubject = ServiceContainer.Resolve<Subject<GlobalEventSubModel>>();
                    globalEventSubject.OnNext(new GlobalEventSubModel(EventType.AppStarting, "OnStartup"));

                    //todo 处理启动参数
                    var commandParams = e.Args;
                    m_Logger.LogInfo($"参数ActionType：{CommandParamsHelper.ActionType}");
                    //关闭模式
                    Current.ShutdownMode = ShutdownMode.OnMainWindowClose;

                    // try先为了提供接口，容错后面处理
                    MobileDeviceManager.Instance.IsInstallPlugin = CommandParamsHelper.ActionType == 0 && AdbHelper.IsInstallMobileTools() ? true : false;
                    MobileDeviceManager.Instance.ServerPort = CommandParamsHelper.ServerPort;
                    //await MobileDeviceManager.Instance.StartAppiumServer();
                    InitDevices();
                    StartMobileHost();

                    if (CommandParamsHelper.Pid != 0)
                    {
                        ParentMonitorTask(CommandParamsHelper.Pid);
                    }
                    var win = ServiceContainer.Resolve<MainWindow>();

                    ////bot调用时隐藏主窗口不显示
                    if (CommandParamsHelper.ClientType == ClientType.Bot)
                    {
                        win.Opacity = 0;
                        win.AllowsTransparency = true;
                        win.WindowStyle = WindowStyle.None;
                        win.ShowInTaskbar = false;
                        win.Visibility = Visibility.Hidden;
                    }
                    win.Show();

                    base.OnStartup(e);
                }
                catch (Exception ex)
                {
                    m_Logger?.LogError(ex);
                    throw;
                }
            }
            else
            {
                this.Shutdown();//已存在运行进程 执行关闭
            }
        }
        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                _server?.StopAsync().GetAwaiter().GetResult();
                //ProcessHelper.KillProcessAndChildren(Process.GetCurrentProcess().Id);
                //ProcessHelper.KillProcessAndChildrenByName("II.RPA.MobileScreen");
                ExitChildProcess();
                Environment.Exit(0);
                base.OnExit(e);
            }
            catch (Exception ex)
            {
                m_Logger?.LogError($"关闭主窗口出现异常：{ex}");
                try
                {
                    _server?.StopAsync().GetAwaiter().GetResult();
                }
                catch { }
                Environment.Exit(0);
                base.OnExit(e);
            }
        }
        public GrpcServer Server
        {
            get => _server;
        }

        public MobilePluginService.MobilePluginServiceClient MobileServiceClient
        {
            get => _mobileServiceClient;
        }
        public void StartGrpcServer(IMobileService mobileService)
        {
            var service = new MobileDeviceManagerGRPCService(mobileService);
            _server = new GrpcServer(new ServerOptions
            {
                Endpoint = new NamedPipeEndpoint(CommandParamsHelper.PipeName),
                ErrorHandler = NamedPipeServer_Error
            }, services => services.Register(
                MobileDeviceManagerService.BindService(service),
                binder => MobileDeviceManagerService.BindService(binder, service)));
            _server.StartAsync().GetAwaiter().GetResult();
        }

        private static void NamedPipeServer_Error(Exception e)
        {
            if (e is ObjectDisposedException)
            {
                //ignore
                return;
            }
            m_Logger.LogError(e, "NamedPipeServerError");
        }
        private static void ParentMonitorTask(int aParentPid)
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    try
                    {
                        Process.GetProcessById(aParentPid);
                        await Task.Delay(1000);
                        continue;
                    }
                    catch
                    {
                        m_Logger.LogInfo($"父进程 {aParentPid} 不存在，RunTimeHost自动退出!");
                        _server?.StopAsync().GetAwaiter().GetResult();
                        //ProcessHelper.KillProcessAndChildren(Process.GetCurrentProcess().Id);
                        //ProcessHelper.KillProcessAndChildrenByName("II.RPA.MobileScreen");
                        ExitChildProcess();
                        Environment.Exit(500);
                    }
                    break;
                }
            }, TaskCreationOptions.LongRunning)
                .ConfigureAwait(false)
                .GetAwaiter()
                .OnCompleted(() => { });
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            m_Logger?.LogError($"未捕获到的Task任务异常：{e}");
            e.SetObserved();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            m_Logger?.LogError($"未捕获到的非UI线程异常：{e}");
            if (e.IsTerminating)
            {
                //clr结束
            }
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            m_Logger?.LogError($"未捕获到的UI线程异常：{e}");
            e.Handled = true;
        }

        private void InitDevices()
        {
            try
            {
                string deviceFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Z-Factory/mobileDeviceList.json");
                if (File.Exists(deviceFilePath))
                {
                    string deviceContent = File.ReadAllText(deviceFilePath);
                    if (!string.IsNullOrEmpty(deviceContent))
                    {
                        m_Logger?.LogInfo($"同步历史设备列表数据：{deviceContent}");
                        MobileDeviceManager.Instance.DeviceInfos = JsonConvert.DeserializeObject<List<DeviceInfo>>(deviceContent);
                    }
                }

            }
            catch (Exception e)
            {
                m_Logger?.LogError($"同步历史设备列表数据异常：{e}");
            }
        }

        public void StartMobileHost()
        {
            if (!File.Exists(_mobileChannelFileName))
            {
                m_Logger?.LogError("手机组件能力服务管道配置不存在");
                Shutdown();
                return;
            }

            var channelName = File.ReadAllText(_mobileChannelFileName).Trim();
            if (string.IsNullOrWhiteSpace(channelName))
            {
                m_Logger?.LogError("手机组件能力服务管道配置为空");
                Shutdown();
                return;
            }

            SetMobileProviderClient(channelName);
        }

        private void SetMobileProviderClient(string channelName)
        {
            var grpcClient = new GrpcClient(new ClientOptions
            {
                Endpoint = new NamedPipeEndpoint(channelName),
                NamedPipeConnectionTimeoutMilliseconds = 2000
            });
            var serviceClient = grpcClient.CreateClient(invoker => new MobilePluginService.MobilePluginServiceClient(invoker));
            _mobileGrpcClient = grpcClient;
            _mobileServiceClient = serviceClient;
            _channelName = channelName;
            MobileDeviceManager.Instance.mobileServerClint = serviceClient;
            m_Logger?.LogInfo($"手机Provider客户端管道名：{channelName}");
        }

        private static async Task CheckMobileProviderAsync(string channelName)
        {
            if (string.IsNullOrWhiteSpace(channelName))
                throw new InvalidOperationException("手机Provider管道名为空");

            var grpcClient = new GrpcClient(new ClientOptions
            {
                Endpoint = new NamedPipeEndpoint(channelName),
                NamedPipeConnectionTimeoutMilliseconds = 2000
            });
            try
            {
                var healthClient = grpcClient.CreateClient(invoker => new MobilePluginService.MobilePluginServiceClient(invoker));
                var response = await healthClient.CheckServiceHeartbeatUnaryAsync(
                    new Rpa.Core.Mobileplugin.MobileRequest(), deadline: DateTime.UtcNow.AddSeconds(3));
                if (response.Code != "0")
                    throw new InvalidOperationException($"手机Provider心跳异常：{response.Code} {response.Message}");
            }
            finally
            {
                await grpcClient.DisposeAsync();
            }
        }

        public async Task EnsureMobileProviderAvailableAsync()
        {
            await _providerConnectionLock.WaitAsync();
            try
            {
                var fileChannelName = File.Exists(_mobileChannelFileName)
                    ? File.ReadAllText(_mobileChannelFileName).Trim() : string.Empty;
                if (!string.IsNullOrWhiteSpace(fileChannelName) &&
                    !string.Equals(_channelName, fileChannelName, StringComparison.Ordinal))
                {
                    try
                    {
                        await CheckMobileProviderAsync(fileChannelName);
                        SetMobileProviderClient(fileChannelName);
                        return;
                    }
                    catch (Exception ex)
                    {
                        m_Logger?.LogWarning($"新手机Provider管道检查失败，管道名：{fileChannelName}，错误：{ex}");
                    }
                }

                await CheckMobileProviderAsync(_channelName);
            }
            finally
            {
                _providerConnectionLock.Release();
            }
        }

        private static void ExitChildProcess()
        {
            var pids = II.RPA.WinAutomation.CommonUtility.ProcessHelper.GetListChildProcessesId(Process.GetCurrentProcess().Id);
            foreach (var pid in pids)
            {
                try
                {
                    m_Logger.LogDebug($"关闭子进程，pid为【{pid}】");
                    Process process = Process.GetProcessById(pid);
                    process?.Kill();
                }
                catch (Exception ex)
                {
                    m_Logger.LogWarning($"关闭子进程失败，pid为【{pid}】", ex);
                }
            }
        }
    }
}
