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

namespace II.RPA.MobileDeviceManager.Views
{
    /// <summary>
    /// DeviceListBoxItem.xaml 的交互逻辑
    /// </summary>
    public partial class DeviceListBoxItem : UserControl
    {
        public DeviceListBoxItem()
        {
            InitializeComponent();
        }

        public bool IsConnectUsb
        {
            get { return (bool)GetValue(IsConnectUsbProperty); }
            set
            {
                SetValue(IsConnectUsbProperty, value);
            }
        }

        public static readonly DependencyProperty IsConnectUsbProperty =
            DependencyProperty.Register("IsConnectUsb", typeof(bool), typeof(DeviceListBoxItem), new PropertyMetadata(false));

        #region  Connect
        public static readonly DependencyProperty ConnectCommandProperty = DependencyProperty.Register("ConnectCommand", typeof(ICommand), typeof(DeviceListBoxItem));
        public static readonly DependencyProperty ConnectCommandParameterProperty = DependencyProperty.Register("ConnectCommandParameter", typeof(object), typeof(DeviceListBoxItem),
            new PropertyMetadata(null));

        public ICommand ConnectCommand
        {
            get => (ICommand)GetValue(ConnectCommandProperty);
            set => SetValue(ConnectCommandProperty, value);
        }

        public object ConnectCommandParameter
        {
            get => GetValue(ConnectCommandParameterProperty);
            set => SetValue(ConnectCommandParameterProperty, value);
        }
        #endregion

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            ConnectCommand?.Execute(ConnectCommandParameter);
        }
    }
}
