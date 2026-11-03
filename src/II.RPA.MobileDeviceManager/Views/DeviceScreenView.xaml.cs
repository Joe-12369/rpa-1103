using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using II.RPA.MobileDeviceManager.Common;
using II.RPA.MobileDeviceManager.Models;
using II.RPA.MobileDeviceManager.Services;
using II.RPA.MobileDeviceManager.ViewModels;
using II.RPA.MobileDeviceManager.Win32;
using IPA.Ubases.States;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Unity;
using II.RPA.MobileDeviceManager.Log;
using OpenQA.Selenium.Interactions.Internal;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ScrollBar;
using System.Reactive.Subjects;
using ProcessHelper = II.RPA.MobileDeviceManager.Common.ProcessHelper;

namespace II.RPA.MobileDeviceManager.Views
{
    /// <summary>
    /// DeviceScreenView.xaml 的交互逻辑
    /// </summary>
    public partial class DeviceScreenView : UserControl
    {
        private ILogger logger;
        private WindowsFormsHost _windowsFormsHost;
        private System.Windows.Forms.Panel _pnlSched;
        private IntPtr _mobileHandle = (IntPtr)0;
        private Process _process;
        private MobileScreenMirrorService _screenMirrorService;
        private ScrcpyInputMonitor _scrcpyInputMonitor;
        //private string deviceId = "";
        private string deviceName = "";
        public BitmapImage _bitmapImage;
        private System.Windows.Controls.Image image;
        public double _bitmapImageWidth;
        private Grid gridhost;
        public double _bitmapImageHeight;
        private double _scale;
        public double _dpiX = 1.0;
        public double _dpiY = 1.0;
        public Grid grid;
        private DispatcherTimer AutoConnectDispatcherTimer = new DispatcherTimer();
        private readonly CurrentDeviceGlobal rxCurrentDevice;
        private string _mobileToolsTargetPath = System.IO.Path.Combine(Environment.GetEnvironmentVariable("ALLUSERSPROFILE"), "II.RPA");
        public DeviceScreenViewModel deviceScreenViewModel;
        private readonly Subject<GlobalEventSubModel> _eventSubject;
        private readonly Subject<GlobalExceptionSubModel> _exceptionSubject;
        private readonly Subject<ConnectEventSubModel> _connectSubJect;
        private MobileDeviceManager deviceManager;
        private MobileRotation _mobileRotation = MobileRotation.Vertical;
        private Rect _currentDisplaySize;
        private DateTime _lastDisplayOrientationCheckUtc;
        private DateTime _nextOrientationScreenshotUtc;
        private int _orientationScreenshotFailures;
        private int _orientationUpdateRunning;
        private bool _updatingMirrorLayout;
        private readonly SemaphoreSlim _connectionTransition = new SemaphoreSlim(1, 1);
        private long _connectionRequestVersion;
        private IDisposable _connectSubscription;
        private IDisposable _exceptionSubscription;
        private IDisposable _orientationSubscription;
        private bool _viewActive = true;
        public bool isConnecting;

        public MainViewModel _mainViewModel;
        public DeviceScreenView()
        {
            InitializeComponent();
            logger = App.Current.ServiceContainer.Resolve<ILogger>();
            _eventSubject = App.Current.ServiceContainer.Resolve<Subject<GlobalEventSubModel>>();
            _exceptionSubject = App.Current.ServiceContainer.Resolve<Subject<GlobalExceptionSubModel>>();
            _connectSubJect = App.Current.ServiceContainer.Resolve<Subject<ConnectEventSubModel>>();
            _mainViewModel = App.Current.ServiceContainer.Resolve<MainViewModel>();
            _mainViewModel.CaptureElement = CaptureElementHandle;
            _mainViewModel.CapturePoint = CapturePointHandle;
            _mainViewModel.CancelCapture = () => overlayWnd?.Close();
            MobileElement.OnCheckElement = CheckElementHandle;
            deviceScreenViewModel = App.Current.ServiceContainer.Resolve<DeviceScreenViewModel>();
            this.DataContext = deviceScreenViewModel;
            deviceManager = MobileDeviceManager.Instance;
            _screenMirrorService = new MobileScreenMirrorService(logger);
            _scrcpyInputMonitor = new ScrcpyInputMonitor(
                () => MobileDeviceManager.Instance.mobileDeviceRect,
                () => MobileDeviceManager.Instance.IsCaptureMode,
                reason =>
                {
                    if (deviceScreenViewModel.IsConnected)
                    {
                        deviceManager.MarkPageSourceDirty(reason);
                    }
                });
            //deviceManager.ChangeMobileRotation = ChangeRotationHandle;
            ChangeRotation(deviceManager.MobileRotation);
            KillMobileScreenProcess();
            this.rxCurrentDevice = App.Current.ServiceContainer.Resolve<ObjectRef<CurrentDeviceGlobal>>().Get(() => new CurrentDeviceGlobal());
            Loaded += DeviceScreenView_Loaded;
            Unloaded += DeviceScreenView_Unloaded;
            SubscribeToDeviceEvents();
            startGetPoint();
        }

