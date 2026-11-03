
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
    public partial class CheckWnd : Window
    {
        public CheckWnd()
        {
            InitializeComponent();
            // 设置全屏
            this.WindowState = System.Windows.WindowState.Normal;
            this.WindowStyle = System.Windows.WindowStyle.None;
            this.ResizeMode = System.Windows.ResizeMode.NoResize;
            this.Topmost = true;

            this.Left = 0.0;
            this.Top = 0.0;
            this.Width = System.Windows.SystemParameters.PrimaryScreenWidth;
            this.Height = System.Windows.SystemParameters.PrimaryScreenHeight;
        }
        public void MoveRects(List<System.Drawing.Rectangle> rectangles)
        {
            foreach (var rects in rectangles)
            {
                System.Windows.Shapes.Rectangle rect = new System.Windows.Shapes.Rectangle();
                rect.Width = rects.Width;
                rect.Height = rects.Height;
                SolidColorBrush myBrush = new SolidColorBrush(Colors.Red);
                rect.Stroke = myBrush;
                rect.StrokeThickness = 1.5;
                Canvas.SetLeft(rect, rects.Left);
                Canvas.SetTop(rect, rects.Top);
                canvas.Children.Add(rect);
            }
        }
    }
}
