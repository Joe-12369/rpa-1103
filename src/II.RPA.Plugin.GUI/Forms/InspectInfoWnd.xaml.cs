using Gma.UserActivityMonitor;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Params;
using II.RPA.Plugin.GUI.ProcessCreator.ElementTrack;
using II.RPA.Plugin.GUI.Utils;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.WinAutomation.CommonUtility.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
//using System.Windows.Media;
using System.Windows.Media.Imaging;
using static II.RPA.Plugin.GUI.ElementHighlight.HighlightUtils;

namespace II.RPA.Plugin.GUI
{
    /// <summary>
    /// InspectInfoWnd.xaml 的交互逻辑
    /// </summary>
    public partial class InspectInfoWnd : Window
    {
        #region 变量
        DateTime _lastKeyDownTime;
        List<Keys> _hookKeys;
        private bool isPause = false;
        public bool IsPause
        {
            get { return isPause; }
            set { isPause = value; }
        }
        public CancellationTokenSource CancellationForCheckCursorLoop
        {
            get;
            private set;
        } = new CancellationTokenSource();

        internal bool IsClosed { get; set; }
        #endregion
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int GWL_EXSTYLE = (-20);
        private static ILogger logger = Logger.OfContext();
        private static IntPtr curHanble = IntPtr.Zero;
        private volatile bool isEnableCheckCursor = false;
        /// <summary>
        /// 是否已向 <see cref="HookManager"/> 注册键盘钩子；仅用于避免重复 +=，卸载时一律 -=。
        /// </summary>
        private bool _globalInputHooksSubscribed;

        [DllImport("user32", EntryPoint = "SetWindowLong")]
        private static extern uint SetWindowLong(IntPtr hwnd, int nIndex, uint dwNewLong);

        private string SceneName { get; set; } = null;
        private readonly ConcurrencyItemContainer<System.Drawing.Point?> m_NextPoint = new ConcurrencyItemContainer<System.Drawing.Point?>();
        private readonly object _moveFromLocationSync = new object();
        private bool _moveFromLocationPending;
        private System.Drawing.Point _latestMoveFromLocationPoint;
        public InspectInfoWnd()
        {
            InitializeComponent();
            WinAutomation.CommonUtility.XamlResource.LanguageResourceHelper.GetInstance().SetAppInstance(this);
            this.SourceInitialized += delegate
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                uint extendedStyle = NativeMethods.GetWindowLong(hwnd, GWL_EXSTYLE);
                NativeMethods.SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle |
                WS_EX_TRANSPARENT);
            };
            SceneName = null;
            this.WindowStartupLocation = WindowStartupLocation.Manual;

