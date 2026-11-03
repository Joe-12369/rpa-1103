using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace II.RPA.MobileDeviceManager.Controls
{
    /// <summary>
    /// Interaction logic for ProgressRing.xaml
    /// </summary>
    public partial class ProgressRing : UserControl
    {
        public ProgressRing()
        {
            InitializeComponent();
        }

        public double Progress
        {
            get => (double)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
            nameof(Progress),
            typeof(double),
            typeof(ProgressRing),
            new PropertyMetadata(50d, PropertyChangedCallback));
        protected static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressRing control)
            {
                control.UpdateProgressAngle();
            }
            else
            {
                return;
            }
        }

        protected void UpdateProgressAngle()
        {
            var percentage = Progress * 100;

            if (percentage > 100)
                percentage = 100;

            if (percentage < 0)
                percentage = 0;

            // (360 / 100) * percentage
            var endAngle = 3.6d * percentage + 90;

            //if (endAngle >= 360)
            //    endAngle = 359;

            ProgressArc.EndAngle = endAngle;
            ProgressValue.Text = ((int)percentage).ToString();
        }
        protected void UpdateEndEllipse()
        {
            var percentage = Progress;

            if (percentage > 100)
                percentage = 100;

            if (percentage < 0)
                percentage = 0;

            // (360 / 100) * percentage
            var endAngle = 3.6d * percentage;
            var angle = DegreesToRadians(endAngle);
            double y = Math.Cos(angle) * 95;
            double x = Math.Sin(angle + 180) * -95;
            //EndEllipse.Margin = new Thickness(x, y, 0, 0);
        }
        static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}
