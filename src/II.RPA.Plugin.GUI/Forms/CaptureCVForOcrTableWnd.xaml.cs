using Gma.UserActivityMonitor;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Drawing;
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
using System.Windows.Shapes;
using System.Windows.Threading;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.GUI
{
    /// <summary>
    /// wnd_CaputureCVForOcrTable.xaml 的交互逻辑
    /// </summary>
    public partial class CaptureCVForOcrTableWnd : Window
    {
        Bitmap bitmap;
        Bitmap maskBitmap;
        /// <summary>
        /// 是否是应用截图 TRUE 应用截图；FALSE 普通屏幕截图
        /// </summary>
        private bool IsCaptureApp { get; set; }

        public System.Drawing.Point RealStartPoint;
        public void InitialStartPoint()
        {
            RealStartPoint = new System.Drawing.Point();
        }
        public void InitialStartPoint(int _X, int _Y)
        {
            RealStartPoint = new System.Drawing.Point(_X, _Y);
        }
        public void InitialStartPoint(double _X, double _Y)
        {
            RealStartPoint = new System.Drawing.Point((int)_X, (int)_Y);
        }

        public int GetStartPointX
        {
            get { return RealStartPoint.X; }
        }

        public int GetStartPointY
        {
            get { return RealStartPoint.Y; }
        }

        public void SaveImg(Bitmap _BitMap, string _FileName)
        {
            _BitMap.Save(_FileName);
            logger.LogInformation($"保存图片 {_FileName}");
        }

        private double RectWidth;
        private double RectHeight;
        public System.Drawing.Rectangle ImgRectFromScreen = System.Drawing.Rectangle.Empty;//实际屏幕截图部分框体
        private bool isMouseDown = false;
        private bool isCtrlDown = false;
        private bool isDrawingComplete = false;
        private static ILogger logger = Logger.OfContext();

        public System.Drawing.Point startPoint;
        public System.Drawing.Point endPoint;

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

        private DispatcherTimer DrawCrossDispatcherTimer = new DispatcherTimer();
        private bool CtrlKeyDown
        {
            get
            {
                return (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            }
        }

        double CurrentScaleX;//当前DPI缩放
        double CurrentScaleY;//垂直方向上dpi缩放
        bool IsBaseFunc = false;//基础功能调用(屏幕截图) 非应用内截图
        bool HasCaptureScreen = false;//是否已截屏
        private string FilePath { get; set; }
        public CaptureCVForOcrTableWnd(string filePath, bool _IsBaseFunc = true)
        {
            InitializeComponent();
            WinAutomation.CommonUtility.XamlResource.LanguageResourceHelper.GetInstance().SetAppInstance(this);
            logger.LogDebug($"CaptureCVForOcrTableWnd 初始化");
            CurrentScaleX = (double)NativeMethods.DpiX / 96;
            CurrentScaleY = (double)NativeMethods.DpiY / 96;
            this.SourceInitialized += CaptureScreenWnd_SourceInitialized;
            DrawCrossDispatcherTimer.Tick -= new EventHandler(DispatcherTimer_Tick);
            DrawCrossDispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            DrawCrossDispatcherTimer.Interval = TimeSpan.FromMilliseconds(40);
            InstallHook();
            FilePath = filePath;

            IsBaseFunc = _IsBaseFunc;
            if (IsBaseFunc)
            {
                //this.Background = new SolidColorBrush(Colors.Red);
                this.Width = SystemParameters.PrimaryScreenWidth;
                this.Height = SystemParameters.PrimaryScreenHeight;
                this.Left = 0;
                this.Top = 0;
            }
        }

        bool IsByRegion { get; set; }

        public CaptureCVForOcrTableWnd(System.Drawing.Rectangle location, Bitmap _bitmap, System.Drawing.Point _Point, string savepath = null) : this(savepath, false)
        {
            IsByRegion = true;
            this.WindowState = WindowState.Normal;
            this.Width = location.Width / CurrentScaleX;
            this.Height = location.Height / CurrentScaleY;
            //this.Location = new Point(location.X, location.Y);
            this.Left = location.X / CurrentScaleX;
            this.Top = location.Y / CurrentScaleY;

            isDrawingComplete = false;
            isCtrlDown = true;
            isMouseDown = true;////默认窗体出现后即进入拖拽阶段

            this.bitmap = _bitmap;
            //maskBitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            //if (bitmap != null)
            //{
            //    Graphics gg = Graphics.FromImage(maskBitmap);
            //    gg.DrawImage(bitmap, 0, 0);
            //    gg.FillRectangle(new SolidBrush(System.Drawing.Color.FromArgb(100, 0, 0, 0)), 0, 0, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            //}

#if DEBUG
            string _ImgName = $"存图传入的{DateTime.Now.ToString("yyyyMMdd HHmmss")}.jpg";
            SaveImg(_bitmap, _ImgName);
#endif
            logger.LogInformation($"CaptureCVForOcrTableWnd SetWindowBackgroundImg 设置背景图片");
            SetWindowBackgroundImg(_bitmap);
            logger.LogInformation($"CaptureCVForOcrTableWnd SetWindowBackgroundImg 设置背景图片结束");
            double x = _Point.X / CurrentScaleX - this.Left;
            double y = _Point.Y / CurrentScaleY - this.Top;
            //logger.LogDebug($"{this.Left} {this.Top} {this.Width} {this.Height}   点击{_Point.X} {_Point.Y}");
            InitialStartPoint(x, y);

            //var CPoint = PointToScreen(new System.Windows.Point((double)(_Point.X), (double)(_Point.Y)));
            //StartPointX = CPoint.X;
            //StartPointY = CPoint.Y;

            //int _PositionX = point.X - location.X >= 0 ? point.X - location.X : 0;
            //int _PositionY = point.Y - location.Y >= 0 ? point.Y - location.Y : 0;

            //endPoint = startPoint = new System.Drawing.Point(_PositionX, _PositionY);//this.PointToClient(point);//工作区的坐标
            //downX = startPoint.X;
            //downY = startPoint.Y;
        }

        internal void InstallHook()
        {
            UninstallHook();
            HookManager.KeyDown += CaptureWindow_KeyDown;//HookManager_KeyUp;
            HookManager.KeyUp += CaptureWindow_KeyUp;//HookManager_KeyUp;
        }

        internal void UninstallHook()
        {
            HookManager.KeyDown -= CaptureWindow_KeyDown;
            HookManager.KeyUp -= CaptureWindow_KeyUp;
        }
        public void CaptureScreenWnd_SourceInitialized(object sender, EventArgs e)
        {
            //SetPenetrate(true);
        }
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            var _Point = System.Windows.Forms.Cursor.Position;
            GuiElement.overlayForm?.inspectorStatus?.MoveFromLocation(_Point);
            //logger.LogDebug($"CaptureCVForOcrTableWnd DispatcherTimer_Tick isMouseDown {isMouseDown}、 isCtrlDown{isCtrlDown}");
            if (isMouseDown && isCtrlDown)
            {
                //try
                //{
                var wndPoint = this.PointFromScreen(new System.Windows.Point(_Point.X, _Point.Y));//准确
                                                                                                  //logger.LogDebug($"{this.Left} {this.Top} {this.Width} {this.Height}   点击{_Point.X},{_Point.Y} WndPoint:{wndPoint.X},{wndPoint.Y}");

                if (dockPanel.Visibility == Visibility.Hidden)
                {
                    dockPanel.Visibility = Visibility.Visible;
                }
                DrawRectangle(wndPoint);
                //}
                //catch (Exception ex)
                //{
                //    logger.LogError(ex.Message);
                //}
            }
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
        private void CaptureWindow_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            logger.LogDebug($"CaptureWindow_KeyDown 捕获 {e.KeyCode} 、isCtrlDown {isCtrlDown}");
            if (e.KeyCode == System.Windows.Forms.Keys.Escape)
            {
                isMouseDown = false;
                RealStartPoint = new System.Drawing.Point();
                this.Close();
            }
            else if (e.KeyCode == System.Windows.Forms.Keys.Control
                || e.KeyCode == System.Windows.Forms.Keys.LControlKey
                || e.KeyCode == System.Windows.Forms.Keys.RControlKey)
            {
                //if (GetAsyncKeyState(0x01) < 0 || Mouse.LeftButton == MouseButtonState.Pressed)//鼠标左键
                //{
                //    isMouseDown = true;
                //}

                ////if (isCtrlDown && !isMouseDown)
                ////{
                ////    return;
                ////}
                if (isCtrlDown)
                {
                    //logger.LogDebug($"CaptureWindow_KeyDown isCtrlDown {isCtrlDown} 连续触发");
                    return;//防止连续触发
                }

                logger.LogDebug($"CaptureWindow_KeyDown isCtrlDown {isCtrlDown} 绕过连续触发");

                GuiElement.overlayForm?.inspectorStatus?.SetWndHide();
                btn_ok.Visibility = Visibility.Hidden;
                if (IsBaseFunc && !HasCaptureScreen)
                {
                    //logger.LogDebug("ChangeWindowState");
                    //this.Visibility = Visibility.Collapsed;
                    using (var currentDesktopScreenshot = UiCommon.UIAAutomation.GetDesktop().Capture())
                    {
#if DEBUG
                        string _ImgName = $"存图传入的{DateTime.Now.ToString("yyyyMMdd HHmmss")}.jpg";
                        SaveImg(currentDesktopScreenshot, _ImgName);
#endif
                        SetWindowBackgroundImg(currentDesktopScreenshot);
                        HasCaptureScreen = true;
                    }
                    //this.Visibility = Visibility.Visible;
                    //logger.LogDebug("ChangeWindowState End");
                }
                //GuiElement.overlayForm.SetOpacity(1);
                SetPenetrate(false);
                logger.LogDebug($"CaptureWindow_KeyDown isCtrlDown {isCtrlDown}、IsBaseFunc {IsBaseFunc} SetPenetrate(false); 停止穿透 进入画框模式");
                //SetPenetrate(true);//穿透效果
                isCtrlDown = true;
                IsDrawingComplete = false;
            }
        }
        private void CaptureWindow_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            //logger.LogDebug($"CaptureWindow_KeyUp 捕捉按键 {e.KeyCode} isMouseDown {isMouseDown} 、isCtrlDown {isCtrlDown}");
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
                }

                isCtrlDown = false;
                GuiElement.overlayForm?.inspectorStatus?.SetWndShow();
            }
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


        private void DrawText(double x, double y, string text, System.Windows.Media.Color color, Canvas canvasObj)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.Text = text;
            textBlock.Foreground = new SolidColorBrush(color);
            Canvas.SetLeft(textBlock, x);
            Canvas.SetTop(textBlock, y);
            canvasObj.Children.Add(textBlock);
        }

        private Bitmap desktopImg = null;
        private void StartCapture()
        {
            //var point = Mouse.GetPosition(this);
            //StartPointX = point.X;
            //StartPointY = point.Y;
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

        /*
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
        */

        private void DrawRectangle(System.Windows.Point _CurrentPoint)
        {
            double dx = _CurrentPoint.X;
            double dy = _CurrentPoint.Y;

            if (dx < 0)
            {
                dx = 0;
            }
            else if (dx > this.Width)
            {
                dx = this.Width;
            }

            if (dy < 0)
            {
                dy = 0;
            }
            else if (dy > this.Height)
            {
                dy = this.Height;
            }

            //if (dx > GetStartPointX)
            //{
            //    dx = GetStartPointX;
            //}

            //if (dy > GetStartPointY)
            //{
            //    dy = GetStartPointY;
            //}

            //logger.LogDebug($"当前点坐标处理后 {dx},{dy}");

            //logger.LogDebug($"当前点坐标 {dx},{dy} 起始点坐标 {GetStartPointX}, {GetStartPointY}");

            double rectWidth = Math.Abs(dx - GetStartPointX);
            double rectHeight = Math.Abs(dy - GetStartPointY);
            //logger.LogDebug($"rectWidth {rectWidth},rectHeight{rectHeight}");

            SolidColorBrush brush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(00, 255, 255, 255));
            lightRect.Width = rectWidth;
            lightRect.Height = rectHeight;
            lightRect.Fill = brush;
            lightRect.Stroke = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 30, 144, 255));//蓝色的
            //lightRect.Stroke = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 0, 255));
            lightRect.StrokeThickness = 2;

            if (dx < GetStartPointX)
            {
                lightRect_Left.Width = dx;
                ImgRectFromScreen.X = (int)((dx) * CurrentScaleX);
            }
            else
            {
                lightRect_Left.Width = GetStartPointX;
                ImgRectFromScreen.X = (int)(GetStartPointX * CurrentScaleX);
            }

            if (dy < GetStartPointY)
            {
                lightRect_Top.Height = dy;
                ImgRectFromScreen.Y = (int)(dy * CurrentScaleY);
            }
            else
            {
                lightRect_Top.Height = GetStartPointY;
                ImgRectFromScreen.Y = (int)(GetStartPointY * CurrentScaleY);
            }

            double _RightRectWidth = this.Width - lightRect_Left.Width - lightRect.Width;
            double _BottomRectHeight = this.Height - lightRect_Top.Height - lightRect.Height;
            //logger.LogDebug($"RightRectWidth:{_RightRectWidth} { this.Width } { lightRect_Left.Width } { lightRect.Width}");
            //logger.LogDebug($"lightRect_Bottom.Height:{lightRect_Bottom.Height} { this.Height } { lightRect_Top.Height } { lightRect.Height}");
            lightRect_Right.Width = _RightRectWidth >= 0 ? _RightRectWidth : 0;
            lightRect_Bottom.Height = _BottomRectHeight > 0 ? _BottomRectHeight : 0;
            ImgRectFromScreen.Width = (int)(lightRect.Width * CurrentScaleX);
            ImgRectFromScreen.Height = (int)(lightRect.Height * CurrentScaleY);
        }

        private async void CaptureScreen()
        {
            CaptureCanvas.Children.Clear();
            await Task.Run(() =>
            {
                Thread.Sleep(10);
                this.Dispatcher.Invoke(() =>
                {
                    var point = this.PointToScreen(new System.Windows.Point(ImgRectFromScreen.X / CurrentScaleX, ImgRectFromScreen.Y / CurrentScaleY));
                    //Console.WriteLine($"ImgRectFromScreen X：{ImgRectFromScreen.X} Y:{ImgRectFromScreen.Y} width:{ImgRectFromScreen.Width} height:{ImgRectFromScreen.Height}");
                    //logger.LogInformation($"测试最新版本截图 ImgRectFromScreen X：{ImgRectFromScreen.X} Y:{ImgRectFromScreen.Y} width:{ImgRectFromScreen.Width} height:{ImgRectFromScreen.Height}");
                    using (System.Drawing.Bitmap bitmap = new Bitmap(ImgRectFromScreen.Width, ImgRectFromScreen.Height))
                    {
                        //string strBase64 = II.RPA.WinAutomation.CommonUtility.ImageHelper.Bitmap2Base64(bitmap);
                        ///II.RPA.WinAutomation.CommonUtility.logger.LogInformation($"当前Base64 FFFF:{strBase64}");
                        using (System.Drawing.Graphics graphics = Graphics.FromImage(bitmap))
                        {
                            graphics.CopyFromScreen((int)point.X, (int)point.Y, 0, 0, ImgRectFromScreen.Size);
                            graphics.Save();
                        }

                        //Console.WriteLine($"存图路径：{this.FilePath}");
                        //strBase64 = II.RPA.WinAutomation.CommonUtility.ImageHelper.Bitmap2Base64(bitmap);
                        ///II.RPA.WinAutomation.CommonUtility.logger.LogInformation($"当前Base64 DDDD:{strBase64}");

                        string dirPath = System.IO.Path.GetDirectoryName(this.FilePath);
#if DEBUG
                        if (string.IsNullOrEmpty(dirPath))
                        {
                            dirPath = System.Environment.CurrentDirectory;
                        }
#endif

                        if (!System.IO.Directory.Exists(dirPath))
                        {
                            System.IO.Directory.CreateDirectory(dirPath);
                        }
                        bitmap.Save(this.FilePath);
                    }

                    startPoint = new System.Drawing.Point((int)ImgRectFromScreen.X, (int)ImgRectFromScreen.Y);
                    endPoint = new System.Drawing.Point((int)ImgRectFromScreen.X + ImgRectFromScreen.Width, (int)ImgRectFromScreen.Y + ImgRectFromScreen.Height);

                    if (this.WindowState == WindowState.Maximized)
                    {
                        startPoint.X += (int)this.Left;
                        startPoint.Y += (int)this.Top;
                        endPoint.X += (int)this.Left;
                        endPoint.Y += (int)this.Top;
                    }
                    isMouseDown = false;
                    InitialStartPoint();
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
            if (btnX + btn_ok.ActualWidth >= this.ActualWidth)
            {
                btnX = (int)(this.lightRect_Left.ActualWidth + ImgRectFromScreen.Width / CurrentScaleX - btn_ok.ActualWidth);
            }
            if (btnY >= this.ActualHeight - btn_ok.ActualWidth)
            {
                if (this.lightRect_Top.ActualHeight >= btn_ok.ActualHeight)
                {
                    btnY = (int)(this.lightRect_Top.ActualHeight - btn_ok.ActualHeight);
                }
                else
                {
                    btnY = (int)(this.lightRect_Top.ActualHeight + ImgRectFromScreen.Height / CurrentScaleY - btn_ok.ActualHeight);
                }
            }


            if (btnY < 0)
            {
                btnY = 0;
            }
            else if (btnY > this.Height - 25)
            {
                btnY = this.Height - 25;
            }

            if (btnX > this.Width - 60)
            {
                btnX = this.Width - 60;
            }
            else if (btnX < 0)
            {
                btnX = 0;
            }

            Canvas.SetLeft(btn_ok, btnX);
            Canvas.SetTop(btn_ok, btnY);
            CaptureCanvas.IsHitTestVisible = true;
        }

        private void AddOkBtn_Click(object sender, RoutedEventArgs e)
        {
            CaptureScreen();
        }


        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                RectWidth = Math.Abs(e.GetPosition(null).X - GetStartPointX);
                RectHeight = Math.Abs(e.GetPosition(null).Y - GetStartPointY);
                if (RectWidth <= 0 || RectHeight <= 0)
                {
                    logger.LogInformation($"RectWidth:{RectWidth} RectHeight:{RectHeight} 检测到参数异常 截图无效");
                    return;
                }

                btn_ok.Visibility = Visibility.Hidden;
                CaptureScreen();
                logger.LogInformation("双击截图");
            }
        }


        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isMouseDown && isCtrlDown)
            {
                IsDrawingComplete = true;
            }
            isMouseDown = false;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //if (IsDrawingComplete && e.RightButton == MouseButtonState.Pressed)
            //{
            //    IsDrawingComplete = false;
            //    btn_ok.Visibility = Visibility.Hidden;
            //}

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                isMouseDown = true;
                if (CtrlKeyDown)
                {
                    btn_ok.Visibility = Visibility.Hidden;
                    InitialStartPoint(e.GetPosition(null).X, e.GetPosition(null).Y);

                    logger.LogInformation($"Window_MouseDown e.GetPosition: {GetStartPointX} {GetStartPointY}");
                    Console.WriteLine($"Window_MouseDown e.GetPosition: {GetStartPointX} {GetStartPointY}");
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!IsByRegion)
            {
                if (GuiElement.overlayForm.inspectorStatus == null)
                {
                    GuiElement.overlayForm.inspectorStatus = new InspectInfoWnd();
                }
                GuiElement.overlayForm.inspectorStatus.InitCaptureByRegion("区域拾取");
                GuiElement.overlayForm.inspectorStatus.SetWndShow();
            }
            NativeMethods.SetProcessDPIAware();
            //this.Width = SystemParameters.PrimaryScreenWidth;
            //this.Height = SystemParameters.PrimaryScreenHeight;

            // 与 ctor 解耦：避免 Loaded 前 Tick 访问未初始化的 inspectorStatus（与 CaptureScreenWnd 一致）
            DrawCrossDispatcherTimer.Start();
        }
    }
}
