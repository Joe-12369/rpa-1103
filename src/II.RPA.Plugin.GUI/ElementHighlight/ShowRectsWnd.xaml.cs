using II.RPA.Logging;
using II.RPA.Plugin.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static II.RPA.Plugin.GUI.ElementHighlight.ShowRectsWnd;

namespace II.RPA.Plugin.GUI.ElementHighlight
{
    /// <summary>
    /// ShowRectsWnd.xaml 的交互逻辑
    /// </summary>
    public partial class ShowRectsWnd : Window
    {
        double CurrentScaleX;//当前DPI缩放
        double CurrentScaleY;//垂直方向上dpi缩放

        private static ILogger logger = Logger.OfContext();

        public ShowRectsWnd()
        {
            InitializeComponent();
            //this.SourceInitialized += ShowRectsWnd_SourceInitialized;
        }

        private void ShowRectsWnd_SourceInitialized(object sender, EventArgs e)
        {
            //this.Left = 0;
            //this.Top = 0;
            //this.Width = SystemParameters.PrimaryScreenWidth;
            //this.Height = SystemParameters.PrimaryScreenHeight;
            //CurrentScaleX = (double)NativeMethods.DynamiDpiScale;
            //CurrentScaleY = (double)NativeMethods.DynamiDpiScale;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            SetWindowTransparent(true);
        }


