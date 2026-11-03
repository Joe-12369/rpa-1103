using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// DesktopToolDialog.xaml 的交互逻辑
    /// </summary>
    public partial class DesktopToolDialog : Window
    {
        public DesktopToolDialog(string aProcessName)
        {
            InitializeComponent();
            tipText.Text = aProcessName;    
        }
        public bool IsShowing { get; set; } = false;
        public bool IsIgnoreNext { get; private set; } = false;
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TFButton_Click(object sender, RoutedEventArgs e)
        {
            IsIgnoreNext = true;
            this.Close();
        }

        private void TFButton_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
            try
            {
                Process.Start(@"C:\WINDOWS\system32\taskmgr.exe");
            }
            catch
            {
                
            }
        }

        private void Grid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {

        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    if (WindowState == WindowState.Normal)
                    {
                        WindowState = WindowState.Maximized;
                    }
                    else
                    {
                        WindowState = WindowState.Normal;
                    }
                }
                else
                {
                    DragMove();
                }
            }
        }
    }
}
