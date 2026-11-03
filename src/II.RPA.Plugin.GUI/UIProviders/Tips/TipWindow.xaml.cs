using II.RPA.Plugin.GUI.UIProviders.Controls;
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
using System.Windows.Shapes;

namespace II.RPA.Plugin.GUI.UIProviders.Tips
{
    /// <summary>
    /// TipWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TipWindow : Window
    {
        private IconControl PART_Icon;
        private TextBlock PART_Content;
        public TipWindow()
        {
            InitializeComponent();
            PART_Content = new TextBlock();
            PART_Icon = new IconControl();
            Loaded += TipWindow_Loaded;
        }

        private void TipWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Top = 60;
        }

        public TipWindow(string aTitle, string aContent, TipLevel aTipLevel):this()
        {
            PART_Content.Text = aContent;
            switch (aTipLevel)
            {
                case TipLevel.DONE:
                    //PART_Icon = new Done();
                    PART_Icon.IconType = IconType.DONE;
                    PART_Border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00B856"));
                    break;
                case TipLevel.PROCEED:
                    //PART_Icon = new LoadingRing();
                    PART_Icon.IconType = IconType.PROCEED;
                    PART_Border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D9DDE6"));
                    break;
                case TipLevel.ERROR:
                    //PART_Icon = new LoadingRing();
                    PART_Icon.IconType = IconType.ERROR;
                    PART_Border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6151"));
                    PART_Border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEFED"));
                    break;
                default:
                    //PART_Icon = new Done();
                    PART_Icon.IconType = IconType.DEFAULT;
                    PART_Border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D9DDE6"));
                    break;

            }
            PART_Icon.VerticalAlignment = VerticalAlignment.Center;
            PART_Content.VerticalAlignment = VerticalAlignment.Center;
            PART_Icon.Height = 25;
            PART_Icon.Width = 25;
            PART_Content.Margin = new Thickness(10, 0, 0, 0);
            PART_Icon.Margin = new Thickness(22, 0, 0, 0);
            PART_Content.FontSize= 14;
            PART_Content.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#161923"));
            PART_MainPanel.Children.Add(PART_Icon);
            PART_MainPanel.Children.Add(PART_Content);
            //PART_MainGrid.Children.Add(PART_Icon);
            //Grid.SetColumn(PART_Icon, 0);
            //this.Show();
        }
        //public void Show(string aTitle,string aContent,TipLevel aTipLevel)
        //{
        //    PART_Content.Text = aContent;
        //    switch (aTipLevel)
        //    {
        //        case TipLevel.DONE:
        //            PART_Icon.Template = (ControlTemplate)this.TryFindResource("DoneTemplate");
        //            break;
        //        case TipLevel.INFO:
        //            PART_Icon.Template = (ControlTemplate)this.TryFindResource("ProgressTemplate");
        //            break;
        //        default:
        //            PART_Icon.Template = (ControlTemplate)this.TryFindResource("DoneTemplate");
        //            break;

        //    }
        //    //PART_MainGrid.Children.Add(PART_Icon);
        //    //Grid.SetColumn(PART_Icon, 0);
        //    this.Show();
        //}
    }
}
