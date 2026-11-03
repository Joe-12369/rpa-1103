
using Gma.UserActivityMonitor;
using II.RPA.MobileDeviceManager.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace II.RPA.MobileDeviceManager.Views
{
    /// <summary>
    /// OverlayWnd.xaml 的交互逻辑
    /// </summary>
    public partial class OverlayWnd : Window
    {
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int GWL_EXSTYLE = (-20);

        [DllImport("user32", EntryPoint = "SetWindowLong")]
        private static extern uint SetWindowLong(IntPtr hwnd, int nIndex, uint dwNewLong);

        [DllImport("user32", EntryPoint = "GetWindowLong")]
        private static extern uint GetWindowLong(IntPtr hwnd, int nIndex);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetCursorPos(out POINTt pt);
        private DispatcherTimer HighLightDispatcherTimer = new DispatcherTimer();
        private DispatcherTimer IsActiveDispatcherTimer = new DispatcherTimer();
        private DeviceScreenView mobileForm;
        public delegate MobileElement DegGetMobileElement(System.Windows.Point point);   //外部委托获取屏幕   
        public DegGetMobileElement GetMobileElementHandle;
        public delegate Task<MobileElement> DegClickScreen(System.Windows.Point point, bool isClick);
        public DegClickScreen ClickScreen;
        private double toolHeight = 0;
        private int isStartPoint = 0;
        private bool _selectionCompleted;

        private int _edgeLeft;

        private int _edgeTop;



        private SelectorToolWnd SelectorToolWnd;
        public OverlayWnd(DeviceScreenView _mobileForm)
        {
            InitializeComponent();
            mobileForm = _mobileForm;
            this.Topmost = true;
            this.SourceInitialized += delegate
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                uint extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle |
                WS_EX_TRANSPARENT);
            };
            InstallHook();
        }
        public void ShowSelectTool()
        {
            SelectorToolWnd = new SelectorToolWnd();
            SelectorToolWnd.Owner = this;
            SelectorToolWnd.Left = 0;
            SelectorToolWnd.Top = 0;
            SelectorToolWnd.Show();
        }
        //拾取元素
        public void startGetSelector()
        {
            ShowSelectTool();
            this.Left = -1;
            this.Top = -1;
            this.Width = 0;
            this.Height = 0;
            HighLightDispatcherTimer.Tick -= new EventHandler(DispatcherTimer_Tick);
            HighLightDispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            HighLightDispatcherTimer.Interval = TimeSpan.FromMilliseconds(10);
            HighLightDispatcherTimer.Start();
        }
        //获取坐标
        public void startGetPoint()
        {
            this.Left = -1;
            this.Top = -1;
            this.Width = 0;
            this.Height = 0;
            boder.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#7EA7FF"));
            HighLightDispatcherTimer.Tick -= new EventHandler(DispatcherGetPointTimer_Tick);
            HighLightDispatcherTimer.Tick += new EventHandler(DispatcherGetPointTimer_Tick);
            HighLightDispatcherTimer.Interval = TimeSpan.FromMilliseconds(10);
            HighLightDispatcherTimer.Start();
        }
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
        private void ActiveDispatcherTimer_Tick(object sender, EventArgs e)
        {
            GetCursorPos(out POINTt pt);
            //主窗体左上角相对屏幕的坐标
            System.Windows.Point ptLeftUp = mobileForm.grid.PointToScreen(new System.Windows.Point(0, 0));
            if (pt.X < ptLeftUp.X || pt.X > ptLeftUp.X + mobileForm.grid.Width || pt.Y < ptLeftUp.Y || pt.Y > ptLeftUp.Y + mobileForm.grid.Height)
            {
                this.Width = 0;
                this.Height = 0;
                this.Left = 0;
                this.Top = 0;
            }
            else
            {
                isStartPoint = 0;
            }
        }
        private async void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (MobileDeviceManager.Instance.CvException != null)
            {
                mobileForm.Dispatcher.Invoke(() =>
                {
                    this.Close();
                });
            }
            System.Windows.Point position = new System.Windows.Point(0, 0);
            this.Dispatcher.Invoke(new Action(() =>
            {
                position = mobileForm.grid.PointToScreen(new System.Windows.Point(0.0, 0.0));
                _edgeLeft = (int)(position.X / mobileForm._dpiX);
                _edgeTop = (int)(position.Y / mobileForm._dpiX);

            }));
            GetCursorPos(out POINTt pt);
            pt = new POINTt((int)(pt.X / mobileForm._dpiX), (int)(pt.Y / mobileForm._dpiY));
            //if (pt.X > _edgeLeft && pt.X < _edgeLeft + mobileForm.grid.Width * mobileForm._dpiX && pt.Y > _edgeTop && pt.Y < _edgeTop + mobileForm.grid.Height * mobileForm._dpiY)
            if (pt.X > _edgeLeft && pt.X < _edgeLeft + mobileForm.grid.Width && pt.Y > _edgeTop && pt.Y < _edgeTop + mobileForm.grid.Height)
            {
                position = new System.Windows.Point(pt.X - _edgeLeft, pt.Y - _edgeTop);
                var realPoint = TransformPoint(position);
                SelectorToolWnd.location.Content = $"({(int)realPoint.X},{(int)realPoint.Y})";
                MobileElement element = null;
                await Task.Run(() =>
                {
                    element = MobileDeviceManager.Instance.GetElementFromPoint(new System.Drawing.Point((int)realPoint.X, (int)realPoint.Y));
                });

                if (element != null)
                {
                    this.Width = mobileForm.grid.Width / mobileForm._bitmapImageWidth * element.BoundRectangle.Width;
                    this.Height = mobileForm.grid.Height / mobileForm._bitmapImageHeight * element.BoundRectangle.Height;
                    this.Left = mobileForm.grid.Width / mobileForm._bitmapImageWidth * element.BoundRectangle.X + _edgeLeft;
                    this.Top = mobileForm.grid.Height / mobileForm._bitmapImageHeight * element.BoundRectangle.Y + _edgeTop;
                }
            }

        }
        private System.Windows.Point TransformPoint(System.Windows.Point point)
        {
            System.Windows.Point point1 = new System.Windows.Point();
            //point1.X = Math.Round(point.X / (mobileForm.grid.Width * mobileForm._dpiX) * mobileForm._bitmapImageWidth, 0);
            //point1.Y = Math.Round(point.Y / (mobileForm.grid.Height * mobileForm._dpiY) * mobileForm._bitmapImageHeight, 0);
            point1.X = Math.Round(point.X / (mobileForm.grid.Width) * mobileForm._bitmapImageWidth, 0);
            point1.Y = Math.Round(point.Y / (mobileForm.grid.Height) * mobileForm._bitmapImageHeight, 0);
            return point1;
        }
        DateTime lastClickTime;
        internal void InstallHook()
        {
            HookManager.MouseClickExt += HookManager_MouseClickExt;
            HookManager.KeyDown += HookManager_KeyUp;
        }
        private async void HookManager_MouseClickExt(object sender, MouseEventExtArgs e)
        {
            var span = (DateTime.Now - lastClickTime).TotalMilliseconds;
            lastClickTime = DateTime.Now;
            if (span < 200)
            {
                e.Handled = System.Windows.Forms.Control.ModifierKeys == System.Windows.Forms.Keys.Control || System.Windows.Forms.Control.ModifierKeys == System.Windows.Forms.Keys.Shift;
                return;
            }
            switch (e.Button)
            {
                case System.Windows.Forms.MouseButtons.Left:
                    //cv加载中的时候规避拾取操作
                    if (System.Windows.Forms.Control.ModifierKeys == System.Windows.Forms.Keys.Control
                        && !MobileDeviceManager.Instance.CvLoading)
                    {
                        try
                        {
                            e.Handled = true;
                            boder.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#7EA7FF"));

                            GetCursorPos(out POINTt pt);
                            pt = new POINTt((int)(pt.X / mobileForm._dpiX), (int)(pt.Y / mobileForm._dpiY));
                            //主窗体左上角相对屏幕的坐标
                            System.Windows.Point ptLeftUp = mobileForm.grid.PointToScreen(new System.Windows.Point(0, 0));
                            var point = new System.Windows.Point(pt.X - ptLeftUp.X / mobileForm._dpiX, pt.Y - ptLeftUp.Y / mobileForm._dpiX);
                            point.Y = point.Y;
                            var realPoint = TransformPoint(point);
                            if (MobileElement.OnGetMousePointSelected != null)
                            {
                                MobileElement.OnGetMousePointSelected.Invoke(realPoint);
                                _selectionCompleted = true;
                            }
                            else
                            {
                                MobileDeviceManager.Instance.mobileForm = mobileForm;
                                MobileElement element = await ClickScreen(realPoint, true);
                                MobileElement.OnSelected?.Invoke(element);
                                _selectionCompleted = true;
                                //mobileForm.selectorToolForm.Close();
                            }
                            //mobileForm.positionM.Visibility = Visibility.Collapsed;
                        }
                        catch (Exception ex)
                        {
                            // 回调失败也要结束拾取，不能让异步鼠标钩子抛出未处理异常。
                            MobileDeviceManager.Instance.CaptureException = ex;
                        }
                        finally
                        {
                            e.Handled = true;
                            this.Close();
                        }

                    }
                    else
                    {
                        //boder.Background = System.Windows.Media.Brushes.Transparent;
                        e.Handled = false;
                        return;
                    }
                    e.Handled = true;
                    break;
                default:
                    break;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 关闭覆盖窗（包括断开设备、关闭管理器）时结束等待中的拾取请求。
            if (!_selectionCompleted)
            {
                MobileElement.OnCancelSelected?.Invoke();
            }
            HighLightDispatcherTimer.Tick -= new EventHandler(DispatcherTimer_Tick);
            HighLightDispatcherTimer.Tick -= new EventHandler(DispatcherGetPointTimer_Tick);
            IsActiveDispatcherTimer.Tick -= new EventHandler(ActiveDispatcherTimer_Tick);
            HookManager.KeyDown -= HookManager_KeyUp;
            HookManager.MouseClickExt -= HookManager_MouseClickExt;
        }

        private void DispatcherGetPointTimer_Tick(object sender, EventArgs e)
        {
            System.Windows.Point position = new System.Windows.Point(0, 0);
            this.Dispatcher.Invoke(new Action(() =>
            {
                position = mobileForm.grid.PointToScreen(new System.Windows.Point(0.0, 0.0));
                _edgeLeft = (int)(position.X / mobileForm._dpiX);
                _edgeTop = (int)(position.Y / mobileForm._dpiX);

            }));
            GetCursorPos(out POINTt pt);
            pt = new POINTt((int)(pt.X / mobileForm._dpiX), (int)(pt.Y / mobileForm._dpiY));
            //if (pt.X > _edgeLeft && pt.X < _edgeLeft + mobileForm.grid.Width * mobileForm._dpiX && pt.Y > _edgeTop && pt.Y < _edgeTop + mobileForm.grid.Height * mobileForm._dpiY)
            if (pt.X > _edgeLeft && pt.X < _edgeLeft + mobileForm.grid.Width && pt.Y > _edgeTop && pt.Y < _edgeTop + mobileForm.grid.Height)
            {
                position = new System.Windows.Point(pt.X - _edgeLeft, pt.Y - _edgeTop);
                var realPoint = TransformPoint(position);
                this.Width = (double)mobileForm.grid.Width;
                this.Height = (double)mobileForm.grid.Height;
                this.Left = _edgeLeft;
                this.Top = _edgeTop;
            }
        }
        private void HookManager_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.Escape)
            {
                mobileForm.Dispatcher.Invoke(() =>
                {
                    this.Close();
                });
            }
        }

    }
}
