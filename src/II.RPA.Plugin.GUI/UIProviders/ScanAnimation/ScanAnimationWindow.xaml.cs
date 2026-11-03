using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.ProcessCreator;
using II.RPA.Plugin.GUI.ProcessCreator.Controls;
using System;
using System.Collections.Generic;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace II.RPA.Plugin.GUI.UIProviders.ScanAnimation
{
    /// <summary>
    /// ScanAnimationWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ScanAnimationWindow : Window
    {

        public event EventHandler ScanAnimationCompleted = null;
        double top, left, width, height;
        public ScanAnimationWindow(double aTop, double aLeft, double aWidth, double aHeight)
        {
            top = aTop;
            left = aLeft;
            width = aWidth;
            height = aHeight;
            double factor = TransformToPixels(this).X / 96.0;
            this.Top = left / factor;
            this.Left = top / factor;
            this.Width = width / factor;
            this.Height = height / factor;

            InitializeComponent();
            SourceInitialized += ScanAnimationWindow_SourceInitialized;
           
        }

        private void ScanAnimationWindow_SourceInitialized(object sender, EventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            uint extendedStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, extendedStyle |
            NativeMethods.WS_EX_TRANSPARENT);
            ScanAnimationControl scanAnimationControl = new ScanAnimationControl(ActualHeight, ActualWidth);
            scanAnimationControl.RepeatBehavior = new RepeatBehavior(1);
            scanAnimationControl.StoryboardCompleted += ScanAnimationCompleted;
            scanAnimationControl.IsHitTestVisible = false;
            this.mainPanel.Children.Add(scanAnimationControl);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
           // MessageBox.Show($"{this.Height};{this.Width}");

            //this.Activate();
        }

        public Dpi TransformToPixels(Visual aVisual)
        {
            Matrix matrix;
            var source = PresentationSource.FromVisual(aVisual);
            if (source != null)
            {
                matrix = source.CompositionTarget.TransformToDevice;
            }
            else
            {
                using (var src = new HwndSource(new HwndSourceParameters()))
                {
                    matrix = src.CompositionTarget.TransformToDevice;
                }
            }

            double pixelX = (int)(matrix.M11 * 96.0);
            double pixelY = (int)(matrix.M22 * 96.0);
            return new Dpi(pixelX, pixelY);
        }
    }
}