            var screenPoint = System.Windows.Forms.Cursor.Position;
            tb_Position.Text = $"({screenPoint.X.ToString()}, {screenPoint.Y.ToString()})";
            _lastKeyDownTime = DateTime.Now;
            _hookKeys = new List<Keys>() { Keys.Escape, Keys.F2, Keys.F3 };
        }

        /// <summary>
        /// 初始化窗体状态 添加事件钩子
        /// </summary>
        internal void InitCapture()
        {
            _lastKeyDownTime = DateTime.Now;
            isEnableCheckCursor = true;
            AttachGlobalInputHooks();
        }

        internal void UninitCapture()
        {
            StopCheckCursorLoop();
            DetachGlobalInputHooks();
        }

        /// <summary>
        /// 调整信息框数据
        /// </summary>
        /// <param name="sceneName"></param>
        public void InitCaptureByRegion(string sceneName)
        {
            ChangeCaptureStatusStyle(sceneName);
            _lastKeyDownTime = DateTime.Now;
            isEnableCheckCursor = true;
            AttachGlobalInputHooks();
        }


        public void ChangeCaptureStatusStyle(string sceneName = "")
        {
            try
            {

                if (!txt_Status.CheckAccess())
                {
                    txt_Status.Dispatcher.InvokeAsync(delegate ()
                    {
                        ChangeCaptureStatusStyle(sceneName);
                    });
                    return;
                }

                if (sceneName == "区域拾取")
                {
                    tb_WindowType.Text = "--";
                    txt_Status.Text = "拾取区域";
                    tb_singleClick.Text = "区域";
                    SetPanelVisibility(sp_shiftClick);
                    SetPanelVisibility(rect_refreshLine);
                    SetPanelVisibility(sp_refreshPanle);
                    SetPanelVisibility(rect_EscTopLine);
                }
                else
                {
                    SetPanelVisibility(sp_shiftClick, true);
                    SetPanelVisibility(rect_refreshLine, true);
                    SetPanelVisibility(sp_refreshPanle, true);
                    SetPanelVisibility(rect_EscTopLine, true);
                }
            }
            catch (Exception ex)
            {
                //trace
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "改变拾取状态 窗口效果时发生异常，一般情况下忽略此问题。");
            }
        }
        private void AttachGlobalInputHooks()
        {
            if (_globalInputHooksSubscribed)
            {
                return;
            }
            HookManager.KeyUp += HookManager_KeyUp1;
            HookManager.KeyDown += HookManager_KeyDown;
            _globalInputHooksSubscribed = true;
            logger.LogDebug("InspectInfoWnd: global keyboard hooks attached");
        }

        private void HookManager_MouseMoveExt(object sender, MouseEventExtArgs e)
        {
            m_NextPoint?.SetData(e.Location);
        }

        private void HookManager_KeyUp1(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            //logger.LogInformation($"HookManager_KeyUp {e.KeyCode.ToString()}");
            if (e.KeyCode == Keys.LControlKey)
            {
                //临时注释，目前选择元素后此处会再次将窗体设置为穿透，后面排查作用及改动
                //GuiElement.overlayForm.CurrentElementHighlighterManager.SetWndPenetrate(true);
                GuiElement.overlayForm.CurrentElementHighlighterManager.SetBackgroundActive(false);
                OverlayFormCaptureTask.Instance.Resume();
                CVCaptureTask.Instance.Resume();
                GuiElement.overlayForm.CurrentElementHighlighterManager.SetTopmost();
            }
        }

        private void DetachGlobalInputHooks()
        {
            HookManager.KeyDown -= HookManager_KeyDown;
            HookManager.KeyUp -= HookManager_KeyUp1;
            HookManager.MouseMoveExt -= HookManager_MouseMoveExt;
            _globalInputHooksSubscribed = false;
            logger.LogDebug("InspectInfoWnd: global input hooks detached");
        }

        private void HookManager_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            try
            {
                var span = (DateTime.Now - _lastKeyDownTime).TotalMilliseconds;
                _lastKeyDownTime = DateTime.Now;
                if (span < 200)
                {
                    e.Handled = _hookKeys.Contains(e.KeyCode);
                    return;
                }
                logger.LogDebug($"HookManager_KeyDown {e.KeyCode}");
                switch (e.KeyCode)
                {
                    case Keys.Escape:
                        e.Handled = true;
                        GuiElement.overlayForm.CaptureOperationData.trigger_condition = "Esc";
                        GuiElement.overlayForm.KeyCancel();
                        //OverlayForm.client.EndRemoteCapture();
                        //OverlayForm.CurrentRemoteCaptureClient.EndRemoteCapture();
                        isEnableCheckCursor = false;
                        CancellationForCheckCursorLoop.Cancel();
                        break;
                    case Keys.F2:
                        GuiElement.overlayForm.CaptureOperationData.f2_key_number++;
                        if (GuiElement.overlayForm.isCVParsing)
                        {
                            return;
                        }
                        GuiElement.overlayForm.IsCVCapture = false;
                        GuiElement.overlayForm.CurrentCaptureMode = (CaptureModeType)(((int)GuiElement.overlayForm.CurrentCaptureMode + 1) % 2);
                        ChangeCaptureStatusStyle();
                        e.Handled = true;
                        break;
                    case Keys.F3:
                        if (GuiElement.overlayForm.isCVParsing)
                        {
                            return;
                        }
                        GuiElement.overlayForm.IsCVCapture = true;
                        GuiElement.overlayForm.CurrentCaptureMode = CaptureModeType.CV;
                        //SetPanelVisibility(rect_loadingLine);
                        //SetPanelVisibility(sp_loading);
                        ChangeCaptureStatusStyle();
                        e.Handled = true;
                        break;
                    case Keys.F4:
                        GuiElement.overlayForm.CaptureOperationData.f4_key_number++;
                        if (GuiElement.overlayForm.isCVParsing)
                        {
                            return;
                        }
                        GuiElement.overlayForm.IsCVCapture = false;
                        CVCaptureTask.Instance.IsRefrush = true;
                        e.Handled = true;
                        break;
                    case Keys.F7:
                        GuiManager.Info.ToggleJavaCaptureMode();
                        e.Handled = true;
                        break;
                    case Keys.LControlKey:
                        if (!(GuiElement.overlayForm.CurrentHighlightElement is IGuiCVNode590))
                        {
                            logger.LogDebug($"HookManager_KeyDown {e.KeyCode}: SetWindowTransparent (penetrate off)");
                            OverlayFormCaptureTask.Instance.Pause();
                            CVCaptureTask.Instance.Pause();
                            GuiElement.overlayForm.CurrentElementHighlighterManager.SetWndPenetrate(false);
                            GuiElement.overlayForm.CurrentElementHighlighterManager.SetTopmost();
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "按键事件处理发生了一个异常：HookManager_KeyDown");
            }
        }

        private void SetPanelVisibility(FrameworkElement panel, bool isVisible = false)
        {
            try
            {
                if (!panel.CheckAccess())
                {
                    this.Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        SetPanelVisibility(panel, isVisible);
                    });
                    return;
                }
                if (!isVisible)
                {
                    if (panel.Visibility == Visibility.Visible)
                    {
                        var height = panel.Height;
                        this.Height -= height;
                    }
                    panel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    if (panel.Visibility == Visibility.Collapsed)
                    {
                        var height = panel.Height;
                        this.Height += height;
                    }
                    panel.Visibility = Visibility.Visible;
                }
            }
            catch (ObjectDisposedException ex)
            {
            }
            catch (Exception ex)
            {
            }
        }
        internal void SetWndHide()
        {
            logger.LogDebug("=================Start 设置状态窗口隐藏");
            try
            {
                if (!this.CheckAccess())
                {
                    this.Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        logger.LogDebug($"被占用 暂时无法隐藏");
                        SetWndHide();
                    });
                    return;
                }
                this.Hide();
                logger.LogDebug($"成功隐藏");
            }
            catch (Exception)
            {
                logger.LogDebug("设置状态窗口隐藏异常，忽略");
            }

            logger.LogDebug("=================End 设置状态窗口隐藏");
        }

        #region 设置窗体大小
        private double WndWidth = -1;
        private double WndHeight = -1;

        internal void SetWndMin()
        {
            logger.LogDebug("=================Start 设置状态窗口最小化");
            try
            {
                if (!this.CheckAccess())
                {
                    this.Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        logger.LogDebug($"被占用 暂时无法最小化");
                        SetWndMin();
                    });
                    return;
                }
                WndWidth = this.Width;
                WndHeight = this.Height;
                this.Width = 0;
                this.Height = 0;
                logger.LogDebug($"成功隐藏");
            }
            catch (Exception)
            {
                logger.LogDebug("设置状态窗口最小化异常，忽略");
            }

            logger.LogDebug("=================End 设置状态窗口最小化");
        }

        internal void SetWndNormal()
        {
            logger.LogDebug("=================Start 还原状态窗口");
            try
            {
                if (!this.CheckAccess())
                {
                    this.Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        logger.LogDebug($"被占用 暂时无法还原");
                        SetWndNormal();
                    });
                    return;
                }

                if (WndWidth == -1 || WndHeight == -1)
                {
                    return;
                }

                this.Width = WndWidth;
                this.Height = WndHeight;
                logger.LogDebug($"成功还原");
            }
            catch (Exception)
            {
                logger.LogDebug("还原状态窗口异常，忽略");
            }

            logger.LogDebug("=================End 还原状态窗口");
        }

        internal void SetWndClose()
        {
            try
            {
                if (!this.CheckAccess())
                {
                    this.Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        logger.LogDebug($"被占用 暂时无法关闭");
                        SetWndClose();
                    });
                    return;
                }
                UninitCapture();
                this.Hide();
                logger.LogDebug($"成功关闭");
            }
            catch (Exception)
            {
                logger.LogDebug("关闭状态窗口异常，忽略");
            }
        }
        #endregion

        private ManualResetEvent ResetEvent = new ManualResetEvent(false);

        private bool isOnCVScreenshot = false;
        /// <summary>
        /// true的话说明正在进行cv截图 用来分析图片是否改变进行解析上面的cv元素 ，所以此时不能显示拾取状态框; false的话可显示拾取状态框
        /// </summary>
        public bool IsOnCVScreenshot
        {
            get { return isOnCVScreenshot; }
            set
            {
                isOnCVScreenshot = value;
                if (!isOnCVScreenshot)
                {
                    ResetEvent.Set();
                }
            }
        }


        internal void SetWndShow(bool isInitial = false)
        {
            logger.LogDebug("=================Start 设置状态窗口显示");
            try
            {
                if (!isInitial)
                {
                    if (GuiElement.overlayForm.CancellationForCapture.IsCancellationRequested)
                    {
                        return;
                    }
                }

                if (!this.CheckAccess())
                {
                    this.Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        logger.LogDebug($"被占用 暂时无法显示");
                        SetWndShow();
                    });
                    return;
                }

                if (GuiElement.overlayForm.CancellationForCapture.IsCancellationRequested)
                {
                    return;
                }
         
                if (IsOnCVScreenshot)
                {
                    Task.Factory.StartNew(() =>
                    {
                        logger.LogDebug($"IsOnCVScreenshot为true 阻塞等待其为false 来显示黑框");
                        ResetEvent.WaitOne(1000);
                        logger.LogDebug($"阻塞结束");
                        this.Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                        {
                            this.Show();
                            logger.LogDebug($"阻塞后 显示黑框");
                        });
                    });
                }
                else
                {
                    this.Show();
                    logger.LogDebug($"显示黑框");
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug("设置状态窗口显示异常，忽略");
            }

            logger.LogDebug("=================End 设置状态窗口显示");
        }

        public void SetCVCaptureMsg(string msg = "")
        {
            try
            {
                if (!this.CheckAccess())
                {
                    this.Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        SetCVCaptureMsg(msg);
                    });
                    return;
                }

                if (string.IsNullOrEmpty(msg))
                {
                    SetPanelVisibility(rect_loadingLine);
                    SetPanelVisibility(sp_loading);
                    //SetCursorBusy(false);
                }
                else
                {
                    SetPanelVisibility(rect_loadingLine, true);
                    SetPanelVisibility(sp_loading, true);
                    //SetCursorBusy(true);
                }
                //l_captureMsg.Text = msg;
            }
            catch (ObjectDisposedException ex)
            {

            }
            //l_captureMsg.Text = msg;
        }
        private void SetCursorBusy(bool isBusy)
        {
            try
            {
                if (isBusy)
                {
                    if (InspectInfoWnd.curHanble == IntPtr.Zero)
                    {
                        var cursorFilePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(this.GetType().Assembly.Location), "aero_working.ani");
                        curHanble = NativeMethods.LoadCursorFromFile(cursorFilePath);
                    }
                    NativeMethods.SetSystemCursor(NativeMethods.CopyIcon(curHanble), NativeMethods.OCR_NORMAL);
                    NativeMethods.SetSystemCursor(NativeMethods.CopyIcon(curHanble), NativeMethods.OCR_IBEAM);
                }
                else
                {
                    NativeMethods.SystemParametersInfo(0x0057, 0, null, 0);
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"{(isBusy ? "设置" : "取消")}鼠标后台运行效果异常");
            }
        }

        public void HideSwitchPanle()
        {
            try
            {
                if (!this.CheckAccess())
                {
                    this.Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        HideSwitchPanle();
                    });
                    return;
                }
                SetPanelVisibility(rect_refreshLine);
                SetPanelVisibility(sp_refreshPanle);
                //SetPanelVisibility(sp_switchPanle);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "隐藏拾取模式提示区域异常");
            }
        }

        /// <summary>
        /// 鼠标移动时 更新提示框显示的坐标信息 同时调整信息框出现位置
        /// </summary>
        /// <param name="point"></param>
        public void MoveFromLocation(System.Drawing.Point point)
        {
            try
            {
                if (!this.CheckAccess())
                {
                    bool needPost = false;
                    lock (_moveFromLocationSync)
                    {
                        _latestMoveFromLocationPoint = point;
                        if (!_moveFromLocationPending)
                        {
                            _moveFromLocationPending = true;
                            needPost = true;
                        }
                    }

                    if (needPost)
                    {
                        this.Dispatcher.BeginInvoke((Action)(() =>
                        {
                            System.Drawing.Point latestPoint;
                            lock (_moveFromLocationSync)
                            {
                                latestPoint = _latestMoveFromLocationPoint;
                                _moveFromLocationPending = false;
                            }
                            MoveFromLocation(latestPoint);
                        }));
                    }
                    return;
                }
                scale = (float)NativeMethods.DpiX / 96;
                m_WindowWidth = (int)(this.ActualWidth * scale) + 1;
                m_WindowHeight = (int)(this.ActualHeight * scale) + 1;
                //Size OutTaskBarSize = new Size(SystemInformation.WorkingArea.Width, SystemInformation.WorkingArea.Height);
                //Size ScreenSize = new Size(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height);

                //Size ScreenSize = new Size(SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height);
                //Size TaskBarSize;
                //TaskBarSize = new Size(
                //               ScreenSize.Width - (ScreenSize.Width - OutTaskBarSize.Width),
                //                ScreenSize.Height - OutTaskBarSize.Height
                //                );

                //var ScreenArea = SystemInformation.VirtualScreen;
                tb_Position.Text = $"({point.X.ToString()}, {point.Y.ToString()})";
                var ScreenArea = Screen.FromPoint(point).WorkingArea;
                var lastHighlightRect = GuiElement.overlayForm.CurrentElementHighlighterManager.CurrentBorderRectangle;
                var left = 4d;
                var top = 4d;
                var leftTop_left = 4d;
                var leftTop_top = 4d;
                var rightBottom_left = (ScreenArea.Width - m_WindowWidth) / scale;
                var rightBottom_top = (ScreenArea.Height - m_WindowHeight) / scale;
                if (string.IsNullOrEmpty(this.SceneName))
                {
                    bool isOverlap = lastHighlightRect.IsOverlap(new System.Drawing.Rectangle((int)Left, (int)this.Top, m_WindowWidth, m_WindowHeight));
                    if (isOverlap)
                    {
                        if (this.Left == 4 || this.Top == 4)
                        {
                            //窗口在右下角显示
                            top = rightBottom_top;
                            left = rightBottom_left;
                        }
                        else
                        {
                            top = leftTop_left;
                            left = leftTop_top;
                        }
                        //新位置和高亮重叠
                        if (!lastHighlightRect.IsOverlap(new System.Drawing.Rectangle((int)left, (int)top, m_WindowWidth, m_WindowHeight)))
                        {
                            this.Top = top;
                            this.Left = left;
                        }
                        else if (point.X < m_WindowWidth + 9 && point.Y < m_WindowHeight + 9)
                        {
                            this.Top = rightBottom_top;
                            this.Left = rightBottom_left;
                        }
                        else if (this.Left != 4 || this.Top != 4)// if (point.X > ScreenArea.Width - m_WindowWidth - 5 && point.Y > ScreenArea.Height - m_WindowHeight - 5)
                        {
                            this.Top = leftTop_top;
                            this.Left = leftTop_left;
                        }
                        return;
                    }
                }
                if (point.X < m_WindowWidth + 9 && point.Y < m_WindowHeight + 9)
                {
                    //窗口在右下角显示
                    top = rightBottom_top;
                    left = rightBottom_left;
                }
                else if (this.Left != 4 || this.Top != 4)// if (point.X > ScreenArea.Width - m_WindowWidth - 5 && point.Y > ScreenArea.Height - m_WindowHeight - 5)
                {
                    top = leftTop_top;
                    left = leftTop_left;
                }
                if (!lastHighlightRect.IsOverlap(new System.Drawing.Rectangle((int)left, (int)top, m_WindowWidth, m_WindowHeight)))
                {
                    this.Top = top;
                    this.Left = left;
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
        }

        internal System.Drawing.Rectangle GetWndRectangle()
        {
            if (!this.CheckAccess())
            {
                return this.Dispatcher.Invoke(() =>
                {
                    return GetWndRectangle();
                });
            }
            //var point = this.PointToScreen(new Point(this.Left, this.Top));
            //System.Drawing.Rectangle statusWndRect = new System.Drawing.Rectangle((int)point.X, (int)point.Y, (int)this.Width, (int)this.Height);
            System.Drawing.Rectangle statusWndRect = new System.Drawing.Rectangle((int)this.Left - 1, (int)this.Top - 1, (int)this.Width + 2, (int)this.Height + 2);
            return statusWndRect;
        }

        public void WindowType(string value)
        {
            try
            {
                tb_WindowType.Text = value.Equals("Swing") ? "Java" : value;
                if (tb_WindowType.Text.Equals("Java"))
                {
                    java_alter.Visibility = Visibility.Visible;
                    java_alter_text.Text=value.Equals("Swing")?"使用插件模式":"使用无插件模式";
                }
                else
                {
                    java_alter.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                //tb_WindowType.Text = "Window";
                logger.LogError(ex);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            IsClosed = true;
            UninitCapture();
            //SetCursorBusy(false);
        }



        int m_WindowWidth, m_WindowHeight;
        double scale;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            logger.LogDebug("Start Window_Loaded");
            scale = (float)NativeMethods.DpiX / 96;
            m_WindowWidth = (int)(this.ActualWidth * scale) + 1;
            m_WindowHeight = (int)(this.ActualHeight * scale) + 1;
            //测试发现此处 loading动画对Ui线程影响很大 暂时隐藏 后面可以调整显示效果
            //if (txt_Status.Text != "拾取区域")
            //{
            //    Assembly Asmb = Assembly.GetExecutingAssembly();
            //    Stream ManifestStream = Asmb.GetManifestResourceStream("II.RPA.Plugin.GUI.Forms.loading.gif");
            //    {
            //        var image = new BitmapImage();
            //        image.BeginInit();
            //        image.StreamSource = ManifestStream;
            //        image.EndInit();
            //        ImageBehavior.SetAnimatedSource(loadingImg, image);
            //    }
            //}
            logger.LogDebug("End Window_Loaded");
        }

        #region StartCheckCursorLoop 检测并设置ctrl时消除悬浮效果

        public bool IsPenetrate { get; set; } = true;

        public void StartCheckCursorLoop()
        {
            HookManager.MouseMoveExt -= HookManager_MouseMoveExt;
            HookManager.MouseMoveExt += HookManager_MouseMoveExt;

            // 避免 StartHighlight 等路径多次调用时叠出多个 while 循环（共用旧 CTS 时旧任务未结束）
            try
            {
                CancellationForCheckCursorLoop?.Cancel();
            }
            catch
            {
                // ignore
            }

            CancellationForCheckCursorLoop = new CancellationTokenSource();

            Task.Factory.StartNew(async () =>
            {
                try
                {
                    logger.LogDebug("Enter CheckCursorLoop");

                    while (true)
                    {
                        await Task.Delay(500);

                        if (!OverlayFormCaptureTask.Instance.IsStart)//普通元素拾取未开启
                        {
                            continue;
                        }

                        CancellationForCheckCursorLoop.Token.ThrowIfCancellationRequested();

                        if (isEnableCheckCursor && !(GuiElement.overlayForm.CurrentHighlightElement is IGuiCVNode590))
                        {
                            if (!OverlayFormCaptureTask.Instance.IsTaskEnable && m_NextPoint.TakeData().HasValue)
                            {
                                CancellationForCheckCursorLoop.Token.ThrowIfCancellationRequested();
                                logger.LogDebug("StartCheckCursorLoop Need Resume");
                                GuiElement.overlayForm.CurrentElementHighlighterManager.SetWndPenetrate(true);
                                IsPenetrate = true;
                                OverlayFormCaptureTask.Instance.Resume();
                                CVCaptureTask.Instance.Resume();
                                GuiElement.overlayForm.CurrentElementHighlighterManager.SetTopmost();
                            }
                            else
                            {
                                CancellationForCheckCursorLoop.Token.ThrowIfCancellationRequested();
                                if (m_NextPoint.WaitForData(300))
                                {
                                    continue;
                                }
                                else
                                {
                                    if (Control.ModifierKeys == Keys.Control && OverlayFormCaptureTask.Instance.IsStart)
                                    {
                                        OverlayFormCaptureTask.Instance.Pause();
                                        CVCaptureTask.Instance.Pause();
                                        //await Task.Delay(50);
                                        if (IsPenetrate)
                                        {
                                            GuiElement.overlayForm.CurrentElementHighlighterManager.SetWndPenetrate(false);
                                            GuiElement.overlayForm.CurrentElementHighlighterManager.SetTopmost();
                                            GuiElement.overlayForm.CurrentElementHighlighterManager.Hide();
                                            GuiElement.overlayForm.CurrentElementHighlighterManager.UndoHide();
                                            IsPenetrate = false;
                                        }

                                    }
                                }
                            }
                        }
                        else
                        {
                            CancellationForCheckCursorLoop.Token.ThrowIfCancellationRequested();
                            await Task.Delay(200);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    logger.LogDebug("Cancel CheckCursorLoop");
                }
            }, CancellationForCheckCursorLoop.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default).ConfigureAwait(false).GetAwaiter().OnCompleted(() => { });
        }

        public void StopCheckCursorLoop()
        {
            logger.LogDebug("StopCheckCursorLoop");
            isEnableCheckCursor = false;
            CancellationForCheckCursorLoop.Cancel();
        }

        #endregion

        #region WPF窗体缩放计算
        public Dpi GetWPFDpi()
        {
            return TransformToPixels(this);
        }

        public Dpi TransformToPixels(Visual aVisual)
        {
            Matrix matrix;
            ///原写法 win11下会存在拿不到实时dpi的情况（窗口没初始化...,此处窗口处理逻辑复杂），先这样改
            using (var src = new HwndSource(new HwndSourceParameters()))
            {
                matrix = src.CompositionTarget.TransformToDevice;
            }
            double pixelX = (int)(matrix.M11 * 96.0);
            double pixelY = (int)(matrix.M22 * 96.0);
            return new Dpi(pixelX, pixelY);
        }
        #endregion
    }
}


