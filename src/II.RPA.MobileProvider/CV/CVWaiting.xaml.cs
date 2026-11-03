using II.RPA.WinAutomation.CommonUtility;
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
using System.Windows.Threading;

namespace II.RPA.MobileProvider.CV
{
    /// <summary>
    /// WaitingWnd.xaml 的交互逻辑
    /// </summary>
    public partial class CVWaiting : Window
    {

        public CVWaiting()
        {
            InitializeComponent();
            this.Topmost = true;
            this.WindowStyle = WindowStyle.None;
            txt.Content = eCommonMessageType.WaitingWnd_xaml_cs_ZhiNengZhangYuZhengZaiShiBieZhong.GetMessage();
        }
    }
}
