using II.RPA.Plugin.Dialog.Depends;
using II.RPA.Plugin.Dialog.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.WinAutomation.CommonUtility.XamlResource;

namespace II.RPA.Plugin.Dialog
{
    /// <summary>
    /// ConfigSetWnd.xaml 的交互逻辑
    /// </summary>
    public partial class FormInfo : Window
    {
        public string outJson;
        private readonly AutoOut _autoOutParam;
        private AutoOutSource autoOutSource;
        private static ILogger logger = Logger.OfContext();

        public FormInfo(ParamInfoDialogModel paramModel)
        {
            InitializeComponent();
            LanguageResourceHelper.GetInstance().SetAppInstance(this);
            if (!string.IsNullOrEmpty(paramModel.Title))
            {
                this.TITLE.Text = paramModel.Title;
            }
            _autoOutParam = paramModel.AutoOut;
            autoOutSource = new AutoOutSource()
            {
                IsEnable = _autoOutParam?.IsEnable == EmOpenAutoOut.Open ? Visibility.Visible : Visibility.Collapsed,
                OutTime = _autoOutParam?.Time ?? 0,
                Tip = bulidTip(_autoOutParam),
            };
            if (_autoOutParam != null && _autoOutParam.IsEnable == EmOpenAutoOut.Open)
            {
                StartAutoOut(_autoOutParam);
                this.MouseDown += new MouseButtonEventHandler(Windows_Click);
            }
            this.DataContext = autoOutSource;
            SpButtons.DataContext = ShowButton(paramModel.InfoResultType);
            logo.Content = paramModel.InfoType;
            tbContext.Text = paramModel.Context;
            this.Topmost = true;
        }

        private InfoButtonSource ShowButton(EmInfoResultType emInfoResultType)
        {
            InfoButtonSource infoButtonSource = new InfoButtonSource
            {
                IsEnableConfirm = emInfoResultType.HasFlag(EmInfoResultType.Confirm) ? Visibility.Visible : Visibility.Collapsed,
                IsEnableCancel = emInfoResultType.HasFlag(EmInfoResultType.Cancel) ? Visibility.Visible : Visibility.Collapsed,
                IsEnableRetry = emInfoResultType.HasFlag(EmInfoResultType.Retry) ? Visibility.Visible : Visibility.Collapsed,
                IsEnableAbort = emInfoResultType.HasFlag(EmInfoResultType.Abort) ? Visibility.Visible : Visibility.Collapsed
            };
            return infoButtonSource;
        }

        private string bulidTip(AutoOut autoOut)
        {
            string state = eCommonMessageType.ConfigSetWnd_xaml_QueRen.GetMessage();// "确认";
            if (autoOut != null)
            {
                switch (autoOut.OutState)
                {
                    case EmAutoOutStste.Cancel:
                        state = eCommonMessageType.XMessageBox_xaml_QuXiao.GetMessage(); //"取消";
                        break;
                    case EmAutoOutStste.Retry:
                        state = eCommonMessageType.FormInfo_xaml_ZhongShi.GetMessage(); //"重试";
                        break;
                    case EmAutoOutStste.Abort:
                        state = eCommonMessageType.FormInfo_xaml_ZhongZhi.GetMessage(); //"中止";
                        break;
                }
            }
            return string.Format(eCommonMessageType.FormInfo_xaml_cs_JiShiWeiCaoZuoJiangZiDongZhiXing.GetMessage(), state); // $"计时未操作将自动执行【{state}按钮】，倒计时 "; 不确定是否正常
        }


        private void DockPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Windows_Click(object sender, RoutedEventArgs e)
        {
            logger.LogInformation("Windows_Click");
            CloseAutoOut();
        }

        DispatcherTimer timerQuit = new DispatcherTimer();
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (autoOutSource.OutTime > 0)
            {
                autoOutSource.OutTime--;
            }
            else
            {
                AutoOut(_autoOutParam);
            }
        }

        /// <summary>
        /// 启动自动退出
        /// </summary>
        private void StartAutoOut(AutoOut autoOut)
        {
            autoOutSource.IsEnable = Visibility.Visible;
            autoOutSource.OutTime = autoOut.Time;
            timerQuit.Interval = TimeSpan.FromMilliseconds(1000);
            timerQuit.Tick += new EventHandler(dispatcherTimer_Tick);
            timerQuit.Start();
        }
        /// <summary>
        /// 关闭自动退出
        /// </summary>
        private void CloseAutoOut()
        {
            timerQuit.Tick -= new EventHandler(dispatcherTimer_Tick);
            timerQuit.Stop();
            autoOutSource.IsEnable = Visibility.Collapsed;
        }

        /// <summary>
        /// 自动退出
        /// </summary>
        private void AutoOut(AutoOut autoOut)
        {
            Back(autoOut.OutState);
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            Back(EmAutoOutStste.Confirm);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Back(EmAutoOutStste.Cancel);
        }
        private void btnRetry_Click(object sender, RoutedEventArgs e)
        {
            Back(EmAutoOutStste.Retry);
        }


        private void btnAbort_Click(object sender, RoutedEventArgs e)
        {
            Back(EmAutoOutStste.Abort);
        }

        private void Back(EmAutoOutStste outStste)
        {
            timerQuit.Tick -= new EventHandler(dispatcherTimer_Tick);
            timerQuit.Stop();
            outJson = outStste.GetHashCode().ToString();
            this.DialogResult = true;
        }
    }
}