        private void SubscribeToDeviceEvents()
        {
            if (_connectSubscription != null) return;

            //this.rxCurrentDevice.CurrentDeviceIdObs.Value.Subscribe(async devId =>
            //{
            //    if (!string.IsNullOrEmpty(devId) && deviceId != devId)
            //    {
            //        deviceScreenViewModel.IsConnected = true;
            //        deviceId = devId;
            //        deviceScreenViewModel.DeviceName = this.rxCurrentDevice.CurrentDeviceName;
            //        await StartMobileScreen();
            //    }
            //    if (string.IsNullOrEmpty(devId))
            //    {
            //        MobileDeviceManager.Instance.IsConnected = false;
            //        deviceScreenViewModel.IsConnected = false;
            //        deviceScreenViewModel.ConnectStatus = 1;
            //        deviceScreenViewModel.DeviceName = "";
            //        await Task.Run(() =>
            //        {
            //            MobileDeviceManager.Instance.IsConnected = false;
            //            ReleaseWindowsFormsHost();
            //        });
            //        deviceId = "";
            //    }
            //});

            _connectSubscription = _connectSubJect.ObserveOn((System.Threading.SynchronizationContext.Current ?? new System.Threading.SynchronizationContext())).Subscribe(t =>
            {
                long version = Interlocked.Increment(ref _connectionRequestVersion);
                _ = HandleConnectionChangeAsync(t, version);
            });


            _exceptionSubscription = _exceptionSubject.ObserveOn((System.Threading.SynchronizationContext.Current ?? new System.Threading.SynchronizationContext())).Subscribe(
                onNext: async t =>
                {
                    switch (t.ExceptionType)
                    {
                        case ExceptionType.AppiumEx:
                            string affectedDevice = rxCurrentDevice.CurrentDeviceId;
                            long affectedVersion = Interlocked.Read(ref _connectionRequestVersion);
                            if (!_viewActive || string.IsNullOrEmpty(affectedDevice)) break;
                            await _connectionTransition.WaitAsync();
                            try
                            {
                                if (_viewActive && affectedVersion == Interlocked.Read(ref _connectionRequestVersion) && affectedDevice == rxCurrentDevice.CurrentDeviceId
                                    && MobileDeviceManager.Instance.IsConnected && !MobileDeviceManager.Instance.IsConnecting)
                                {
                                    MobileDeviceManager.Instance.IsConnected = false;
                                    deviceScreenViewModel.IsConnected = false;
                                    _eventSubject.OnNext(new GlobalEventSubModel(EventType.MoblieConnectFailed, ""));
                                    var device = rxCurrentDevice.DeviceInfos.FirstOrDefault(a => a.Udid == affectedDevice);
                                    if (device != null) device.IsConnected = false;
                                    ReleaseWindowsFormsHost();
                                    rxCurrentDevice.CurrentDeviceId = "";
                                    MsgTipsHelper.ShowWarningPageInMainFrame(Localize($"手机连接异常!", "DeviceScreenView.xaml_1E5B8F1A"), Application.Current.MainWindow);
                                }
                            }
                            finally
                            {
                                _connectionTransition.Release();
                            }
                            break;
                        case ExceptionType.ScreenEx:
                            break;
                        case ExceptionType.OtherEx:
                            break;
                    }
                },
                onError: ex =>
                {
                    //_logger.LogError(ex);
                },
                onCompleted: () =>
                {

                });
            _orientationSubscription = Observable.Interval(TimeSpan.FromMilliseconds(100)).Subscribe(async d =>
            {
                if (!_viewActive || Interlocked.CompareExchange(ref _orientationUpdateRunning, 1, 0) != 0) return;
                try
                {
                    if (MobileDeviceManager.Instance.IsConnected
                        && !MobileDeviceManager.Instance.IsConnecting)
                    {
                        // The display still changes when PageSource cannot be obtained.
                        if (DateTime.UtcNow - _lastDisplayOrientationCheckUtc >= TimeSpan.FromSeconds(2)
                            && !string.IsNullOrEmpty(rxCurrentDevice.CurrentDeviceId))
                        {
                            _lastDisplayOrientationCheckUtc = DateTime.UtcNow;
                            var id = rxCurrentDevice.CurrentDeviceId;
                            try
                            {
                                // PageSource was the original source of orientation. Use ADB screenshot
                                // only if no recent PageSource is available for this device.
                                Rect size;
                                bool hasSize = deviceManager.TryGetRecentScreenshotDisplaySize(id, out size)
                                    || deviceManager.TryGetRecentPageSourceDisplaySize(id, out size);
                                if (hasSize)
                                {
                                    _orientationScreenshotFailures = 0;
                                    _nextOrientationScreenshotUtc = DateTime.MinValue;
                                }
                                else if (!deviceManager.IsScreenshotInProgress
                                    && DateTime.UtcNow >= _nextOrientationScreenshotUtc)
                                {
                                    try
                                    {
                                        size = await deviceManager.GetCurrentDisplaySizeAsync(id);
                                        hasSize = true;
                                        _orientationScreenshotFailures = 0;
                                        _nextOrientationScreenshotUtc = DateTime.MinValue;
                                    }
                                    catch
                                    {
                                        if (_viewActive && id == rxCurrentDevice.CurrentDeviceId)
                                        {
                                            _orientationScreenshotFailures = Math.Min(_orientationScreenshotFailures + 1, 4);
                                            int delaySeconds = Math.Min(30, 5 * (1 << (_orientationScreenshotFailures - 1)));
                                            _nextOrientationScreenshotUtc = DateTime.UtcNow.AddSeconds(delaySeconds);
                                            logger.LogDebug($"Orientation screenshot failed; retry after [{delaySeconds}s], device[{id}]");
                                        }
                                        throw;
                                    }
                                }
                                if (hasSize && _viewActive && id == rxCurrentDevice.CurrentDeviceId)
                                {
                                    _currentDisplaySize = size;
                                    deviceManager.MobileRotation = size.Width > size.Height
                                        ? MobileRotation.Horizontal : MobileRotation.Vertical;
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.LogDebug($"读取手机当前方向失败，设备[{id}]：{ex.Message}");
                            }
                        }
                        if (!_viewActive) return;
                        if (deviceManager.MobileRotation != _mobileRotation)
                        {
                            logger.LogInfo($"手机显示方向变化[{_mobileRotation} -> {deviceManager.MobileRotation}]，设备[{rxCurrentDevice.CurrentDeviceId}]");
                            _mobileRotation = deviceManager.MobileRotation;
                            _bitmapImage = null;
                            if (_currentDisplaySize.Width > 0 && _currentDisplaySize.Height > 0)
                            {
                                _bitmapImageWidth = _currentDisplaySize.Width;
                                _bitmapImageHeight = _currentDisplaySize.Height;
                            }
                            await this.Dispatcher.InvokeAsync(() => ChangeRotation(_mobileRotation));
                            _eventSubject.OnNext(new GlobalEventSubModel(
                                _mobileRotation == MobileRotation.Horizontal
                                    ? EventType.HorizontalScreenOrientation : EventType.verticalScreenOrientation, ""));
                        }
                        #region 先临时这样处理，后续再细化
                        this.Dispatcher.Invoke(() =>
                        {
                            var position = grid.PointToScreen(new System.Windows.Point(0.0, 0.0));
                            MobileDeviceManager.Instance.mobileDeviceRect = new Rect
                            {
                                X = position.X / _dpiX,
                                Y = position.Y / _dpiX,
                                Width = grid.Width,
                                Height = grid.Height
                            };
                            MobileDeviceManager.Instance.mobileDeviceSoultion = new { width = Math.Round(_bitmapImageWidth, 2), height = Math.Round(_bitmapImageHeight, 2) };
                        });
                        #endregion

                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning($"同步列表屏幕方向异常：{ex}");
                }
                finally
                {
                    Interlocked.Exchange(ref _orientationUpdateRunning, 0);
                }
            });
        }

        private void DeviceScreenView_Loaded(object sender, RoutedEventArgs e)
        {
            _viewActive = true;
            SubscribeToDeviceEvents();
            startGetPoint();
        }

        private void DeviceScreenView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource != this) return;
            _viewActive = false;
            Interlocked.Increment(ref _connectionRequestVersion);
            _connectSubscription?.Dispose();
            _connectSubscription = null;
            _exceptionSubscription?.Dispose();
            _exceptionSubscription = null;
            _orientationSubscription?.Dispose();
            _orientationSubscription = null;
            ReleaseWindowsFormsHost();
            if (MobileDeviceManager.Instance.mobileForm == this)
            {
                MobileDeviceManager.Instance.IsConnected = false;
                deviceScreenViewModel.IsConnected = false;
                var device = rxCurrentDevice.DeviceInfos.FirstOrDefault(d => d.Udid == rxCurrentDevice.CurrentDeviceId);
                if (device != null) device.IsConnected = false;
                rxCurrentDevice.CurrentDeviceId = "";
            }
        }

