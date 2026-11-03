using Gma.UserActivityMonitor;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.GUI
{
    /// <summary>
    /// CaptureScreenWnd.xaml 的交互逻辑
    /// </summary>
    public partial class CaptureScreenWnd : Window
    {
        private double x;
        private double y;
        private double width;
        private double height;
        public System.Drawing.Rectangle ImgRectFromScreen = System.Drawing.Rectangle.Empty;
        private bool isMouseDown = false;
        private bool isCtrlDown = false;
        private bool isDrawingComplete = false;
        double CurrentScaleX = 1;//当前DPI缩放
        double CurrentScaleY = 1;//垂直方向上dpi缩放
        private static ILogger logger = Logger.OfContext();

        private bool IsDrawingComplete
        {
            get
            {
                return isDrawingComplete;
            }
            set
            {
                isDrawingComplete = value;
                if (isDrawingComplete)
                {
                    if (ImgRectFromScreen.Width <= 0 || ImgRectFromScreen.Height <= 0)
                    {
                        isDrawingComplete = false;
                    }
                    else
                    {
                        AddOkBtn();
                    }
                }
            }
        }

        private bool IsOnCaptureScreen = false;

        private DispatcherTimer DrawCrossDispatcherTimer = new DispatcherTimer();
        private bool CtrlKeyDown
        {
            get
            {
                return (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            }
        }
        private string FilePath { get; set; }
        public CaptureScreenWnd(string filePath)
        {
            InitializeComponent();
            WinAutomation.CommonUtility.XamlResource.LanguageResourceHelper.GetInstance().SetAppInstance(this);
            CurrentScaleX = (double)NativeMethods.DpiX / 96;
            CurrentScaleY = (double)NativeMethods.DpiY / 96;
            this.SourceInitialized += CaptureScreenWnd_SourceInitialized;
            DrawCrossDispatcherTimer.Tick -= new EventHandler(DispatcherTimer_Tick);
            DrawCrossDispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            DrawCrossDispatcherTimer.Interval = TimeSpan.FromMilliseconds(40);
            InstallHook();
            FilePath = filePath;
        }
        bool b = true;
        internal void InstallHook()
        {
            UninstallHook();
            HookManager.KeyDown += CaptureWindow_KeyDown;//HookManager_KeyUp;
            HookManager.KeyUp += CaptureWindow_KeyUp;//HookManager_KeyUp;
            //HookManager.MouseDown += CaptureWindow_MouseDown;
            //HookManager.MouseUp += CaptureWindow_MouseUp;
            //HookManager.MouseDoubleClick += CaptureWindow_MouseDoubleClick;

        }

        internal void UninstallHook()
        {
            HookManager.KeyDown -= CaptureWindow_KeyDown;
            HookManager.KeyUp -= CaptureWindow_KeyUp; ;
            //HookManager.MouseDown -= CaptureWindow_MouseDown;
            //HookManager.MouseUp -= CaptureWindow_MouseUp;
            //HookManager.MouseDoubleClick -= CaptureWindow_MouseDoubleClick;
        }
        public void CaptureScreenWnd_SourceInitialized(object sender, EventArgs e)
        {
            SetPenetrate(true);
        }
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            var point = System.Windows.Forms.Cursor.Position;
            GuiElement.overlayForm?.inspectorStatus?.MoveFromLocation(point);
            var wndPoint = this.PointFromScreen(new System.Windows.Point(point.X, point.Y));

            //this.InvalidateVisual();

            if (isMouseDown && isCtrlDown)
            {
                //if (xCrossLine.Visibility == Visibility.Visible)
                //{
                //    xCrossLine.Visibility = Visibility.Hidden;
                //    yCrossLine.Visibility = Visibility.Hidden;
                //}
                if (dockPanel.Visibility == Visibility.Hidden)
                {
                    dockPanel.Visibility = Visibility.Visible;
                }
                DrawRectangle(wndPoint);
            }

            //if (IsOnCaptureScreen)
            //{
            //    if (xCrossLine.Visibility == Visibility.Visible)
            //    {
            //        xCrossLine.Visibility = Visibility.Hidden;
            //    }
            //    if (yCrossLine.Visibility == Visibility.Visible)
            //    {
            //        yCrossLine.Visibility = Visibility.Hidden;
            //    }
            //    logger.LogDebug("定时器关闭十字线");
            //    return;
            //}

            //if (!IsDrawingComplete)
            //{
            //    if (xCrossLine.Visibility == Visibility.Hidden)
            //    {
            //        xCrossLine.Visibility = Visibility.Visible;
            //        yCrossLine.Visibility = Visibility.Visible;
            //        logger.LogDebug("定时器开启十字线");
            //    }
            //    if (dockPanel.Visibility == Visibility.Visible)
            //    {
            //        dockPanel.Visibility = Visibility.Hidden;
            //    }
            //    DrawCross(wndPoint);
            //}
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
        [DllImport("User32")]
        public static extern short GetAsyncKeyState(int vKey);

        bool HasCaptureScreen = false;//是否已截屏
        private void CaptureWindow_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.Escape)
            {
                isMouseDown = false;
                x = 0.0;
                y = 0.0;
                this.Close();
            }
            else if (e.KeyCode == System.Windows.Forms.Keys.Control
                || e.KeyCode == System.Windows.Forms.Keys.LControlKey
                || e.KeyCode == System.Windows.Forms.Keys.RControlKey)
            {
                if (GetAsyncKeyState(0x01) < 0)
                {
                    isMouseDown = true;
                }
                if (isCtrlDown)
                {
                    return;
                }
                GuiElement.overlayForm?.inspectorStatus?.SetWndHide();
                btn_ok.Visibility = Visibility.Hidden;

                if (!HasCaptureScreen)
                {
                    //IsOnCaptureScreen = true;
                    //this.Dispatcher.Invoke(()=> {
                    //    xCrossLine.Visibility = Visibility.Hidden;
                    //    yCrossLine.Visibility = Visibility.Hidden;
                    //    logger.LogDebug("CaptureWindow_KeyDown 隐藏十字线");
                    //});

                    //Thread.Sleep(80);
                    using (System.Drawing.Bitmap currentDesktopScreenshot = new Bitmap((int)(this.Width * CurrentScaleX), (int)(this.Height * CurrentScaleY)))
                    {

                        using (System.Drawing.Graphics graphics = Graphics.FromImage(currentDesktopScreenshot))
                        {
                            graphics.CopyFromScreen(0, 0, 0, 0, currentDesktopScreenshot.Size);
                            graphics.Save();
                        }
#if DEBUG
                        string _ImgName = $"存图传入的{DateTime.Now.ToString("yyyyMMdd HHmmss")}.jpg";
                        SaveImg(currentDesktopScreenshot, _ImgName);
#endif
                        SetWindowBackgroundImg(currentDesktopScreenshot);
                        HasCaptureScreen = true;
                    }
                    //Thread.Sleep(50);
                    //this.Dispatcher.Invoke(() => {
                    //    xCrossLine.Visibility = Visibility.Visible;
                    //    yCrossLine.Visibility = Visibility.Visible;
                    //    logger.LogDebug("CaptureWindow_KeyDown 显示十字线");
                    //});
                    //IsOnCaptureScreen = false;
                }

                SetPenetrate(false);
                logger.LogDebug($"CaptureWindow_KeyDown isCtrlDown {isCtrlDown}、 isMouseDown {isMouseDown} SetPenetrate(false); 停止穿透");
                isCtrlDown = true;
                IsDrawingComplete = false;

                //if (isMouseDown)
                {
                    var point = System.Windows.Forms.Cursor.Position;
                    var wndPoint = this.PointFromScreen(new System.Windows.Point(point.X, point.Y));
                    x = wndPoint.X;
                    y = wndPoint.Y;
                }
            }
        }

        public void SaveImg(Bitmap _BitMap, string _FileName)
        {
            _BitMap.Save(_FileName);
            logger.LogInformation($"保存图片{_FileName}");
        }

        public void SetWindowBackgroundImg(Bitmap _bitmap, double _opacity = 1)
        {
            var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(_bitmap.GetHbitmap(),
                                                                              IntPtr.Zero,
                                                                              Int32Rect.Empty,
                                                                              BitmapSizeOptions.FromEmptyOptions()
              );
            _bitmap.Dispose();
            var brush = new ImageBrush(bitmapSource);
            this.Opacity = _opacity;
            this.Background = brush;
        }

        private void CaptureWindow_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.Control
                || e.KeyCode == System.Windows.Forms.Keys.LControlKey
                || e.KeyCode == System.Windows.Forms.Keys.RControlKey)
            {
                if (isMouseDown && isCtrlDown)
                {
                    IsDrawingComplete = true;
                }
                if (!IsDrawingComplete)
                {
                    btn_ok.Visibility = Visibility.Hidden;
                    //SetPenetrate(true);
                }
                isCtrlDown = false;
                GuiElement.overlayForm?.inspectorStatus?.SetWndShow();
            }
        }
        private void CaptureWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //if (IsDrawingComplete && e.RightButton == MouseButtonState.Pressed)
            //{
            //    IsDrawingComplete = false;
            //    DrawCross(e.GetPosition(null));
            //    btn_ok.Visibility = Visibility.Hidden;
            //    SetPenetrate(true);
            //}
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                isMouseDown = true;
                if (CtrlKeyDown)
                {
                    btn_ok.Visibility = Visibility.Hidden;
                    x = e.GetPosition(null).X;
                    y = e.GetPosition(null).Y;
                }
            }
        }

        private void CaptureWindow_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //var point = e.GetPosition(null);
            //if (isMouseDown && CtrlKeyDown)
            //{
            //    DrawRectangle(point);
            //}
            //else if (!isDrawingComplete)
            //{
            //    DrawCross(point);
            //}

            //IntPtr foregroundWindow = NativeMethods.GetForegroundWindow();
            //IntPtr hwnd = new WindowInteropHelper(this).Handle;
            //DrawText(20, 20, $"{hwnd},{foregroundWindow}", System.Windows.Media.Color.FromRgb(200, 0, 0), CaptureCanvas);
        }

        private void DrawText(double x, double y, string text, System.Windows.Media.Color color, Canvas canvasObj)
        {

            TextBlock textBlock = new TextBlock();

            textBlock.Text = text;

            textBlock.Foreground = new SolidColorBrush(color);

            Canvas.SetLeft(textBlock, x);

            Canvas.SetTop(textBlock, y);

            canvasObj.Children.Add(textBlock);

        }
        private void CaptureWindow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                width = Math.Abs(e.GetPosition(null).X - x);
                height = Math.Abs(e.GetPosition(null).Y - y);
                if (width <= 0 || height <= 0)
                {
                    logger.LogInformation($"width:{width} height:{height} 检测到参数异常 截图无效");
                    return;
                }

                btn_ok.Visibility = Visibility.Hidden;
                CaptureScreen();
                logger.LogInformation($"双击截图");
            }
        }
        private void CaptureWindow_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isMouseDown && isCtrlDown)
            {
                IsDrawingComplete = true;
            }
            isMouseDown = false;
        }


        private Bitmap desktopImg = null;
        private void StartCapture()
        {
            var point = Mouse.GetPosition(this);
            x = point.X;
            y = point.Y;
            //var screenArea = Screen.FromPoint(new System.Drawing.Point
            //{
            //    X = (int)point.X,
            //    Y = (int)point.Y
            //}).WorkingArea;
            //System.Drawing.Bitmap bitmap = new Bitmap(screenArea.Width, screenArea.Height);
            //using (System.Drawing.Graphics graphics = Graphics.FromImage(bitmap))
            //{
            //    graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
            //}

        }
        private void DrawCross(System.Windows.Point point)
        {
            System.Windows.Media.Brush brush = System.Windows.Media.Brushes.LightSkyBlue;
            if (CtrlKeyDown)
            {
                brush = System.Windows.Media.Brushes.Red;
            }
            xCrossLine.Stroke = brush;
            xCrossLine.X1 = point.X - 1;
            xCrossLine.Y1 = 0;
            xCrossLine.X2 = point.X - 1;
            xCrossLine.Y2 = this.Height;
            xCrossLine.StrokeThickness = 2;

            yCrossLine.Stroke = brush;
            yCrossLine.X1 = 0;
            yCrossLine.Y1 = point.Y - 1;
            yCrossLine.X2 = this.Width;
            yCrossLine.Y2 = point.Y - 1;
            yCrossLine.StrokeThickness = 2;
            yCrossLine.SetValue(Canvas.ZIndexProperty, 2);
        }

        private void DrawRectangle(System.Windows.Point point)
        {
            //try
            //{
            //logger.LogDebug($"当前点坐标 {point.X},{point.Y} 起始点{x}，{y}");
            double dx = point.X;
            double dy = point.Y;
            //if (dx < 0)
            //{
            //    dx = 0;
            //}
            //else if (dx > this.width)
            //{
            //    dx = this.width;
            //}

            //if (dy < 0)
            //{
            //    dy = 0;
            //}
            //else if (dy > this.height)
            //{
            //    dy = this.height;
            //}

            //logger.LogDebug($"当前点坐标处理后 {dx},{dy}");

            double rectWidth = Math.Abs(dx - x);
            double rectHeight = Math.Abs(dy - y);
            SolidColorBrush brush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(00, 255, 255, 255));
            //logger.LogDebug($"rectWidth {rectWidth},rectHeight {rectHeight}");
            lightRect.Width = rectWidth;
            lightRect.Height = rectHeight;
            lightRect.Fill = brush;
            lightRect.Stroke = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 30, 144, 255));
            lightRect.StrokeThickness = 2;

            if (dx < x)
            {
                lightRect_Left.Width = dx;
                ImgRectFromScreen.X = (int)((dx) * CurrentScaleX);
            }
            else
            {
                lightRect_Left.Width = x;
                ImgRectFromScreen.X = (int)(x * CurrentScaleX);
            }

            if (dy < y)
            {
                lightRect_Top.Height = dy;
                ImgRectFromScreen.Y = (int)(dy * CurrentScaleY);
            }
            else
            {
                lightRect_Top.Height = y;
                ImgRectFromScreen.Y = (int)(y * CurrentScaleY);
            }

            lightRect_Right.Width = this.Width - lightRect_Left.Width - lightRect.Width;// this.Width - lightRect_Left.Width - lightRect.Width>=0? this.Width - lightRect_Left.Width - lightRect.Width:0;
            lightRect_Bottom.Height = this.Height - lightRect_Top.Height - lightRect.Height;//this.Height - lightRect_Top.Height - lightRect.Height>0? this.Height - lightRect_Top.Height - lightRect.Height:0;
            ImgRectFromScreen.Width = (int)(lightRect.Width * CurrentScaleX);
            ImgRectFromScreen.Height = (int)(lightRect.Height * CurrentScaleY);
            //}
            //catch (Exception ex)
            //{
            //    logger.LogError($"DrawRect {ex.ToString()}");
            //    throw ex;
            //}

        }

        private async void CaptureScreen()
        {
            CaptureCanvas.Children.Clear();
            x = 0.0;
            y = 0.0;
            await Task.Run(() =>
            {
                Thread.Sleep(10);
                this.Dispatcher.Invoke(() =>
                {
                    var point = this.PointToScreen(new System.Windows.Point(ImgRectFromScreen.X / CurrentScaleX, ImgRectFromScreen.Y / CurrentScaleY));
                    var point_wh = this.PointToScreen(new System.Windows.Point(ImgRectFromScreen.Width / CurrentScaleX, ImgRectFromScreen.Height / CurrentScaleY));
                    Console.WriteLine($"ImgRectFromScreen X：{ImgRectFromScreen.X} Y:{ImgRectFromScreen.Y} width:{ImgRectFromScreen.Width} height:{ImgRectFromScreen.Height}");

                    using (System.Drawing.Bitmap bitmap = new Bitmap((int)point_wh.X, (int)point_wh.Y))
                    {
                        string strBase64 = WinAutomation.CommonUtility.ImageHelper.Bitmap2Base64(bitmap);
                        logger.LogInformation($"获取截图的Base64");
                        using (System.Drawing.Graphics graphics = Graphics.FromImage(bitmap))
                        {
                            graphics.CopyFromScreen((int)point.X, (int)point.Y, 0, 0, bitmap.Size);
                            graphics.Save();
                        }

                        Console.WriteLine($"存图路径：{this.FilePath}");
                        strBase64 = WinAutomation.CommonUtility.ImageHelper.Bitmap2Base64(bitmap);


                        string dirPath = System.IO.Path.GetDirectoryName(this.FilePath);
                        if (!System.IO.Directory.Exists(dirPath))
                        {
                            System.IO.Directory.CreateDirectory(dirPath);
                        }
                        bitmap.Save(this.FilePath);
                    }
                    isMouseDown = false;
                    this.DialogResult = true;
                });
            });
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DrawCrossDispatcherTimer?.Stop();
            UninstallHook();
            //GuiElement.overlayForm?.inspectorStatus?.Close();
            GuiElement.overlayForm?.inspectorStatus?.SetWndClose();
        }
        private void AddOkBtn()
        {
            btn_ok.Visibility = Visibility.Visible;
            var btnX = (ImgRectFromScreen.X + ImgRectFromScreen.Width) / CurrentScaleX;
            var btnY = (ImgRectFromScreen.Y + ImgRectFromScreen.Height) / CurrentScaleY;
            if (btnX + btn_ok.Width >= this.Width)
            {
                btnX = (int)(this.lightRect_Left.Width + ImgRectFromScreen.Width / CurrentScaleX - btn_ok.Width);
            }
            if (btnY >= this.Height - btn_ok.Height)
            {
                if (this.lightRect_Top.Height >= btn_ok.Height)
                {
                    btnY = (int)(this.lightRect_Top.Height - btn_ok.Height);
                }
                else
                {
                    btnY = (int)(this.lightRect_Top.Height + ImgRectFromScreen.Height / CurrentScaleY - btn_ok.Height);
                }
            }
            Canvas.SetLeft(btn_ok, btnX);
            Canvas.SetTop(btn_ok, btnY);
            CaptureCanvas.IsHitTestVisible = true;
        }

        private void AddOkBtn_Click(object sender, RoutedEventArgs e)
        {
            CaptureScreen();
            //x = 0.0;
            //y = 0.0;
            //this.DialogResult = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            NativeMethods.SetProcessDPIAware();

            this.Left = 0;
            this.Top = 0;
            this.Width = SystemParameters.PrimaryScreenWidth;
            this.Height = SystemParameters.PrimaryScreenHeight;

            GuiElement.overlayForm.InitCancellationForCapture();
            if (GuiElement.overlayForm.inspectorStatus == null)
            {
                GuiElement.overlayForm.inspectorStatus = new InspectInfoWnd();
            }
            GuiElement.overlayForm.inspectorStatus.InitCaptureByRegion("区域拾取");
            GuiElement.overlayForm.inspectorStatus.SetWndShow();

            DrawCrossDispatcherTimer.Start();
        }
    }
}
