using Gma.UserActivityMonitor;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using Microsoft.Extensions.Logging;
using System;
using Drawing = System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using II.RPA.Plugin.GUI.Commons;
using System.Threading;
using II.RPA.Plugin.Core.Commons;

namespace II.RPA.Plugin.GUI
{
    public enum WindowType
    {
        DeskTop,
        ForegroundWindow
    }
    /// <summary>
    /// CaptureScreenWnd.xaml 的交互逻辑
    /// </summary>
    public partial class CursorPositionWnd : Window
    {
        private static ILogger logger = Logger.OfContext();
        private Drawing.Rectangle _windowRect = new Drawing.Rectangle();
        public Drawing.Point CursorResult { get; private set; }
        public Drawing.Point positionBase;
        private MousePosition mousePosition = null;
        private DispatcherTimer DrawCrossDispatcherTimer = new DispatcherTimer();
        private CancellationTokenSource cts = new CancellationTokenSource();
        private IntPtr cacheHandle = IntPtr.Zero;
        private double _scale = 1.0;
        // 0 桌面 1 激活窗口
        private WindowType _windowType = 0;

        public CursorPositionWnd()
        {
            InitializeComponent();
            this.SourceInitialized += CursorPositionWnd_SourceInitialized;
        }
        private void ResetPositionAndResize()
        {

            #region [WPF 高DPI显示器内 需要做DPI换算]
            this.Dispatcher.Invoke(new Action(() =>
            {
                try
                {
                    //this.Activate();
                    this.Topmost = true;
                    _scale = DpiConverter.GetScalingFactor();
                    _windowRect = DpiConverter.ConvertRectangle(_windowRect);
                    this.Left = _windowRect.X;
                    this.Top = _windowRect.Y;
                    this.Width = _windowRect.Width;
                    this.Height = _windowRect.Height;
                }
                catch (Exception ex)
                {
                    logger.LogError($"RetryPositionAndResize:{ex}");
                }

            }));
            #endregion
        }
        public void CursorPositionWnd_Loaded(object sender, EventArgs e)
        {

            this.Topmost = true;

            InstallHook();

            mousePosition = new MousePosition();
            mousePosition.SetWndShow();
            mousePosition.Left = _windowRect.X + 5;
            mousePosition.Top = _windowRect.Y + 5;
            mousePosition.tb_PositionX.Text = (_windowRect.X).ToString();
            mousePosition.tb_PositionY.Text = (_windowRect.Y).ToString();

            // RetryPositionAndResize();
        }