        private void KillMobileScreenProcess()
        {
            _screenMirrorService.KillAllScreenProcesses();
        }
        private async Task HandleConnectionChangeAsync(ConnectEventSubModel change, long version)
        {
            await _connectionTransition.WaitAsync();
            try
            {
                if (version != Interlocked.Read(ref _connectionRequestVersion))
                {
                    if (change.ConnectType == ConnectType.Connect)
                    {
                        var skippedDevice = rxCurrentDevice.DeviceInfos.FirstOrDefault(d => d.Udid == change.DeviceId);
                        if (skippedDevice != null) skippedDevice.IsConnected = false;
                    }
                    return;
                }

                if (change.ConnectType == ConnectType.Connect)
                {
                    // 即使旧连接尚未写入 CurrentDeviceId，也必须先关闭旧投屏。
                    ReleaseWindowsFormsHost();
                    MobileDeviceManager.Instance.IsConnected = false;
                    deviceScreenViewModel.IsConnected = true;
                    deviceScreenViewModel.DeviceName = rxCurrentDevice.CurrentDeviceName;
                    await StartMobileScreen(change.DeviceId, version);
                }
                else if (change.ConnectType == ConnectType.DisConnect)
                {
                    MobileDeviceManager.Instance.IsConnected = false;
                    deviceScreenViewModel.IsConnected = false;
                    deviceScreenViewModel.ConnectStatus = 1;
                    deviceScreenViewModel.DeviceName = "";
                    rxCurrentDevice.CurrentDeviceId = "";
                    ReleaseWindowsFormsHost();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"切换手机连接异常：{ex}");
                ReleaseWindowsFormsHost();
            }
            finally
            {
                _connectionTransition.Release();
            }
        }

