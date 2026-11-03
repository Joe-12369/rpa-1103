using System;
using System.Collections.Generic;
using System.ComponentModel;
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

    public partial class IIProgressBar : UserControl
    {
       
        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(IIProgressBar),
                new PropertyMetadata(0.0, new PropertyChangedCallback(OnPropetyChanged)));

        public Brush BackColor
        {
            get { return (Brush)GetValue(BackColorProperty); }
            set { SetValue(BackColorProperty, value); }
        }

        public static readonly DependencyProperty BackColorProperty =
            DependencyProperty.Register("BackColor", typeof(Brush), typeof(IIProgressBar), new PropertyMetadata(default(Brush)));


        public Brush ForeColor
        {
            get { return (Brush)GetValue(ForeColorProperty); }
            set { SetValue(ForeColorProperty, value); }
        }

        public static readonly DependencyProperty ForeColorProperty =
            DependencyProperty.Register("ForeColor", typeof(Brush), typeof(IIProgressBar), new PropertyMetadata(default(Brush)));


        public string Header
        {
            get { return (string)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register("Header", typeof(string), typeof(IIProgressBar), new PropertyMetadata(default(string)));


        public static void OnPropetyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var obj = d as IIProgressBar;
            obj.UpdateValue();
        }

        private void UpdateValue()
        {
            this.layout.Width = Math.Min(this.RenderSize.Width, this.RenderSize.Height);
            this.layout.Height = Math.Min(this.RenderSize.Width, this.RenderSize.Height);
            double radius = Math.Min(this.RenderSize.Width, this.RenderSize.Height) / 2;
            if (radius <= 0) return;

            double newX = 0.0;
            double newY = 0.0;
            newX = radius + (radius - 3) * Math.Cos((this.Value % 100.0 * 100 * 3.6 - 90) * Math.PI / 180);
            newY = radius + (radius - 3) * Math.Sin((this.Value % 100.0 * 100 * 3.6 - 90) * Math.PI / 180);
            string pathDataStr = "M{0} 3A{3} {3} 0 {4} 1 {1} {2}";
            pathDataStr = string.Format(pathDataStr, radius + 0.01, newX, newY, radius - 3, this.Value < 0.5 ? 0 : 1);
            var converter = TypeDescriptor.GetConverter(typeof(Geometry));
            this.path.Data = (Geometry)converter.ConvertFrom(pathDataStr);
            this.percentText.Text = Math.Round(this.Value * 100, 0).ToString();
        }
        public IIProgressBar()
        {
            InitializeComponent();
            this.SizeChanged += CircularProgressBar_SizeChanged;
        }

        private void CircularProgressBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.UpdateValue();
        }
    }
}
