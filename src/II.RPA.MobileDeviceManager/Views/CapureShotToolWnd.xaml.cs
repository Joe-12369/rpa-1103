using Gma.UserActivityMonitor;
using II.RPA.MobileDeviceManager.Extentions;
using II.RPA.MobileDeviceManager.ScreenShot;
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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace II.RPA.MobileDeviceManager.Views
{
    /// <summary>
    /// SelectorToolWnd.xaml 的交互逻辑
    /// </summary>
    public partial class CapureShotToolWnd : Window
    {
        private readonly ScreenShoter _screenshot;
        private DispatcherTimer freshPositionTimer = new DispatcherTimer();
        public CapureShotToolWnd(ScreenShoter screenShoter)
        {
            InitializeComponent();
            WinAutomation.CommonUtility.XamlResource.LanguageResourceHelper.GetInstance().SetAppInstance(this);
            _screenshot = screenShoter;
            this.ShowInTaskbar = false;
            this.Left = 0;
            this.Top = 0;
            this.Topmost = true;
            HookManager.KeyDown += HookManager_KeyUp;
            freshPositionTimer.Tick -= new EventHandler(DispatcherTimer_Tick);
            freshPositionTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            freshPositionTimer.Interval = TimeSpan.FromMilliseconds(20);
            freshPositionTimer.Start();
            MobileDeviceManager.Instance.Semaphore.WaitOne();
            MobileDeviceManager.Instance.CancelException = null;
        }
        private void HookManager_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            //以下两个快捷键触发后不需要再次监听快捷键事件了
            if (e.KeyCode == System.Windows.Forms.Keys.LControlKey)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    //双重激活、置顶窗口
                    bool originTopStatus = Application.Current.MainWindow.Topmost;
                    Application.Current.MainWindow.WindowState = WindowState.Normal;
                    Application.Current.MainWindow.Topmost = true;
                    Application.Current.MainWindow.Activate();
                    Application.Current.MainWindow.Topmost = originTopStatus;
                });
                MobileDeviceManager.Instance.Semaphore.SafeRelease();
                HookManager.KeyDown -= HookManager_KeyUp;
                freshPositionTimer.Tick -= new EventHandler(DispatcherTimer_Tick);
            }
            if (e.KeyCode == System.Windows.Forms.Keys.Escape)
            {
                MobileDeviceManager.Instance.CancelException = new MobileException();
                HookManager.KeyDown -= HookManager_KeyUp;
                freshPositionTimer.Tick -= new EventHandler(DispatcherTimer_Tick);
                MobileDeviceManager.Instance.Semaphore.SafeRelease();
                _screenshot.Stop();
            }
        }
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            this.location.Content = $"({MobileDeviceManager.Instance.CurrentPositionX},{MobileDeviceManager.Instance.CurrentPositionY})";
        }
    }
}
