using II.Framework.Resource;
using static II.Framework.Resource.I18nExtensions;
using AxMSTSCLib;
using II.Framework;
using II.Framework.Models;
using II.Framework.PInvoker;
using II.Framework.Service;
using II.Framework.UI.Common;
using II.Framework.Utility;
using II.RPA.ZDeskTop.Abstracts;
using II.RPA.ZDeskTop.Models;
using II.RPA.ZDeskTop.Services;
using II.RPA.ZDeskTop.ViewModels;
using II.RPA.ZDeskTop.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using MSTSCLib;
using ReactiveUI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using Application = System.Windows.Application;
namespace II.RPA.ZDeskTop;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class ZDeskTopWindow : INotifyPropertyChanged, IControllableWindow
{
    private readonly ILogger _logger;
    private readonly string _startUser = "";
    private IMsRdpClient7 _msRdpClient7 = null;
    private IMsRdpClientAdvancedSettings6 _advancedSettings = null;
    private InputHook _inputHook;
    public event PropertyChangedEventHandler PropertyChanged;
    private readonly DirectoryInfo _currentPath = new(AppDomain.CurrentDomain.BaseDirectory);
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, string> _configurationDictionary;
    private bool _isAutoCloseFlag = false;
    private bool _closeForAuto = false;
    private static readonly int _sessionID = EnvironmentHelper.CurrentSessionId;
    public static readonly int Pid = EnvironmentHelper.CurrentProcessId;
    private static Point _lastPoint = new(-1, -1);
    private readonly BotConfigService _botConfigService;
    private readonly ControlService _controlService;
    private readonly OverlayService _overlayService;
    private readonly IDisposable _disposable;
    private WindowPInvoker _windowPInvoker;
    private System.Windows.Rect _originalWinRect;
    private Status _status;
    private readonly ConcurrentQueue<string> _capturedImages = new();
    private readonly DialogService _dialogService;
    public ZDeskTopWindow(IConfiguration configuration,
        BotConfigService botConfigService,
        ControlService controlService,
        OverlayService overlayService,
        DialogService dialogService,
        ILogger<ZDeskTopWindow> logger)
    {
        InitializeComponent();
        DataContext = this;
        _logger = logger;
        _controlService = controlService;
        _botConfigService = botConfigService;
        _configuration = configuration;
        _overlayService = overlayService;
        _dialogService = dialogService;
        _status = Status.Starting;
        _logger.LogInformation("ZDeskTopWindow ctor!");
        UpdateProcessFile(Pid.ToString());
        _configurationDictionary = _configuration.AsEnumerable().ToDictionary(c => c.Key, c => c.Value);
        if (_configurationDictionary.TryGetValue("u",out string user))
        {
            _logger.LogInformation($"u:{user}");
            _startUser = user;
            if(_startUser.Contains("indeedadmin"))
            {
                Global.IsSuperAdmin = true;
            }
            else
            {
                Global.IsSuperAdmin = false;
            }
        }
        if (_configurationDictionary.TryGetValue("c", out string client))
        {
            _logger.LogInformation($"c:{client}");
            Global.Client = client;
            UpdateClientFile(Global.Client);
        }
        if (_configurationDictionary.TryGetValue("m", out string mode))
        {
            _logger.LogInformation($"m:{client}");
            Global.IsAgent = string.Equals("agent", mode, StringComparison.InvariantCultureIgnoreCase);
            if (Global.IsAgent)
            {
                isAllowOperation = true;
            }
        }
        if (_configurationDictionary.TryGetValue("wm", out string windowmodel))
        {
            _logger.LogInformation($"wm:{windowmodel}");
            Global.IsNormalWindowMode = string.Equals("1", windowmodel, StringComparison.InvariantCultureIgnoreCase);
        }
        _logger.LogInformation("ZDeskTopWindow is running...");

        SourceInitialized += ZDeskTopWindow_SourceInitialized;
        Closing += ZDeskTopWindow_Closing;
        Closed += ZDeskTopWindow_Closed;
        Loaded += ZDeskTopWindow_Loaded;
        StartUpProgramHandle();

        SetMiniCommand = ReactiveCommand.Create(() =>
        {
            SetMiniMode(null);
        }, outputScheduler: RxApp.MainThreadScheduler);
        PauseCommand = ReactiveCommand.CreateFromTask<string,(bool,bool)>(async pause => 
        {
            try
            {
                if (pause == bool.TrueString)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        IsPausing = true;
                    });
                    _logger.LogDebug($"PasueCommand：start {pause}");
                    await _controlService.Pause();
                    await Task.Delay(100).ConfigureAwait(false);
                    _logger.LogDebug($"PasueCommand：end");
                    return (true, true);
                }
                else
                {
                    return (false, true);
                }
            }
            catch (Exception ex) 
            {
                _logger.LogDebug($"PasueCommand：failed {ex}");
                return (pause == bool.TrueString, false);
            }
        });
        SetMiniCommand.ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe();
        PauseCommand.ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(result => 
            {
                if (result.Item1)
                {
                    IsPausing = false;
                    //IsPaused = result.Item2;
                    //忽略暂停结果
                    IsPaused = false;
                }
            });
        _controlService.RunStream
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(ret =>
            {
                PauseOperationVisibility = ret ? Visibility.Visible : Visibility.Collapsed;
            });
    }

    #region properties
    private bool isAllowOperation = false;
    public bool IsAllowOperation
    {
        get { return isAllowOperation; }
        set
        {
            isAllowOperation = value;
            if (_inputHook != null)
            {
                _inputHook.Enabled = isAllowOperation;
            }
            RaisePropertyChanged();
        }
    }

    private bool isTopMost = false;
    public bool IsTopMost
    {
        get { return isTopMost; }
        set
        {
            isTopMost = value;
            Topmost = isTopMost;
            RaisePropertyChanged();
        }
    }

    public bool IsMiniMode
    {
        get;
        set;
    }

    private Visibility miniOperationVisibility = Visibility.Collapsed;
    public Visibility MiniOperationVisibility
    {
        get => miniOperationVisibility;
        set
        {
            miniOperationVisibility = value;
            RaisePropertyChanged();
        }
    }

    private Visibility pauseOperationVisibility = Visibility.Collapsed;
    public Visibility PauseOperationVisibility
    {
        get => pauseOperationVisibility;
        set
        {
            pauseOperationVisibility = value;
            RaisePropertyChanged();
        }
    }
    private bool isPausing = false;
    public bool IsPausing
    {
        get { return isPausing; }
        set 
        { 
            isPausing = value;
            RaisePropertyChanged();
        }
    }
    private bool isPaused = false;
    public bool IsPaused
    {
        get { return isPaused; }
        set 
        { 
            isPaused = value;
            RaisePropertyChanged();
        }
    }
    public void RaisePropertyChanged([CallerMemberName] string propertyName = null)
    {
        if (PropertyChanged != null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public WindowPInvoker PInvoker
    {
        get
        {
            return _windowPInvoker;
        }
    }

    public Framework.Models.Rect Rect => _originalWinRect.ToRect();

    public Status Status => _status;
    public OverlayService OverlayService => _overlayService;
    #endregion

    public bool IsMiniOn
    {
        get;
        private set;
    }

    public ReactiveCommand<Unit, Unit> SetMiniCommand
    {
        get;
        set;
    }
    public ReactiveCommand<string, (bool, bool)> PauseCommand
    {
        get;
        set;
    }

    #region window event
    private void ZDeskTopWindow_SourceInitialized(object sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        _inputHook = new InputHook(handle)
        {
            Enabled = isAllowOperation
        };
        _windowPInvoker = new WindowPInvoker(handle);
        PresentationSource source = PresentationSource.FromVisual(this);
        double dpiX = source.CompositionTarget.TransformToDevice.M11;
        double dpiY = source.CompositionTarget.TransformToDevice.M22;

        _logger.LogInformation($"Current dpi:[{dpiX},{dpiY}]");

        _logger.LogDebug("开始设置桌面尺寸...");
        try
        {
            Screen currentScreen = Screen.PrimaryScreen;

            if (int.TryParse(System.Configuration.ConfigurationManager.AppSettings.Get("StartScreen"), out int screenNo)
                && Screen.AllScreens.Length >= screenNo)
            {
                currentScreen = Screen.AllScreens[screenNo - 1];
            }
            else
            {
                currentScreen = Screen.PrimaryScreen;
            }
            float bw = currentScreen.Bounds.Width;
            float bh = currentScreen.Bounds.Height;
            if (!float.TryParse(System.Configuration.ConfigurationManager.AppSettings["ZoomRatio"], out float goldenRatio)
               || goldenRatio > 1)
            {
                goldenRatio = 0.618F;
            }
            ///虚拟桌面的分辨率、不能超过显卡所支持的分辨率
            axRDP.Width = (int)bw;
            axRDP.Height = (int)bh;
            ///窗口尺寸
            Width = (bw * goldenRatio) / dpiX;
            Height = (goldenRatio > 0.9 ? bh * goldenRatio : bh * goldenRatio + 45) / dpiY;
            Left = (currentScreen.WorkingArea.Left + (bw * (1 - goldenRatio) / 2F)) / dpiX;
            Top = (currentScreen.WorkingArea.Top + (bh * (1 - goldenRatio) / 2F)) / dpiY;

            _originalWinRect = new System.Windows.Rect(Left, Top, Width, Height);
        }
        catch (Exception ex)
        {
            _logger.LogError(new EventId(9), ex, "SettingSize");
        }
        _logger.LogDebug("结束设置桌面尺寸...");
    }

    private void ZDeskTopWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            mainBorder.BorderThickness = new Thickness(2, 2, 2, 2);
        }
        else
        {
            mainBorder.BorderThickness = new Thickness(2, 2, 2, 2);
        }
    }

    private void ZDeskTopWindow_Closing(object sender, CancelEventArgs e)
    {
        Disconnect();
        UninstallDeskTop();
        _status = Status.Exited;
        _controlService?.Dispose();
    }

    private void ZDeskTopWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _status = Status.Running;
        Title += $"-{Environment.UserName}";
        if (!Global.IsSuperAdmin)
        {
            RunProtectTask();
        }
        Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            if (Global.Client == Global.Bot)
            {
                _isAutoCloseFlag = !await _botConfigService.GetConfigOfAutoClose();
                _logger.LogInformation($"m_IsAutoCloseFlag : {_isAutoCloseFlag}");
            }
            else
            {
                _isAutoCloseFlag = false;
            }
            await _controlService.SetWindow(this);
            if (Global.IsAgent)
            {
                MiniOperationVisibility = Visibility.Visible;
                _overlayService.InitOwner(this);
                if (!Global.IsNormalWindowMode)
                {
                    SetMiniMode(null);
                }
            }
            if (!_startUser.Contains("indeedadmin.windowonly"))
            {
                _logger.LogDebug("LogOutChildSession begin");
                ChildSessionHelper.LogOutChildSession();
                _logger.LogDebug("LogOutChildSession end");
                RegisterTask(Global.Client);
                ConnectToChildSession();
                DetectTasks();
            }
        }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }

    private void ZDeskTopWindow_Closed(object sender, EventArgs e)
    {
        // 清理 pid 文件，避免主控以陈旧 pid 探测小窗状态
        ZDeskTopProcessFile.Clear();
        Application.Current.Shutdown();
    }
    private void UninstallDeskTop()
    {
        _disposable?.Dispose();
        UnregisterRdpEvent();
        Task.Run(() => 
        {
            try
            {
                StartUpProgramHelper.RestoreAllStartupPrograms();
                _logger.LogInformation($"开始删除多余任务：{SchedulerTaskHelper.CHILD_SESSION_CoreServer_TASK}");
                SchedulerTaskHelper.DeleteTask(SchedulerTaskHelper.CHILD_SESSION_CoreServer_TASK);
                _logger.LogDebug("LogOutChildSession begin");
                ChildSessionHelper.LogOutChildSession();
                _logger.LogDebug("LogOutChildSession end");
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggerExtensions.LogError(_logger, ex, "UninstallDeskTop");
            }
        });

    }
    private void UnregisterRdpEvent()
    {
        try
        {
            axRDP.OnLoginComplete -= AxRDP_OnLoginComplete;
            axRDP.OnAuthenticationWarningDisplayed -= AxRDP_OnAuthenticationWarningDisplayed;
            axRDP.OnWarning -= AxRDP_OnWarning;
            axRDP.OnConnecting -= AxRDP_OnConnecting;
            axRDP.OnConnected -= AxRDP_OnConnected;
            axRDP.OnDisconnected -= AxRDP_OnDisconnected;
            axRDP.OnLogonError -= AxRDP_OnLogonError;
            axRDP.OnFatalError -= AxRDP_OnFatalError;
        }
        catch (Exception ex)
        {
            Microsoft.Extensions.Logging.LoggerExtensions.LogError(_logger, ex, "UnregisterRdpEvent");
        }
    }
    #endregion

    
    /// <summary>
    /// 此方法异常需要忽略
    /// </summary>
    private void StartUpProgramHandle()
    {
        _logger.LogDebug("开始禁用部分开机启动项...");
        try
        {
            StartUpProgramHelper.GetAndDisableAllStartupPrograms();
        }
        catch (Exception ex)
        {
            _logger.LogError(new EventId(9), ex, "GetAndDisableAllStartupPrograms");
        }
        _logger.LogDebug("结束禁用部分开机启动项...");
    }
    private void ConnectToChildSession()
    {
        _logger.LogDebug($"开始连接{AppMeta.Default.AppName}...");
        _msRdpClient7 = axRDP.GetOcx() as IMsRdpClient7;
        _advancedSettings = _msRdpClient7.AdvancedSettings7;
        try
        {
            #region 使用相同账户开启子会话
            axRDP.Server = "localhost";

            ///使用配置的密码
            var userInfo = WindowsLogon.GetCurrentCacheUserInfo();
            if (userInfo.HasValue)
            {
                WindowsLogon.GetCurrentUserAndMachineName(out string uName, out string mName);
                if (WindowsLogon.WindowsIdentityVerify(userInfo.Value.UserName, userInfo.Value.Password, mName))
                {
                    axRDP.UserName = userInfo.Value.UserName;
                    _advancedSettings.ClearTextPassword = userInfo.Value.Password;
                }
            }

            IMsRdpExtendedSettings settings = axRDP.GetOcx() as IMsRdpExtendedSettings;
            object otrue = true;
            settings.set_Property("ConnectToChildSession", ref otrue);
            _advancedSettings.PublicMode = false;
            #endregion

            #region 用其他用户解决白屏问题,勿删
            //axRDP.Server = "127.0.0.2";
            //axRDP.AdvancedSettings2.RDPPort = 3389;
            //axRDP.AdvancedSettings9.BandwidthDetection = true;
            //axRDP.AdvancedSettings.BitmapPeristence = 1;
            //axRDP.AdvancedSettings.Compress = 1;
            //axRDP.AdvancedSettings.ContainerHandledFullScreen = 1;
            //axRDP.AdvancedSettings2.BitmapPersistence = 1;
            //axRDP.AdvancedSettings2.CachePersistenceActive = 1;
            //axRDP.AdvancedSettings2.GrabFocusOnConnect = false;
            #endregion


            _advancedSettings.EnableCredSspSupport = true;
            _advancedSettings.SmartSizing = true;
            _advancedSettings.DisplayConnectionBar = false;
            _advancedSettings.RedirectSmartCards = true;
            _advancedSettings.PluginDlls = GetInstalledPluginDlls();


            //IMsRdpClientNonScriptable5 obj = (axRDP.GetOcx() as IMsRdpClientNonScriptable5) ?? throw new Exception("get ocx failed");
            //obj.PromptForCredentials = false;
            //obj.AllowPromptingForCredentials = true;
            //obj.DisableConnectionBar = true;

            _logger.LogDebug("启用子会话功能...");

            #region register event
            axRDP.OnLoginComplete += AxRDP_OnLoginComplete;
            axRDP.OnAuthenticationWarningDisplayed += AxRDP_OnAuthenticationWarningDisplayed;
            axRDP.OnWarning += AxRDP_OnWarning;
            axRDP.OnConnecting += AxRDP_OnConnecting;
            axRDP.OnConnected += AxRDP_OnConnected;
            axRDP.OnDisconnected += AxRDP_OnDisconnected;
            axRDP.OnLogonError += AxRDP_OnLogonError;
            axRDP.OnFatalError += AxRDP_OnFatalError;
            #endregion

            _logger.LogDebug("开始连接...");
            axRDP.DesktopWidth = Screen.PrimaryScreen.Bounds.Width;
            axRDP.DesktopHeight = Screen.PrimaryScreen.Bounds.Height;
            axRDP.Connect();
        }
        catch (Exception ex)
        {
            _logger.LogError(new EventId(9), ex, "Connect");
            _logger.LogError(new EventId(9), ex.StackTrace);
            throw ex;
        }
        _logger.LogDebug($"结束连接{AppMeta.Default.AppName}...");
    }

    private void AxRDP_OnFatalError(object sender, IMsTscAxEvents_OnFatalErrorEvent e)
    {
        _logger.LogError($"OnFatalError: {Environment.UserName};{e?.errorCode}");
        _ = _dialogService.Show<RdpConnectionErrorView, MessageViewModel>(new MessageViewModel("小窗启动异常".I18n("ZDeskTopWindow.xaml_717A451C"),Localize($"登录失败:{e?.errorCode}", "ZDeskTopWindow.xaml_FF91BFB0"), "点击查看解决方案".I18n("ZDeskTopWindow.xaml_D7FC5A3C"), "https://rpa-college.ai-indeed.com/community/15.html"),this);
    }

    private void AxRDP_OnLogonError(object sender, IMsTscAxEvents_OnLogonErrorEvent e)
    {
        if (e.lError != -2)
        {
            _logger.LogError($"OnLogonError: {Environment.UserName};{e?.lError}");
            _ = _dialogService.Show<RdpConnectionErrorView, MessageViewModel>(new MessageViewModel("小窗启动异常".I18n("ZDeskTopWindow.xaml_717A451C"), Localize($"登录失败:{e?.lError}", "ZDeskTopWindow.xaml_16F92AAD"), "点击查看解决方案".I18n("ZDeskTopWindow.xaml_D7FC5A3C"), "https://rpa-college.ai-indeed.com/community/15.html"),this);
        }
    }

    private void AxRDP_OnDisconnected(object sender, IMsTscAxEvents_OnDisconnectedEvent e)
    {
        _logger.LogWarning($"OnDisconnected: {Environment.UserName}");
        _inputHook.Uninstall();

        if (!_closeForAuto)
        {
            _logger.LogInformation($"OnDisconnected: {_closeForAuto} z-desktop will be closed!");
            ExtendedDisconnectReasonCode extendedDisconnectReason = _msRdpClient7.ExtendedDisconnectReason;
            string errorDescription = _msRdpClient7.GetErrorDescription((uint)e.discReason, (uint)extendedDisconnectReason);
            _logger.LogError($"ChildSession Disconnected: {errorDescription}, Reason: {e.discReason}, ExtendedReason {extendedDisconnectReason}");
            var msg = ResolveMessage(e.discReason);
            _ = _dialogService.Show<RdpConnectionErrorView, MessageViewModel>(new MessageViewModel("小窗启动异常".I18n("ZDeskTopWindow.xaml_717A451C"),
                msg, 
                "查看解决方案".I18n("ZDeskTopWindow.xaml_7B1F2724"), 
                "https://rpa-college.ai-indeed.com/community/15.html"),this);
            Close();
        }
    }

    private void AxRDP_OnConnected(object sender, EventArgs e)
    {
        _logger.LogInformation($"OnConnected: {Environment.UserName}");
        _inputHook?.Install();
        _controlService.SetReady();
    }

    private void AxRDP_OnConnecting(object sender, EventArgs e)
    {
        _logger.LogInformation($"OnConnecting: {Environment.UserName}");
    }

    private void AxRDP_OnWarning(object sender, IMsTscAxEvents_OnWarningEvent e)
    {
        _logger.LogInformation($"OnWarning: {Environment.UserName}");
    }

    private void AxRDP_OnAuthenticationWarningDisplayed(object sender, EventArgs e)
    {
        _logger.LogError($"登录失败：{axRDP.UserName}");
    }

    private void AxRDP_OnLoginComplete(object sender, EventArgs e)
    {
        _logger.LogInformation($"OnLoginComplete: {Environment.UserName}");
        if (_isAutoCloseFlag)
        {
            RunAutoCloseTask();
        }
    }

    private void RegisterTask(string rootClient)
    {
        var arguments = string.Join(" ", Environment.GetCommandLineArgs());
        using (var owner = Process.GetCurrentProcess())
        {
            arguments = $"-zdesktop-owner-pid {owner.Id} -zdesktop-owner-start {owner.StartTime.ToUniversalTime().Ticks} {arguments}";
        }
        try
        {
            _logger.LogInformation($"开始删除计划任务：{SchedulerTaskHelper.CHILD_SESSION_CoreServer_TASK}");
            SchedulerTaskHelper.DeleteTask(SchedulerTaskHelper.CHILD_SESSION_CoreServer_TASK);

        }
        catch (Exception ex)
        {
            _logger.LogError(new EventId(9), ex, ex.Message);
            Thread.Sleep(1000);
            _logger.LogError($"删除计划任务异常、忽略");
        }

        try
        {
            _logger.LogInformation($"开始注册计划任务：{SchedulerTaskHelper.CHILD_SESSION_CoreServer_TASK}");
            SchedulerTaskHelper.CreateTask(SchedulerTaskHelper.CHILD_SESSION_CoreServer_TASK,
                Path.Combine(_currentPath.Parent.FullName, "II.RPA.PluginLoader", "II.RPA.Core.Server.exe"),
                "Z-DeskTop",
                0,
                $"-client Z-DeskTop -type info -runpath \"{Path.Combine(_currentPath.Parent.FullName, "II.RPA.PluginLoader")}\" -rootClient {rootClient} {arguments}",
                injectDotnetRoot: true);

            _logger.LogInformation($"注册成功：{SchedulerTaskHelper.CHILD_SESSION_CoreServer_TASK}");
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogError($"没有注册注册计划任务权限,需要使用管理员权限启动程序！");
            throw new InvalidOperationException(Localize($"{AppMeta.Default.AppName}启动失败,请使用管理员身份重试！", "ZDeskTopWindow.xaml_9F9A4E3F"));
        }
        catch (Exception ex)
        {
            _logger.LogError(new EventId(9), ex, ex.Message);
            Thread.Sleep(1000);
            _logger.LogError($"注册计划任务异常、重试...");

            _logger.LogInformation($"开始注册计划任务：{SchedulerTaskHelper.CHILD_SESSION_CoreServer_TASK}");
            SchedulerTaskHelper.CreateTask(SchedulerTaskHelper.CHILD_SESSION_CoreServer_TASK,
                Path.Combine(_currentPath.Parent.FullName, "II.RPA.PluginLoader", "II.RPA.Core.Server.exe"),
                "Z-DeskTop",
                0,
                $"-client Z-DeskTop -type info -runpath \"{Path.Combine(_currentPath.Parent.FullName, "II.RPA.PluginLoader")}\" -rootClient {rootClient} {arguments}",
                injectDotnetRoot: true);

            _logger.LogInformation($"注册成功：{SchedulerTaskHelper.CHILD_SESSION_CoreServer_TASK}");
        }
    }
    private void DetectTasks()
    {
        Task.Run(() =>
        {
            try
            {
                var taskInfos = SchedulerTaskHelper.EnumerateTask(100);

                if (taskInfos != null && taskInfos.Any())
                {
                    foreach (var item in taskInfos)
                    {
                        _logger.LogInformation($"TaskInfo: {item}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(new EventId(9), ex, "DetectTasks");
            }
        });
    }
    public static bool IsChildSessionEnabled()
    {
        try
        {
            Wtsapi32Invoker.WTSIsChildSessionsEnabled(out var isEnabled);
            return isEnabled;
        }
        catch (Exception innerException)
        {
            throw new InvalidOperationException(innerException.Message);
        }
    }
    private void Button_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start("https://rpa-college.ai-indeed.com/document/416e98c01cdd11ecad6c95370d4092b0");
        }
        catch (Exception ex)
        {
            Microsoft.Extensions.Logging.LoggerExtensions.LogError(_logger, ex, "open help");
        }
    }
    private void Setting_Click(object sender, RoutedEventArgs e)
    {
        LoginWindow messageWindow = new LoginWindow(true)
        {
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this
        };
        messageWindow.ShowDialog();
    }
    private void UpdateProcessFile(string aPid)
    {
        ZDeskTopProcessFile.Write(aPid);
    }
    private string GetInstalledPluginDlls()
    {
        List<string> list = new();
        try
        {
            using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Terminal Server Client\\Default\\AddIns", writable: false);
            if (registryKey != null)
            {
                string[] subKeyNames = registryKey.GetSubKeyNames();
                foreach (string name in subKeyNames)
                {
                    using RegistryKey registryKey2 = registryKey.OpenSubKey(name, writable: false);
                    object value = registryKey2.GetValue("Name");
                    if (value != null && (value.ToString().ToLower().EndsWith(".dll")|| value.ToString().ToLower().EndsWith(".DLL")))
                    {
                        list.Add(value.ToString());
                    }
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(new EventId(9),exception, "GetInstalledPluginDlls");
        }
        return string.Join(",", list);
    }
    private void Disconnect()
    {
        try
        {
            lock (this)
            {
                _logger.LogInformation("start disconnecting child session");
                Dispatcher.Invoke(() => 
                {
                    axRDP.OnDisconnected -= AxRDP_OnDisconnected;
                    if (axRDP.Connected != 0)
                    {
                        axRDP.Disconnect();
                    }
                });
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(new EventId(9), exception, "Disconnect");
        }
    }
    private void RunAutoCloseTask()
    {
        Task.Factory.StartNew(async () =>
        {
            bool coreServerAlive = false;
            while (true)
            {
                await Task.Delay(500);
                Process[] processes = Process.GetProcesses();

                var sessionHubProcess = processes.FirstOrDefault(p => p.SessionId != _sessionID
                && p.ProcessName.Contains("II.RPA.Core.Server"));
                if (sessionHubProcess != null)
                {
                    coreServerAlive = true;
                    await Task.Delay(500);
                    continue;
                }
                else
                {
                    if (coreServerAlive)
                    {
                        _logger.LogInformation("II.RPA.Core.Server has been exited!");
                        break;
                    }
                    else
                    {
                        _logger.LogInformation("II.RPA.Core.Server has not been started!");
                        await Task.Delay(500);
                        continue;
                    }
                }
            }
            _logger.LogInformation("ZDeskTop will be closed!");
            _closeForAuto = true;
            Dispatcher.Invoke(() => { Close(); });
            _logger.LogInformation("RunAutoCloseTask end");
        }, TaskCreationOptions.LongRunning);
    }
    private void RunProtectTask()
    {
        Task.Factory.StartNew(async () =>
        {
            string clientstring = Global.Client;
            while (true)
            {
                await Task.Delay(100);
                string idString = File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Z-Pid", $"{clientstring}.txt"));
                if (int.TryParse(idString, out int id) && Process.GetProcesses().FirstOrDefault(p => p.Id == id) != null)
                {
                    await Task.Delay(1000);
                }
                else
                {
                    try
                    {
                        Disconnect();
                        UninstallDeskTop();
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggerExtensions.LogError(_logger, ex, "退出前清理异常");
                    }
                    finally
                    {
                        _logger.LogInformation($"检测不到 {clientstring}-{idString} 进程、{AppMeta.Default.AppName} 已退出");
                        Environment.Exit(0);
                    }
                }
            }
        }, TaskCreationOptions.LongRunning).ConfigureAwait(false);
    }

    #region IControllableWindow
    public void Minimize()
    {
        Application.Current.Dispatcher.Invoke(() => 
        {
            if (WindowState != WindowState.Minimized)
            {
                WindowState = WindowState.Minimized;
            }
        });
    }

    public void Maximize()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (WindowState != WindowState.Maximized)
            {
                WindowState = WindowState.Maximized;
            }
        });
    }

    public void AllowOperation(bool isAllow)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            IsAllowOperation = isAllow;
        });
    }

    public void SetTopmost(bool isTopmost)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            IsTopMost = isTopmost;
        });
    }

    public void SetMiniMode(ParentWindowModel parentInfo)
    {
        IsMiniOn = true;
        Application.Current.Dispatcher.Invoke(() =>
        {
            IsMiniMode = true;
            if (_windowPInvoker != null)
            {
                if (WindowState != WindowState.Normal)
                {
                    WindowState = WindowState.Normal;
                }
                WindowStyle = WindowStyle.SingleBorderWindow;
                Width = 372;
                Height = 213;
                WindowStartupLocation = WindowStartupLocation.Manual;
                if (!RestoreLastWindowPosition())
                {
                    PositionRightBottomCorner(this, 10, 10);
                }
                ShowInTaskbar = false;
                IsTopMost = true;
                ResizeMode = ResizeMode.NoResize;
                ChromeHeight = new GridLength(0);
            }
            _overlayService.InitOwner(this);
            _overlayService.Enable();
            _overlayService.Show();
            Activate();
            AllowOperation(false);
        });
    }
    public void CancelMiniMode()
    {
        IsMiniOn = false;
        Application.Current.Dispatcher.Invoke(() =>
        {
            SaveLastWindowPosition(this);
            IsMiniMode = false;
            if (_windowPInvoker != null)
            {
                ChromeHeight = GridLength.Auto;
                WindowStyle = WindowStyle.SingleBorderWindow;
                WindowStartupLocation = WindowStartupLocation.Manual;
                Left = _originalWinRect.Left;
                Top = _originalWinRect.Top;
                Height = _originalWinRect.Height;
                Width = _originalWinRect.Width;
                ShowInTaskbar = true;
                ResizeMode = ResizeMode.CanResize;
                IsTopMost = false;
                Activate();
                //_windowPInvoker.ResetWindow((int)Left, (int)Top, (int)Width, (int)Height);
            }
            AllowOperation(true);
        });
    }
    public void ActivateWindow()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Activate();
        });
    }
    public void SetWindowRegion(int x, int y, int w, int h)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _windowPInvoker.SetPosition(x, y, w, h);
        });
    }

    public IObservable<SnapshotModel> Capture(int intervalms)
    {
        string root = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Z-Core";
        _windowPInvoker
            .SetPosition(-3333,
                         -3333,
                         (int)_originalWinRect.Width,
                         (int)_originalWinRect.Height)
            .Sleep(100);


        return Observable.Interval(TimeSpan.FromMilliseconds(intervalms))
                         .Select(x =>
                         {
                             _windowPInvoker.CaptureWindow(out System.Drawing.Bitmap bitmap);                              
                             return new SnapshotModel(bitmap,x);
                         });
    }

    private static void DeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                Console.WriteLine($"Deleted: {path}");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error deleting file {path}: {ex.Message}");
        }
    }
    #endregion

    private void UpdateClientFile(string client)
    {
        lock (this)
        {
            var fileName = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Z-Core\\Client.txt";
            DeleteFile(fileName);
            if (!Directory.Exists(Path.GetDirectoryName(fileName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            }
            File.WriteAllText(fileName, client, System.Text.Encoding.Default);
        }
    }

    public static void PositionRightBottomCorner(Window window, double horizontalOffset = 0, double verticalOffset = 0)
    {
        var workingArea = Screen.PrimaryScreen.WorkingArea;
        var dpi = DpiHelper.TransformToPixels(window);
        double left = workingArea.Right - window.Width * dpi - horizontalOffset;
        double top = workingArea.Bottom - window.Height * dpi - verticalOffset;
        window.Left = left / dpi;
        window.Top = top /dpi;

        //RestoreLastWindowPosition();
    }
    private static string ResolveMessage(int code)
    {
        string message = code switch
        {
            516 => Localize($"本机远程桌面服务不可用，请参考帮助文档解决", "ZDeskTopWindow.xaml_A77BB03F"),
            519 => Localize($"本机远程桌面服务不可用，请参考帮助文档解决", "ZDeskTopWindow.xaml_A77BB03F"),
            _ => Localize($"本机远程桌面服务不可用，请参考帮助文档解决", "ZDeskTopWindow.xaml_A77BB03F"),
        };
        return message;
    }

    public static void SaveLastWindowPosition(Window window)
    {
        Application.Current.Dispatcher.Invoke(() => 
        {
            _lastPoint = new(window.Left, window.Top);
        });
    }

    public bool RestoreLastWindowPosition()
    {
        if (double.IsNaN(_lastPoint.X) || double.IsNaN(_lastPoint.Y) ||
         double.IsInfinity(_lastPoint.X) || double.IsInfinity(_lastPoint.Y) ||
         _lastPoint.X < 0 || _lastPoint.Y < 0)
        {
            return false;
        }

        return Application.Current.Dispatcher.Invoke(() =>
        {
            if (PresentationSource.FromVisual(this) is not HwndSource source) return false;

            double dpiX = 96.0, dpiY = 96.0;
            if (source.CompositionTarget != null)
            {
                dpiX *= source.CompositionTarget.TransformToDevice.M11;
                dpiY *= source.CompositionTarget.TransformToDevice.M22;
            }

            var pxX = _lastPoint.X * dpiX / 96.0;
            var pxY = _lastPoint.Y * dpiY / 96.0;

            bool inside = false;
            foreach (var screen in Screen.AllScreens)
            {
                if (screen.Bounds.Contains((int)pxX, (int)pxY))
                {
                    inside = true;
                    break;
                }
            }

            if (!inside)
                return false;

            Left = _lastPoint.X;
            Top = _lastPoint.Y;
            return true;
        });
    }
}

