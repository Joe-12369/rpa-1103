using II.RPA.Logging;
using Microsoft.Extensions.Logging;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace II.RPA.Plugin.GUI.ProcessCreator.Controls
{
    /// <summary>
    /// Interaction logic for ScanAnimationControl.xaml
    /// </summary>
    public partial class ScanAnimationControl : UserControl
    {
        private static ILogger logger = Logger.OfContext();

        public event EventHandler StoryboardCompleted = null;
        public RepeatBehavior RepeatBehavior { get; set; } = RepeatBehavior.Forever;
        Storyboard story = new Storyboard();
        double height;
        double width;
        public ScanAnimationControl(double aHeight, double aWidth)
        {
            InitializeComponent();
            height = aHeight;
            width = aWidth;
            Loaded += ScanAnimationControl_Loaded;
            Unloaded += ScanAnimationControl_Unloaded;
        }
        private void ScanAnimationControl_Unloaded(object sender, RoutedEventArgs e)
        {
            story?.Stop();
        }

        private void ScanAnimationControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.lineScan.RenderTransform = new TranslateTransform();
            //lineScan.X1 = 0;
            //lineScan.X2 = width;
            lineScan.Width = width;
            lineScan.Height = height;
            this.lineScan.Name = "button1";
            this.Name = "window1";
            NameScope.SetNameScope(this, new NameScope());
            this.RegisterName(this.lineScan.Name, this.lineScan);

            DoubleAnimation xAnimation = new DoubleAnimation
            {
                From = 0 - height,
                To = 0,
                Duration = new Duration(TimeSpan.FromSeconds(1.5))
            };

            DependencyProperty[] propertyChain = new DependencyProperty[]
            {
                Line.RenderTransformProperty,
                TranslateTransform.YProperty
            };
            story.AutoReverse = false;
            story.RepeatBehavior = this.RepeatBehavior;
            story.Children.Add(xAnimation);

            Storyboard.SetTargetName(xAnimation, this.lineScan.Name);
            Storyboard.SetTargetProperty(xAnimation, new PropertyPath("(0).(1)", propertyChain));
            story.Completed += (a, b) =>
            {
                logger.LogDebug($"Storyboard Completed Event");
            };
            if (StoryboardCompleted != null)
                story.Completed += StoryboardCompleted;
            story.Begin(this);
        }
    }
}