        private void EnsureCurrentConnection(long version)
        {
            if (version != 0 && version != Interlocked.Read(ref _connectionRequestVersion))
            {
                throw new OperationCanceledException("连接已被新的设备切换取代");
            }
        }

        public async Task StartMobileScreen(string deviceId, long version = 0)
        {
            string connectFailMsg = null;
            try
            {
                var hasBotProcess = ProcessHelper.GetAllProcessesByName("Z-Bot").Any();
                if (hasBotProcess)
                {
                    connectFailMsg = "请关闭机器人，并重启设计器后重试!".I18n("DeviceScreenView.xaml_FA0A85E2");
                    throw new Exception("请关闭机器人，并重启设计器后重试".I18n("controlType_DeviceScreenView.xaml_92A71CAD"));
                }
                MobileDeviceManager.Instance.IsConnecting = true;
                _eventSubject.OnNext(new GlobalEventSubModel(EventType.MoblieConnecting, ""));
                this.Dispatcher.Invoke(() =>
                {
                    this.hostVertical.Visibility = Visibility.Collapsed;
                    this.hostHorizontal.Visibility = Visibility.Collapsed;
                    this.connectLoading.Visibility = Visibility.Visible;
                });
                string platformVersion = this.rxCurrentDevice.DeviceInfos.First(a => a.Udid == deviceId).Version;
                await MobileDeviceManager.Instance.Connect(deviceId, platformVersion);
                EnsureCurrentConnection(version);

                try
                {
                    _bitmapImage = await MobileDeviceManager.Instance.GetScreenshot();
                }
                catch (Exception screenshotException)
                {
                    logger.LogWarning($"首次截图失败，将使用ADB屏幕尺寸继续启动投屏，不据此重建会话，设备[{deviceId}]：{screenshotException}");
                    _bitmapImage = null;
                }
                EnsureCurrentConnection(version);
                _ = MobileDeviceManager.Instance.ForceRefreshPageSourceAsync("Connect");
                try
                {
                    if (!deviceManager.TryGetRecentScreenshotDisplaySize(deviceId, out _currentDisplaySize))
                        _currentDisplaySize = await deviceManager.GetCurrentDisplaySizeAsync(deviceId);
                    deviceManager.MobileRotation = _currentDisplaySize.Width > _currentDisplaySize.Height
                        ? MobileRotation.Horizontal : MobileRotation.Vertical;
                }
                catch (Exception ex)
                {
                    logger.LogWarning($"连接时读取当前显示方向失败，设备[{deviceId}]：{ex.Message}");
                    if (_bitmapImage != null)
                    {
                        deviceManager.MobileRotation = _bitmapImage.Width > _bitmapImage.Height
                            ? MobileRotation.Horizontal : MobileRotation.Vertical;
                        _currentDisplaySize = new Rect(0, 0, _bitmapImage.Width, _bitmapImage.Height);
                    }
                    else
                    {
                        _currentDisplaySize = AdbHelper.GetDeviceSize(deviceId);
                        deviceManager.MobileRotation = _currentDisplaySize.Width > _currentDisplaySize.Height
                            ? MobileRotation.Horizontal : MobileRotation.Vertical;
                    }
                }
                EnsureCurrentConnection(version);
                _orientationScreenshotFailures = 0;
                _nextOrientationScreenshotUtc = DateTime.MinValue;
                _lastDisplayOrientationCheckUtc = DateTime.UtcNow;
                if (_bitmapImage != null
                    && (_bitmapImage.Width > _bitmapImage.Height) != (_currentDisplaySize.Width > _currentDisplaySize.Height))
                {
                    _bitmapImage = null;
                }
                if (_bitmapImage == null)
                {
                    _bitmapImageHeight = _currentDisplaySize.Height;
                    _bitmapImageWidth = _currentDisplaySize.Width;
                }
                _mobileRotation = deviceManager.MobileRotation;
                logger.LogInfo($"连接时手机显示方向[{_mobileRotation}]，显示尺寸[{_currentDisplaySize.Width}x{_currentDisplaySize.Height}]，设备[{deviceId}]");
                if (deviceManager.MobileRotation == MobileRotation.Horizontal)
                {
                    _eventSubject.OnNext(new GlobalEventSubModel(EventType.HorizontalScreenOrientation, ""));
                }
                await StartAndEmbedScreenAsync(deviceId, version);
                EnsureCurrentConnection(version);
                // rxCurrentDevice.CurrentDeviceName = device.Name;
                rxCurrentDevice.CurrentDeviceId = deviceId;
                MobileDeviceManager.Instance.IsConnected = true;
                _scrcpyInputMonitor.Start();
                //_eventSubject.OnNext(new GlobalEventSubModel(EventType.MoblieConnected, ""));
            }
            catch (Exception) when (version != 0 && version != Interlocked.Read(ref _connectionRequestVersion))
            {
                logger.LogInfo($"旧手机连接请求已被新请求取代，设备[{deviceId}]");
                var skippedDevice = rxCurrentDevice.DeviceInfos.FirstOrDefault(d => d.Udid == deviceId);
                if (skippedDevice != null) skippedDevice.IsConnected = false;
                deviceScreenViewModel.IsConnected = false;
                ReleaseWindowsFormsHost();
            }
            catch (Exception ex)
            {
                ReleaseWindowsFormsHost();
                logger.LogError($"连接手机操作异常：{ex}");
                MobileDeviceManager.Instance.IsConnected = false;
                deviceScreenViewModel.IsConnected = false;
                var device = this.rxCurrentDevice.DeviceInfos.FirstOrDefault(a => a.Udid == deviceId);
                if (device != null)
                {
                    device.IsConnected = false;
                }
                //deviceId = "";
                this.rxCurrentDevice.CurrentDeviceId = "";
                _eventSubject.OnNext(new GlobalEventSubModel(EventType.MoblieConnectFailed, ""));
                this.Dispatcher.Invoke(() =>
                {
                    MsgTipsHelper.ShowWarningPageInMainFrame(connectFailMsg ?? $"手机连接失败!", Application.Current.MainWindow);
                });
            }
            finally
            {
                MobileDeviceManager.Instance.IsConnecting = false;
                if (_viewActive && (version == 0 || version == Interlocked.Read(ref _connectionRequestVersion)))
                {
                    MobileDeviceManager.Instance.mobileForm = this;
                }
            }
        }
        private async Task StartAndEmbedScreenAsync(string deviceId, long version)
        {
            const int maxAttempts = 2;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                EnsureCurrentConnection(version);
                try
                {
                    // 宿主先挂载到目标容器，避免嵌入后重挂载导致原生父窗口变化。
                    await Dispatcher.InvokeAsync(() =>
                    {
                        EnsureCurrentConnection(version);
                        // Hidden保留宿主尺寸布局；原生窗口就绪前不遮挡WPF连接动画。
                        _windowsFormsHost = new WindowsFormsHost { Visibility = Visibility.Hidden };
                        _pnlSched = new System.Windows.Forms.Panel();
                        _windowsFormsHost.Child = _pnlSched;
                        gridhost = _mobileRotation == MobileRotation.Horizontal ? hostHorizontal : hostVertical;
                        grid = _mobileRotation == MobileRotation.Horizontal ? gridHorizontal : gridVertical;
                        image = _mobileRotation == MobileRotation.Horizontal ? imageHorizontal : imageVertical;
                        hostHorizontal.Visibility = _mobileRotation == MobileRotation.Horizontal ? Visibility.Visible : Visibility.Collapsed;
                        hostVertical.Visibility = _mobileRotation == MobileRotation.Vertical ? Visibility.Visible : Visibility.Collapsed;
                        connectLoading.Visibility = Visibility.Visible;
                        grid.Children.Add(_windowsFormsHost);
                        UpdateLayout();
                        // 启动投屏进程前准备宿主布局，首次窗口可用后只需校准并嵌入。
                        Render(_mobileRotation, true, resizeNativeWindow: false);
                    });
                    EnsureCurrentConnection(version);
                    _mobileHandle = await _screenMirrorService.StartAsync(deviceId, () => EnsureCurrentConnection(version));
                    EnsureCurrentConnection(version);
                    var embedStopwatch = Stopwatch.StartNew();
                    await Dispatcher.InvokeAsync(() =>
                    {
                        EnsureCurrentConnection(version);
                        logger.LogDebug($"投屏窗口就绪后开始布局和嵌入，调度等待[{embedStopwatch.ElapsedMilliseconds}ms]，设备[{deviceId}]");
                        // 先让WPF和WinForms宿主完成最终尺寸布局，再按真实客户区嵌入。
                        Render(_mobileRotation, true, resizeNativeWindow: false);
                        var clientSize = _pnlSched.ClientSize;
                        if (clientSize.Width <= 0 || clientSize.Height <= 0)
                            throw new InvalidOperationException("投屏宿主尚未获得有效客户区尺寸");
                        _screenMirrorService.Embed(_pnlSched.Handle, clientSize.Width, clientSize.Height);
                    });
                    logger.LogDebug($"投屏窗口就绪后布局和嵌入完成，耗时[{embedStopwatch.ElapsedMilliseconds}ms]，设备[{deviceId}]");
                    // 让出UI线程处理首次嵌入的布局和窗口消息，再校准一次尺寸。
                    await Dispatcher.InvokeAsync(() =>
                    {
                        EnsureCurrentConnection(version);
                        Render(_mobileRotation, true);
                        if (!_screenMirrorService.IsAlive())
                            throw new InvalidOperationException("投屏窗口在嵌入完成后已失效");
                        _windowsFormsHost.Visibility = Visibility.Visible;
                        connectLoading.Visibility = Visibility.Collapsed;
                    }, DispatcherPriority.ContextIdle);
                    logger.LogInfo($"手机投屏启动和嵌入完成，设备[{deviceId}]，尝试[{attempt}/{maxAttempts}]");
                    return;
                }
                catch (Exception ex)
                {
                    ReleaseWindowsFormsHost();
                    // 已切换设备的旧请求不进行恢复，也不发布成功状态。
                    EnsureCurrentConnection(version);
                    if (attempt == maxAttempts) throw;
                    logger.LogWarning($"手机投屏启动或嵌入失败，清理后重新启动一次投屏，设备[{deviceId}]：{ex}");
                }
            }
        }

