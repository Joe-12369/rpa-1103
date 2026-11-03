using OpenQA.Selenium;
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

namespace II.RPA.MobileDeviceManager.Views
{
    /// <summary>
    /// Interaction logic for ModifyWindow.xaml
    /// </summary>
    public partial class ModifyWindow
    {
        public ModifyWindow(string aDeviceName, string anDeviceId)
        {
            InitializeComponent();
            DeviceNameTxt.Text = aDeviceName;
            DeviceIdTxt.Text = anDeviceId;
            Loaded += ModifyWindow_Loaded;
        }

        public string DeviceName
        {
            get => DeviceNameTxt.Text;
        }
        private void ModifyWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void CloseWin_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

    }
}
