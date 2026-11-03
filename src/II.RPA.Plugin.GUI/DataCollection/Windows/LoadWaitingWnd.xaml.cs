using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
using WpfAnimatedGif;

namespace II.RPA.Plugin.GUI.DataCollection.Windows
{
    /// <summary>
    /// LoadWaitingWnd.xaml 的交互逻辑
    /// </summary>
    public partial class LoadWaitingWnd : Window
    {
        public LoadWaitingWnd()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!this.CheckAccess())
            {
                this.Dispatcher.Invoke(() =>
                {
                    ShowLoading();
                });
                return;
            }
            else
            {
                ShowLoading();
            }
        }

        private void ShowLoading()
        {
            Assembly Asmb = Assembly.GetExecutingAssembly();
            Stream ManifestStream = Asmb.GetManifestResourceStream("II.RPA.Plugin.GUI.DataCollection.Windows.DualBall.gif");
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = ManifestStream;
                image.EndInit();
                ImageBehavior.SetAnimatedSource(im_RunLog, image);
            }
        }



        private void gifMedia_MediaEnded(object sender, RoutedEventArgs e)
        {
            //(sender as MediaElement).Stop();
            //(sender as MediaElement).Position = new TimeSpan(0, 0, 1);
            //(sender as MediaElement).Play();
        }

    }
}
