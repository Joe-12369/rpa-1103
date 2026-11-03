using FlaUI.Core.Logging;
using II.RPA.Logging;
using II.RPA.Plugin.GUI.ProcessCreator.Screenshot;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace II.RPA.Plugin.GUI.ProcessCreator
{
    /// <summary>
    /// Window1.xaml 的交互逻辑
    /// </summary>
    public partial class CaptureCursorWindow : Window
    {
        private static readonly Microsoft.Extensions.Logging.ILogger logger = Logging.Logger.OfContext();
        public static double ScreenWidth = SystemParameters.PrimaryScreenWidth;
        public static double ScreenHeight = SystemParameters.PrimaryScreenHeight;
        private static double ScreenScale = 1;
        private readonly Action<System.Windows.Point> m_ActionCallBack;
        public CaptureCursorWindow(Action<System.Windows.Point> actionCallBack = null)
        {
            InitializeComponent();
            m_ActionCallBack = actionCallBack;
            WinAutomation.CommonUtility.XamlResource.LanguageResourceHelper.GetInstance().SetAppInstance(this);
            ScreenScale = ScreenHelper.GetScreenScale();
        }
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
                this.Close();
            };
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            //var context = this.DataContext as CaptureCursorWindowViewModel;
            //var point = e.GetPosition(this);
            var point = e.GetPosition(this);
            var screenPoint = PointToScreen(point);
            xLine.Y1 = point.Y;
            xLine.Y2 = point.Y;
            yLine.X1 = point.X;
            yLine.X2 = point.X;

            MousePointX.Text = screenPoint.X.ToString();
            MousePointY.Text = screenPoint.Y.ToString();
            var Left = screenPoint.X > ScreenWidth - LocationBorder.ActualWidth ? (screenPoint.X - LocationBorder.ActualWidth - 8 ) / ScreenScale : (screenPoint.X + 10) / ScreenScale;
            var Top = screenPoint.Y > ScreenHeight - LocationBorder.ActualHeight ? (screenPoint.Y - LocationBorder.ActualHeight - 8) / ScreenScale : (screenPoint.Y + 10) / ScreenScale;

            LocationBorder.Margin = new Thickness(Left,Top,0,0);
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                var point = e.GetPosition(this);
                var screenPoint = PointToScreen(point);
                logger.LogInformation($"Get Cursor Location：{point}");
                m_ActionCallBack?.Invoke(screenPoint);
                this.DialogResult = true;
                this.Close();
            }
        }
    }
}