        private void Render(MobileRotation mobileRotation, bool isConnectType = false, bool resizeNativeWindow = true)
        {
            // UpdateLayout可同步触发SizeChanged，避免在尺寸计算中重入。
            if (_updatingMirrorLayout) return;
            _updatingMirrorLayout = true;
            try
            {
                if (_windowsFormsHost != null)
                {
                    var targetGrid = mobileRotation == MobileRotation.Horizontal ? gridHorizontal : gridVertical;
                    if (!targetGrid.Children.Contains(_windowsFormsHost))
                    {
                        gridHorizontal.Children.Remove(_windowsFormsHost);
                        gridVertical.Children.Remove(_windowsFormsHost);
                        targetGrid.Children.Add(_windowsFormsHost);
                        UpdateLayout();
                    }
                    image.Source = _bitmapImage;
                }
                CalculateScale(isConnectType, resizeNativeWindow);
            }
            finally
            {
                _updatingMirrorLayout = false;
            }
        }
        private void CalculateScale(bool isConnectType = false, bool resizeNativeWindow = true)
        {
            if (_bitmapImage != null)
            {
                _bitmapImageWidth = _bitmapImage.Width;
                _bitmapImageHeight = _bitmapImage.Height;
                CalculateWinformHost(_bitmapImage.Width, _bitmapImage.Height, isConnectType, resizeNativeWindow);
            }
            else
            {
                CalculateWinformHost(_bitmapImageWidth, _bitmapImageHeight, isConnectType, resizeNativeWindow);
            }
        }
        private void CalculateWinformHost(double Width, double Height, bool isConnectType = false, bool resizeNativeWindow = true)
        {
            if (Height > Width)
            {
                _scale = gridhost.ActualHeight / Height;
            }
            else
            {
                int width = 840;
                if (gridhost.ActualWidth != 0.0)
                {
                    width = (int)gridhost.ActualWidth;
                }
                _scale = (double)width / Width;
            }
            grid.Width = Width * _scale;
            grid.Height = Height * _scale;
            double mainWindWidth = Application.Current.MainWindow.ActualWidth;
            if (mainWindWidth - 260 < grid.Width)
            {
                grid.Width = mainWindWidth - 260;

                grid.Height = grid.Width / Width * Height;
                _scale = (double)grid.Width / Width;
            }
            grid.Width = Math.Round(grid.Width, 0) - 1;
            grid.Height = Math.Round(grid.Height, 0) - 1;
            CalculateZoom();
            UpdateLayout();
            // 使用WinForms实际客户区，避免WPF逻辑尺寸、DPI取整和原生窗口尺寸不一致。
            var clientSize = _pnlSched?.ClientSize ?? System.Drawing.Size.Empty;
            SetPanelRectangle(clientSize.Width, clientSize.Height);
            bool renderResult = !resizeNativeWindow || (clientSize.Width > 0 && clientSize.Height > 0
                && _screenMirrorService.Resize(clientSize.Width, clientSize.Height));
            if (isConnectType)
                logger.LogDebug($"首次投屏布局校准，Grid[{grid.ActualWidth}x{grid.ActualHeight}]，DPI[{_dpiX}x{_dpiY}]，宿主客户区[{clientSize.Width}x{clientSize.Height}]，调整原生窗口[{resizeNativeWindow}]");
            UpdateMobileDeviceLayout();
            if (!renderResult)
            {
                if (isConnectType)
                {
                    throw new Exception("连接手机时手机屏幕窗口渲染(内嵌)失败!".I18n("controlType_DeviceScreenView.xaml_4C098954"));
                }
            }
        }
        private void UpdateMobileDeviceLayout()
        {
            if (grid == null)
            {
                return;
            }

            var position = grid.PointToScreen(new System.Windows.Point(0.0, 0.0));
            MobileDeviceManager.Instance.mobileDeviceRect = new Rect
            {
                X = position.X / _dpiX,
                Y = position.Y / _dpiY,
                Width = grid.Width,
                Height = grid.Height
            };
            MobileDeviceManager.Instance.mobileDeviceSoultion = new { width = Math.Round(_bitmapImageWidth, 2), height = Math.Round(_bitmapImageHeight, 2) };
        }
        private void CalculateZoom()
        {
            PresentationSource presentationSource = PresentationSource.FromVisual(this);
            if (presentationSource != null)
            {
                _dpiX = presentationSource.CompositionTarget.TransformToDevice.M11;
                _dpiY = presentationSource.CompositionTarget.TransformToDevice.M22;
                deviceManager.DpiX = _dpiX;
                deviceManager.DpiY = _dpiY;
            }
        }
        private void ChangeRotation(MobileRotation mobileRotation)
        {
            if (mobileRotation == MobileRotation.Vertical)
            {
                gridhost = hostVertical;
                grid = gridVertical;
                image = imageVertical;
                hostHorizontal.Visibility = Visibility.Collapsed;
                hostVertical.Visibility = Visibility.Visible;
            }
            else
            {
                gridhost = hostHorizontal;
                grid = gridHorizontal;
                image = imageHorizontal;
                hostHorizontal.Visibility = Visibility.Visible;
                hostVertical.Visibility = Visibility.Collapsed;
            }
            RotateImage(mobileRotation);
            //ConfirmEdge();

        }