        internal void InstallHook()
        {
            UninstallHook();

            HookManager.MouseClickExt += CursorPositionWnd_MouseClickExt;
            HookManager.KeyDown += CursorPositionWnd_KeyDown;//HookManager_KeyUp; 
            HookManager.MouseUp += CaptureWindow_MouseUp;
        }
        internal void UninstallHook()
        {
            HookManager.MouseClickExt -= CursorPositionWnd_MouseClickExt;
            HookManager.KeyDown -= CursorPositionWnd_KeyDown;
            HookManager.MouseUp -= CaptureWindow_MouseUp;
        }
        public void CursorPositionWnd_MouseClickExt(object sender, Gma.UserActivityMonitor.MouseEventExtArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
                {
                    //this.DialogResult = true;
                    StopHighlight();
                }
            }
            else if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                StopHighlight();
                if (!this.CheckAccess())
                {
                    this.Dispatcher.Invoke(() => { this.Close(); });
                }
                else
                {
                    this.Close();
                }
            }
        }
        public void CursorPositionWnd_SourceInitialized(object sender, EventArgs e)
        {
            //this.Width = SystemParameters.PrimaryScreenWidth;
            //this.Height = SystemParameters.PrimaryScreenHeight;
            SetPenetrate(true);

            IntPtr wptr = new WindowInteropHelper(this).Handle;
            HwndSource hs = HwndSource.FromHwnd(wptr);
            hs.AddHook(new HwndSourceHook(WndProc));
        }
        protected IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            //if (msg == NativeMethods.WM_NCHITTEST)
            //{
            //    return (IntPtr)NativeMethods.HTTRANSPARENT;
            //}
            return (IntPtr.Zero);
            //return (IntPtr)NativeMethods.HTTRANSPARENT;
        }
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                var point = System.Windows.Forms.Cursor.Position;
                var wndPoint = this.PointFromScreen(new Point(point.X, point.Y));
                #region [确定浮窗位置]
                var bound = _windowRect;
                var width = bound.Width;
                var height = bound.Height;
                if (wndPoint.X > this._windowRect.Width || wndPoint.X < 0 || wndPoint.Y < 0 || wndPoint.Y > this._windowRect.Height)
                {
                    mousePosition.Left = _windowRect.X;
                    mousePosition.Top = _windowRect.Y;
                    DrawCross(new Point(0, 0));
                    mousePosition.tb_PositionX.Text = "0";
                    mousePosition.tb_PositionY.Text = "0";
                    return;
                }
                else
                {
                    xCrossLine.Visibility = Visibility.Visible;
                    yCrossLine.Visibility = Visibility.Visible;
                    DrawCross(wndPoint);
                }
                if (wndPoint.X + mousePosition.Width > width && wndPoint.Y + mousePosition.Height <= height)
                {
                    mousePosition.Left = (int)wndPoint.X + bound.X - mousePosition.Width - 5;
                    mousePosition.Top = (int)wndPoint.Y + bound.Y + 5;
                }
                else if (wndPoint.X + mousePosition.Width <= width && wndPoint.Y + mousePosition.Height > height)
                {
                    mousePosition.Left = (int)wndPoint.X + bound.X + 5;
                    mousePosition.Top = (int)wndPoint.Y + bound.Y - mousePosition.Height - 5;
                }
                else if (wndPoint.X + mousePosition.Width > width && wndPoint.Y + mousePosition.Height > height)
                {
                    mousePosition.Left = (int)wndPoint.X + bound.X - mousePosition.Width - 5;
                    mousePosition.Top = (int)wndPoint.Y + bound.Y - mousePosition.Height - 5;
                }
                else
                {
                    mousePosition.Left = (int)wndPoint.X + bound.X + 5;
                    mousePosition.Top = (int)wndPoint.Y + bound.Y + 5;
                }

                #endregion
                var x = point.X - (int)(_windowRect.Left * _scale);
                var y = point.Y - (int)(_windowRect.Top * _scale);

                mousePosition.tb_PositionX.Text = x.ToString();
                mousePosition.tb_PositionY.Text = y.ToString();

                //设置返回值
                CursorResult = new Drawing.Point(x, y);
            }
            catch (Exception ex)
            {
                logger.LogError($"CursorPositionWnd:{ex}");
            }
        }

        private void ResetWindowRectangle(WindowType windowType)
        {
            try
            {
                switch (windowType)
                {
                    case WindowType.DeskTop:
                        _windowRect = UiCommon.UIAAutomation.GetDesktop().BoundingRectangle;
                        ResetPositionAndResize();
                        break;
                    case WindowType.ForegroundWindow:
                        Thread t = new Thread(() =>
                        {
                            while (true)
                            {
                                if (cts.Token.IsCancellationRequested)
                                {
                                    logger.LogInformation($"获取当前窗口坐标线程退出");
                                    break;
                                }
                                //IntPtr ptr = UiCommon.GetForegroundWindow();
                                //if (ptr == IntPtr.Zero)
                                //{
                                var point = System.Windows.Forms.Cursor.Position;
                                var handle = NativeMethods.WindowFromPoint(point);
                                IntPtr ptr = NativeMethods.GetAncestor(handle, GetAncestorFlags.GetRoot);
                                // }
                                if (cacheHandle != ptr)
                                {
                                    cacheHandle = ptr;
                                    var element = UiCommon.UIAAutomation.FromHandle(ptr);
                                    if (element != null)
                                    {
                                        _windowRect = element.BoundingRectangle;
                                    }
                                    else
                                    {
                                        _windowRect = UiCommon.UIAAutomation.GetDesktop().BoundingRectangle;
                                    }
                                    ResetPositionAndResize();
                                }
                                Thread.Sleep(100);
                            }
                        });
                        t.Start();

                        break;
                    default:
                        _windowRect = UiCommon.UIAAutomation.GetDesktop().BoundingRectangle;
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"ResetWindowRectangle:{ex}");
            }
        }

        //设置拾取坐标初始窗口
        internal void StartCapture(int type)
        {
            if (Enum.IsDefined(typeof(WindowType), type))
            {
                _windowType = (WindowType)type;
                ResetWindowRectangle(_windowType);
            }
            else
            {
                logger.LogWarning($"无此类型窗口:{type}");
            }
            DrawCrossDispatcherTimer.Tick -= new EventHandler(DispatcherTimer_Tick);
            DrawCrossDispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            DrawCrossDispatcherTimer.Interval = TimeSpan.FromMilliseconds(50);
            DrawCrossDispatcherTimer.Start();
        }
        public void SetPenetrate(bool transparent)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            uint extendedStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            if (transparent)
            {
                NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, extendedStyle | NativeMethods.WS_EX_TRANSPARENT);
            }
            else
            {
                NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, extendedStyle & ~NativeMethods.WS_EX_TRANSPARENT);
            }
        }
        private void CursorPositionWnd_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.Escape)
            {
                StopHighlight();

                if (!this.CheckAccess())
                {
                    this.Dispatcher.Invoke(() => { this.Close(); });
                }
                else
                {
                    this.Close();
                }
            }
            else if (e.KeyCode == System.Windows.Forms.Keys.Control
                || e.KeyCode == System.Windows.Forms.Keys.LControlKey
                || e.KeyCode == System.Windows.Forms.Keys.RControlKey)
            {
                //SetPenetrate(false);
                //var point = System.Windows.Forms.Cursor.Position;
                //var wndPoint = this.PointFromScreen(new System.Windows.Point(point.X, point.Y));
                //x = wndPoint.X;
                //y = wndPoint.Y;

            }
        }
        private void CaptureWindow_MouseUp(object sender, EventArgs e)
        {
            if (_windowType == WindowType.ForegroundWindow)
            {
                //鼠标弹起后 还原数据 (场景 拖拽窗口)
                cacheHandle = IntPtr.Zero;
                _windowRect = new Drawing.Rectangle((int)this.Left, (int)this.Top, (int)this.Width, (int)this.Height);
            }
        }

        internal void StopHighlight()
        {
            mousePosition.SetWndHide();
        }

        private void DrawCross(System.Windows.Point point)
        {
            System.Windows.Media.Brush brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF8910"));

            xCrossLine.Stroke = brush;
            xCrossLine.X1 = point.X - 1;
            xCrossLine.Y1 = 0;
            xCrossLine.X2 = point.X - 1;
            xCrossLine.Y2 = _windowRect.Height;
            xCrossLine.StrokeThickness = 2;
            xCrossLine.StrokeDashArray = new DoubleCollection() { 2, 3 };
            xCrossLine.StrokeDashCap = PenLineCap.Triangle;
            xCrossLine.StrokeEndLineCap = PenLineCap.Square;
            xCrossLine.StrokeStartLineCap = PenLineCap.Round;


            yCrossLine.Stroke = brush;
            yCrossLine.X1 = 0;
            yCrossLine.Y1 = point.Y - 1;
            yCrossLine.X2 = _windowRect.Width;
            yCrossLine.Y2 = point.Y - 1;
            yCrossLine.StrokeThickness = 2;
            yCrossLine.StrokeDashArray = new DoubleCollection() { 2, 3 };
            yCrossLine.StrokeDashCap = PenLineCap.Triangle;
            yCrossLine.StrokeEndLineCap = PenLineCap.Square;
            yCrossLine.StrokeStartLineCap = PenLineCap.Round;
            yCrossLine.SetValue(Canvas.ZIndexProperty, 2);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DrawCrossDispatcherTimer?.Stop();
            cts.Cancel();
            UninstallHook();
        }
    }
}