        /// <summary>
        /// 设置窗口穿透
        /// </summary>
        /// <param name="aTransparent"></param>
        public void SetWindowTransparent(bool aTransparent)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            uint extendedStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            if (aTransparent)
            {
                //logger.LogDebug("穿透效果");
                this.Background = System.Windows.Media.Brushes.Transparent;
                NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, extendedStyle | NativeMethods.WS_EX_TRANSPARENT);
                ///SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
            }
            else
            {
                //logger.LogDebug("非穿透效果");
                this.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(100, 0, 0, 139));
                NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, extendedStyle & ~NativeMethods.WS_EX_TRANSPARENT);
            }
        }

        #region 显示高亮框及闪烁



        public void ShowRectsAndFlash()
        {
            if (m_Rects?.Count < 1)
                return;
            if (m_HighLighterFlashParam?.FlashTimes < 1)
                return;

            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(500);
                var _BorderColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(m_HighLighterFlashParam.BorderBrushCode);
                CreateRects(m_Rects, _BorderColor);
                Flash(m_HighLighterFlashParam.FlashInterval, m_HighLighterFlashParam.FlashTimes);
            });
        }

        List<Rectangle> m_Rects = new List<Rectangle>();
        HighLighterFlashParam m_HighLighterFlashParam = new HighLighterFlashParam();
        public void SetParamRectsAndFlash(List<Rectangle> _Rects, HighLighterFlashParam _HighLighterFlashParam)
        {
            m_Rects = _Rects;
            m_HighLighterFlashParam = _HighLighterFlashParam;
        }


        public Rectangle CreateRect(int _X, int _Y, int _Width, int _Height)
        {
            return new Rectangle(new System.Drawing.Point(_X, _Y), new System.Drawing.Size(_Width, _Height));
        }

        public void CreateRects(List<Rectangle> _Rectangles, System.Windows.Media.Color _BorderColor)
        {
            foreach (var item in _Rectangles)
            {
                logger.LogDebug($"实际调用高亮框{item.ToString()}");
                CreateRect(item, _BorderColor);
            }
        }

        public void Flash(int _Period, int _FlashCount)
        {
            Task.Factory.StartNew(() =>
            {
                ShowFlash(_Period, _FlashCount);
            });


        }

        public void ShowFlash(int _Period, int _FlashCount)
        {
            for (int i = 0; i < _FlashCount; i++)
            {
                ShowChildren(true);
                //this.Visibility = Visibility.Visible;
                Thread.Sleep(_Period);
                ShowChildren(false);
                //this.Visibility = Visibility.Hidden;
                Thread.Sleep(_Period);
            }
            SetWndClose();
        }

        public void SetWndClose()
        {
            Dispatcher.BeginInvoke((Action)delegate ()
            {
                this.Close();
            }
            );
        }

        public void ShowChildren(bool IsShow)
        {
            Dispatcher.BeginInvoke((Action)delegate ()
            {
                if (IsShow)
                {
                    foreach (var item in MainPanel.Children)
                    {
                        ((Border)item).Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    foreach (var item in MainPanel.Children)
                    {
                        ((Border)item).Visibility = Visibility.Hidden;
                    }
                }
            }
            );

        }

        public void CreateRect(Rectangle _Rectangle, System.Windows.Media.Color _BorderBrush)
        {
            this.Dispatcher.Invoke(delegate ()
            {
                DockPanel _innerPane = new DockPanel
                {
                    Background = System.Windows.Media.Brushes.Transparent
                };

                SolidColorBrush _InnerBorderBrush = new SolidColorBrush(_BorderBrush);
                Border _InnerBorder = new Border
                {
                    BorderThickness = new Thickness(3.0),
                    BorderBrush = _InnerBorderBrush,
                    CornerRadius = new CornerRadius(2.0),
                    Child = _innerPane
                };

                Border _OutBorder = new Border
                {
                    BorderThickness = new Thickness(1.0),
                    BorderBrush = System.Windows.Media.Brushes.White,
                    CornerRadius = new CornerRadius(4.0),
                    Child = _InnerBorder
                };

                double width = _Rectangle.Width / CurrentScaleX;
                double height = _Rectangle.Height / CurrentScaleY;
                bool useTightBorder = m_HighLighterFlashParam?.UseTightBorder == true;
                if (useTightBorder)
                {
                    _OutBorder.Width = Math.Max(1.0, width);
                    _OutBorder.Height = Math.Max(1.0, height);
                }
                else
                {
                    _innerPane.Width = width;
                    _innerPane.Height = height;
                }
                _OutBorder.Visibility = Visibility.Hidden;

                logger.LogDebug($"实际缩放 CurrentScaleY{CurrentScaleY} CurrentScaleX{CurrentScaleX}");
                MainPanel.Children.Add(_OutBorder);
                _OutBorder.SetValue(Canvas.TopProperty, (_Rectangle.Y / CurrentScaleY) - (useTightBorder ? 0 : 4));
                _OutBorder.SetValue(Canvas.LeftProperty, (_Rectangle.X / CurrentScaleX) - (useTightBorder ? 0 : 4));
                //RegenerateRect(_Rectangle, CurrentScaleX, CurrentScaleY);
            });
        }

        public Rectangle RegenerateRect(Rectangle CRectangle, double XDpiDelta, double YDPIDelta)
        {
            Rectangle cRectangle = new Rectangle();
            cRectangle.X = (int)((double)CRectangle.X / XDpiDelta);
            cRectangle.Y = (int)((double)CRectangle.Y / YDPIDelta);
            cRectangle.Width = (int)((double)CRectangle.Width / XDpiDelta);
            cRectangle.Height = (int)((double)CRectangle.Height / YDPIDelta);
            logger.LogDebug($"最终高亮 {cRectangle.ToString()}");
            return cRectangle;
        }

        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var Matrix = DpiHelper.GetScaleFromVisual(this);
            CurrentScaleX = Matrix.M11;
            CurrentScaleY = Matrix.M22;
            InitialWindow();
            ShowRectsAndFlash();
        }


        private void InitialWindow()
        {
            this.Left = 0;
            this.Top = 0;
            var deltaX = ((NativeMethods.DpiX / 96.0) / CurrentScaleX);
            var deltaY = ((NativeMethods.DpiY / 96.0) / CurrentScaleY);
            logger.LogDebug($"deltaX{deltaX} deltaY{deltaY}");
            this.Width = SystemParameters.PrimaryScreenWidth * deltaX;
            this.Height = SystemParameters.PrimaryScreenHeight * deltaY;
        }
    }
}