        private void RotateImage(MobileRotation mobileRotation)
        {
            if (_bitmapImageWidth == 0.0 || _bitmapImageHeight == 0.0)
            {
                return;
            }
            if (mobileRotation == MobileRotation.Horizontal)
            {
                if (_bitmapImageWidth < _bitmapImageHeight)
                {
                    ExchangeWidthHeight();
                }
            }
            else if (_bitmapImageWidth > _bitmapImageHeight)
            {
                ExchangeWidthHeight();
            }
            Render(deviceManager.MobileRotation);
            //CalculateWinformHost(_bitmapImageWidth, _bitmapImageHeight);
        }

        private void ExchangeWidthHeight()
        {
            double middle = _bitmapImageWidth;
            _bitmapImageWidth = _bitmapImageHeight;
            _bitmapImageHeight = middle;
        }

        private void SetPanelRectangle(int width, int height)
        {
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, width, height);
            Region region = new Region(GetRoundedRectPath(rect, 1));
            if (_pnlSched != null)
            {
                _pnlSched.Region = region;
                _pnlSched.BackColor = System.Drawing.Color.FromArgb(45, 44, 67);
            }
            if (_windowsFormsHost != null)
            {
                _windowsFormsHost.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 44, 67));
            }
        }
        private GraphicsPath GetRoundedRectPath(System.Drawing.Rectangle rect, int radius)
        {
            System.Drawing.Rectangle arcRect = new System.Drawing.Rectangle(rect.Location, new System.Drawing.Size(radius, radius));
            GraphicsPath graphicsPath = new GraphicsPath();
            graphicsPath.AddArc(arcRect, 180f, 90f);
            arcRect.X = rect.Right - radius;
            graphicsPath.AddArc(arcRect, 270f, 90f);
            arcRect.Y = rect.Bottom - radius;
            graphicsPath.AddArc(arcRect, 0f, 90f);
            arcRect.X = rect.Left;
            graphicsPath.AddArc(arcRect, 90f, 90f);
            graphicsPath.CloseFigure();
            return graphicsPath;
        }
        private void DeviceSimulatorView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Render(deviceManager.MobileRotation);
            Win32Helper.GetWindowRect((HWND)_mobileHandle, out RECT _rect);
            int x = _rect.left;
            int y = _rect.top;
        }

        public static int GetCurrentProcessSessionId()
        {
            using (Process process = Process.GetCurrentProcess())
            {
                return process.SessionId;
            }
        }
        public void ReleaseWindowsFormsHost()
        {
            try
            {
                // 断连或切换设备时，先关闭拾取覆盖窗，通过现有取消回调结束拾取等待。
                this.Dispatcher.Invoke(() =>
                {
                    var captureOverlay = overlayWnd;
                    overlayWnd = null;
                    if (captureOverlay?.IsVisible == true)
                    {
                        logger.LogDebug("释放手机投屏资源，关闭当前拾取覆盖窗并结束拾取等待");
                        captureOverlay.Close();
                    }
                });
            }
            catch (Exception ex)
            {
                logger.LogError($"释放手机投屏资源时关闭拾取覆盖窗异常：{ex}");
            }

            try
            {
                _scrcpyInputMonitor.Stop();
                // 先停止投屏进程，再释放其父窗口，避免原生窗口短暂浮到桌面外层。
                _screenMirrorService.Stop();
                _mobileHandle = IntPtr.Zero;
                this.Dispatcher.Invoke(() =>
                {
                    hostHorizontal.Visibility = Visibility.Collapsed;
                    hostVertical.Visibility = Visibility.Collapsed;
                    connectLoading.Visibility = Visibility.Collapsed;
                    imageHorizontal.Source = null;
                    imageVertical.Source = null;
                    if (_windowsFormsHost != null)
                    {
                        gridHorizontal.Children.Remove(_windowsFormsHost);
                        gridVertical.Children.Remove(_windowsFormsHost);
                        _windowsFormsHost.Dispose();
                        _windowsFormsHost = null;
                    }
                    if (_pnlSched != null)
                    {
                        _pnlSched.Dispose();
                        _pnlSched = null;
                    }
                });
            }
            catch (Exception ex)
            {
                logger.LogError($"释放窗体资源异常：{ex}");
            }
        }


        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetCursorPos(out POINTt pt);
        [StructLayout(LayoutKind.Sequential)]
        public struct POINTt
        {
            public int X;
            public int Y;



            public POINTt(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }
        private int _edgeLeft;

        private int _edgeTop;
        public OverlayWnd overlayWnd;
        private System.Windows.Point TransformPoint(System.Windows.Point point)
        {
            System.Windows.Point point1 = new System.Windows.Point();
            //point1.X = Math.Round(point.X / (grid.Width * _dpiX) * _bitmapImageWidth, 0);
            //point1.Y = Math.Round(point.Y / (grid.Height * _dpiY) * _bitmapImageHeight, 0);
            point1.X = Math.Round(point.X / (grid.Width) * _bitmapImageWidth, 0);
            point1.Y = Math.Round(point.Y / (grid.Height) * _bitmapImageHeight, 0);
            return point1;
        }
        private async void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (deviceScreenViewModel.IsConnected)
            {
                try
                {
                    System.Windows.Point position = new System.Windows.Point(0, 0);
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        position = grid.PointToScreen(new System.Windows.Point(0.0, 0.0));
                        _edgeLeft = (int)(position.X / _dpiX);
                        _edgeTop = (int)(position.Y / _dpiX);

                    }));
                    GetCursorPos(out POINTt pt);
                    pt = new POINTt((int)(pt.X / _dpiX), (int)(pt.Y / _dpiY));
                    //if (pt.X > _edgeLeft && pt.X < _edgeLeft + grid.Width * _dpiX && pt.Y > _edgeTop && pt.Y < _edgeTop + grid.Height * _dpiY)
                    if (pt.X > _edgeLeft && pt.X < _edgeLeft + grid.Width && pt.Y > _edgeTop && pt.Y < _edgeTop + grid.Height)
                    {
                        position = new System.Windows.Point(pt.X - _edgeLeft, pt.Y - _edgeTop);
                        var realPoint = TransformPoint(position);
                        positionX.Text = realPoint.X.ToString();
                        positionY.Text = realPoint.Y.ToString();
                        deviceManager.CurrentPositionX = (int)realPoint.X;
                        deviceManager.CurrentPositionY = (int)realPoint.Y;
                    }
                }
                catch { }
            }
        }
        private DispatcherTimer HighLightDispatcherTimer = new DispatcherTimer();
        private void startGetPoint()
        {
            HighLightDispatcherTimer.Tick -= new EventHandler(DispatcherTimer_Tick);
            HighLightDispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            HighLightDispatcherTimer.Interval = TimeSpan.FromMilliseconds(10);
            HighLightDispatcherTimer.Start();

        }
        public void CaptureElementHandle(bool isCvCapture = false, string CvUrl = "", bool isSimilar = false, string originSelecter = "")
        {
            try
            {
                // positionM.Visibility = Visibility.Visible;
                overlayWnd = new OverlayWnd(this);
                MobileDeviceManager device = MobileDeviceManager.Instance;
                device.BeginCaptureMode();
                overlayWnd.Closed += (_, __) => device.EndCaptureMode();
                if (isCvCapture)
                {
                    overlayWnd.GetMobileElementHandle = (System.Windows.Point location) => { return device.GetElementFromPoint(new System.Drawing.Point((int)location.X, (int)location.Y)); };
                    overlayWnd.ClickScreen = async (System.Windows.Point location, bool isClick) => { return await device.ClickElementWithPointCV(new System.Drawing.Point((int)location.X, (int)location.Y), isClick); };
                }
                else
                {
                    overlayWnd.GetMobileElementHandle = (System.Windows.Point location) => { return device.GetElementFromPoint(new System.Drawing.Point((int)location.X, (int)location.Y)); };
                    overlayWnd.ClickScreen = async (System.Windows.Point location, bool isClick) => { return await device.ClickElementWithPoint(new System.Drawing.Point((int)location.X, (int)location.Y), isClick, isSimilar, originSelecter); };
                }
                overlayWnd.ShowInTaskbar = false;
                overlayWnd.Show();
                overlayWnd.startGetSelector();
            }
            catch (Exception ex)
            {
                MobileDeviceManager.Instance.EndCaptureMode();
                throw;
            }
        }

        public void CheckElementHandle(List<System.Drawing.Rectangle> rects)
        {
            try
            {
                List<System.Drawing.Rectangle> showRects = new List<System.Drawing.Rectangle>();
                for (var i = 0; i < 3; i++)
                {
                    foreach (var rectangle in rects)
                    {
                        #region 框住元素位置
                        System.Drawing.Rectangle rect = new System.Drawing.Rectangle();
                        System.Windows.Point position = new System.Windows.Point(0, 0);
                        position = grid.PointToScreen(new System.Windows.Point(0.0, 0.0));
                        int _edgeLeft = (int)(position.X / _dpiX);
                        int _edgeTop = (int)(position.Y / _dpiX);
                        rect.Width = (int)(grid.Width * _dpiX / _bitmapImageWidth * rectangle.Width);
                        rect.Height = (int)(grid.Height * _dpiX / _bitmapImageHeight * rectangle.Height);
                        rect.X = (int)(grid.Width * _dpiX / _bitmapImageWidth * rectangle.X) + _edgeLeft;
                        rect.Y = (int)(grid.Height * _dpiY / _bitmapImageHeight * rectangle.Y) + _edgeTop;
                        showRects.Add(rect);
                        #endregion
                    }
                    CheckWnd checkWnd = new CheckWnd();
                    checkWnd.MoveRects(showRects);
                    checkWnd.ShowInTaskbar = false;
                    checkWnd.Show();
                    Thread.Sleep(300);
                    checkWnd.Close();
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public void CapturePointHandle()
        {
            try
            {
                //positionM.Visibility = Visibility.Visible;
                overlayWnd = new OverlayWnd(this);
                MobileDeviceManager device = MobileDeviceManager.Instance;
                device.BeginCaptureMode();
                overlayWnd.Closed += (_, __) => device.EndCaptureMode();
                overlayWnd.GetMobileElementHandle = (System.Windows.Point location) => { return device.GetElementFromPoint(new System.Drawing.Point((int)location.X, (int)location.Y)); };
                overlayWnd.ShowInTaskbar = false;
                overlayWnd.Show();
                overlayWnd.startGetPoint();
            }
            catch (Exception ex)
            {
                MobileDeviceManager.Instance.EndCaptureMode();
                throw;
            }
        }

        private void Run_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _mainViewModel.AddDeviceCommand?.Execute(null);
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
                e.Handled = true;
            }
            catch
            {

            }
        }
    }
}

