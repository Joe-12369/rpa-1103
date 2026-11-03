using II.RPA.MobileDeviceManager.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace II.RPA.MobileDeviceManager.Common
{
    internal class MsgTipsHelper
    {
        public static void ShowWarningPageInMainFrame(string showContent, Window window)
        {
            double[] d = new double[2];
            window.Dispatcher.Invoke(async () =>
            {
                d[0] = window.Top + 40;
                d[1] = window.Left + window.Width / 2;
                WarningMsgWnd warningMsgWnd = new WarningMsgWnd(showContent);
                warningMsgWnd.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;
                warningMsgWnd.Top = d[0] - warningMsgWnd.Height / 2;
                warningMsgWnd.Left = d[1] - warningMsgWnd.Width / 2;
                warningMsgWnd.ShowInTaskbar = false;
                warningMsgWnd.Show();
                warningMsgWnd.Topmost = true;
                await Task.Delay(2000);
                warningMsgWnd.Close();
            });
        }
    }
}
