using Gma.UserActivityMonitor;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.ProcessCreator.ActionProvider;
using II.RPA.Plugin.GUI.ProcessCreator.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace II.RPA.Plugin.GUI.ProcessCreator.Screenshot
{
    /// <summary>
    /// Interaction logic for CaptureWindow.xaml
    /// </summary>
    public partial class CaptureWindow : Window
    {
        private static readonly ILogger logger = Logger.OfContext();
        public static double ScreenWidth = SystemParameters.PrimaryScreenWidth;
        public static double ScreenHeight = SystemParameters.PrimaryScreenHeight;
        public static double ScreenScale = 1;
        public static int MinSize = 10;

        //画图注册名称集合
        public List<NameAndLimit> list = new List<NameAndLimit>();
        //画图注册名称
        public int num = 1;

        //是否截图开始
        private bool _IsMouseDown = false;
        //是否截图完毕
        private bool _IsCapture = false;

        private double _X0 = 0;
        private double _Y0 = 0;

        private ManualResetEventSlim m_ManualResetEvent;

        public CaptureWindow()
        {
            _Current = this;
            InitializeComponent();
            WinAutomation.CommonUtility.XamlResource.LanguageResourceHelper.GetInstance().SetAppInstance(this);
            DataContext = new DataContextModel();
            Background = new ImageBrush(ImageHelper.GetFullBitmapSource());
            WpfHelper.MainDispatcher = Dispatcher;
            MaxWindow();
            MaskLeft.Height = ScreenHeight;
            MaskRight.Height = ScreenHeight;
            //计算Windows项目缩放比例
            ScreenHelper.ResetScreenScale();
            logger.LogInformation($"ScreenScale:{ScreenScale}");
            HookManager.KeyUp -= HookManager_KeyUp;
            HookManager.KeyUp += HookManager_KeyUp;
            Closing += CaptureWindow_Closing;
            //Cursor = new Cursor(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"cur.cur"));
            Cursor = Cursors.Arrow;
        }

        private void CaptureWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            HookManager.KeyUp -= HookManager_KeyUp;
        }

        public CaptureWindow(bool anIsOnceTime, ManualResetEventSlim manualResetEventSlim, ShotPolicy policy = ShotPolicy.FULL) :this()
        {
            IsOnceTime = anIsOnceTime;
            m_ManualResetEvent = manualResetEventSlim;
            ShotPolicy = policy;
        }
        public bool IsOnceTime 
        { 
            get; 
            private set; 
        }
        public ShotPolicy ShotPolicy 
        { 
            get; 
            private set; 
        }
        public string SelectorResult
        {
            get;
            private set;
        }
        public void SetSelector(string aSelector)
        {
            SelectorResult = aSelector;
            m_ManualResetEvent.Set();

        }
        private void HookManager_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.Escape)
            {
                EventTrackerModel eventTraker = EventTrackerModel.SetEventModel("screenshot.cancel", new JObject { { "triggertype", "esc" } });
                MessageService.PostMessage(TerminalType.CoreServer, "flowedit.tracking", eventTraker.ToString(), _ => { });
                if (m_ManualResetEvent != null)
                {
                    SetSelector("-1");
                    Close();
                }
                else
                {
                    Close();
                }
            }
        }

        #region 属性 Current
        private static CaptureWindow _Current = null;
        public static CaptureWindow Current
        {
            get
            {
                return _Current;
            }
        }
        #endregion

        #region 全屏+置顶
        private void MaxWindow()
        {
            Left = 0;
            Top = 0;
            Width = ScreenWidth;
            Height = ScreenHeight;
            Activate();
        }
        #endregion

        #region 获取截图
        private BitmapSource GetCapture()
        {
            return ImageHelper.GetBitmapSource((int)DataContextModel.Current.MaskLeftWidth + 1,
                (int)DataContextModel.Current.MaskTopHeight + 1, 
                (int)MainImage.ActualWidth - 2, 
                (int)MainImage.ActualHeight - 2);
        }
        #endregion

        #region 退出截图
        public void OnCancel()
        {
            Close();
        }
        #endregion

        #region 完成截图
        public void OnOK()
        {
            Hidden();
            Thread t = new Thread(new ThreadStart(() =>
            {
                Thread.Sleep(50);
                WpfHelper.SafeRun(() =>
                {
                    var source = GetCapture();
                    if (source != null)
                    {
                        Clipboard.SetImage(source);
                    }
                    Close();
                });
            }))
            {
                IsBackground = true
            };
            t.Start();
        }
        #endregion

        #region 截图前隐藏窗口
        private void Hidden()
        {
            //隐藏尺寸RGB框
            if (DataContextModel.Current.MaskTopHeight < 40)
            {
                SizeRGB.Visibility = Visibility.Collapsed;
            }
            var need = 30;
            if (DataContextModel.Current.MaskBottomHeight < need && DataContextModel.Current.MaskTopHeight < need)
            {
                ImageActionBar.Current.Visibility = Visibility.Collapsed;
            }
            MainImage.ZoomThumbVisibility = Visibility.Collapsed;
        }
        #endregion

        #region 鼠标及键盘事件
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_IsCapture)
            {
                return;
            }
            var point = e.GetPosition(this);
            _X0 = point.X;
            _Y0 = point.Y;
            //logger.LogDebug($"xy:{_X0},{_Y0}");
            _IsMouseDown = true;
            Canvas.SetLeft(MainImage, _X0);
            Canvas.SetTop(MainImage, _Y0);
            DataContextModel.Current.MaskLeftWidth = _X0;
            DataContextModel.Current.MaskRightWidth = ScreenWidth - _X0;
            DataContextModel.Current.MaskTopHeight = _Y0;
            Show_Size.Visibility = Visibility.Visible;
            LocationBorder.Visibility = Visibility.Collapsed;
        }

        public void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_IsMouseDown || _IsCapture)
            {
                return;
            }
            _IsMouseDown = false;
            if (MainImage.Width >= MinSize && MainImage.Height >= MinSize)
            {
                _IsCapture = true;
                ImageActionBar.Current.IsCaptureOnceTime = IsOnceTime;
                ImageActionBar.Current.Visibility = Visibility.Visible;
                ImageActionBar.Current.ResetCanvas();
                Cursor = Cursors.Arrow;
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            var point = e.GetPosition(this);
            //logger.LogDebug($"point:{point.X},{point.Y}");
            var screenP = PointToScreen(point);

            DataContextModel.Current.MousePointX = screenP.X;
            DataContextModel.Current.MousePointY = screenP.Y;
            DataContextModel.Current.LocationBorderLeft = screenP.X > ScreenWidth - 140 ? (screenP.X - 140)/ ScreenScale : (screenP.X + 5)/ScreenScale;
            DataContextModel.Current.LocationBorderTop = screenP.Y > ScreenHeight - 40 ? (screenP.Y - 40)/ ScreenScale : (screenP.Y + 5)/ ScreenScale;
            
            DataContextModel.Current.ShowRGB = ImageHelper.GetRGB((int)screenP.X, (int)screenP.Y);
            if (_IsCapture)
            {
                return;
            }

            if (_IsMouseDown)
            {
                var w = point.X - _X0;
                var h = point.Y - _Y0;
                //logger.LogDebug($"wh:{w},{h}");
                if (w < MinSize || h < MinSize)
                {
                    return;
                }
                DataContextModel.Current.MaskRightWidth = ScreenWidth - point.X;
                DataContextModel.Current.MaskTopWidth = w;
                DataContextModel.Current.MaskBottomHeight = ScreenHeight - point.Y;
                //logger.LogDebug($"DataContextModel:{point.X},{point.Y},{DataContextModel.Current.MaskRightWidth},{DataContextModel.Current.MaskBottomHeight}");
                DataContextModel.Current.ChangeShowSize();
                MainImage.Width = w;
                MainImage.Height = h;
                if (MainImage.Visibility == Visibility.Collapsed)
                {
                    MainImage.Visibility = Visibility.Visible;
                }
            }
            else
            {
                DataContextModel.Current.ShowSizeLeft = point.X;
                DataContextModel.Current.ShowSizeTop = ScreenHeight - point.Y < 40 ? point.Y - 40 : point.Y - 30;

                //DataContextModel.Current.ShowSizeTop = ScreenHeight - point.Y < 30 ? point.Y - 30 : point.Y + 10;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                EventTrackerModel eventTraker = EventTrackerModel.SetEventModel("screenshot.cancel", new JObject { { "triggertype", "esc" } });
                MessageService.PostMessage(TerminalType.CoreServer, "flowedit.tracking", eventTraker.ToString(), _ => { });
                if (m_ManualResetEvent != null)
                {
                    SetSelector("-1");
                    Close();
                }
                else
                {
                    Close();
                }
            }
        }
        #endregion

        private void PART_CLOSE_Click(object sender, RoutedEventArgs e)
        {
            logger.LogDebug("PART_CLOSE_Click");
            EventTrackerModel eventTraker = EventTrackerModel.SetEventModel("screenshot.cancel", new JObject { { "triggertype", "close" } });
            MessageService.PostMessage(TerminalType.CoreServer, "flowedit.tracking", eventTraker.ToString(), _ => { });
            if (m_ManualResetEvent != null)
            {
                SetSelector("-1");
                this.Close();
            }
            else
            {
                Close();
            }
        }
    }
    /// <summary>
    /// wpf线程安全帮助类
    /// </summary>
    public static class WpfHelper
    {
        public static Dispatcher MainDispatcher 
        { 
            get; 
            set; 
        }
        public static void SafeRun(this Action action)
        {
            if (MainDispatcher == null)
            {
                action();
                return;
            }

            if (!MainDispatcher.CheckAccess())
            {
                MainDispatcher.BeginInvoke(action);
                return;
            }
            action();
        }
    }
}
