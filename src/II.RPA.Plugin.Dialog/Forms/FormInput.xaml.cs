using II.RPA.Plugin.Dialog.Depends;
using II.RPA.Plugin.Dialog.Params;
using II.RPA.Plugin.Dialog.UCControl;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.Dialog
{
    /// <summary>
    /// ConfigSetWnd.xaml 的交互逻辑
    /// </summary>
    public partial class FormInput : Window
    {
        private readonly List<InputModel> InputList;
        public string outJson;
        private readonly AutoOut _autoOutParam;
        private readonly AutoOutSource autoOutSource;
        private static ILogger logger = Logger.OfContext();

        public FormInput(List<InputModel> json, AutoOut autoOutParam, string title = "")
        {
            InitializeComponent();
            WinAutomation.CommonUtility.XamlResource.LanguageResourceHelper.GetInstance().SetAppInstance(this);
            //string defaultTitle = this.FindResource("ConfigSetWnd.xaml_ZiDingYiDuiHuaKuang").ToString();
            InputList = json;
            if (!string.IsNullOrEmpty(title))
            {
                this.TITLE.Text = title;
            }

            _autoOutParam = autoOutParam;

            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight - 7;
            GenerateControl();
            autoOutSource = new AutoOutSource()
            {
                IsEnable = _autoOutParam?.IsEnable == EmOpenAutoOut.Open ? Visibility.Visible : Visibility.Collapsed,
                OutTime = _autoOutParam?.Time ?? 0,
                Tip = GitTip(_autoOutParam),
            };
            if (_autoOutParam != null && _autoOutParam.IsEnable == EmOpenAutoOut.Open)
            {
                StartAutoOut(_autoOutParam);
                this.GotFocus += new RoutedEventHandler(this.Windows_GotFocus);
            }
            else
            {
                this.ContentRendered += FormInput_ContentRendered;
            }
            this.DataContext = autoOutSource;
            this.Topmost = true;
        }

        private void FormInput_ContentRendered(object sender, EventArgs e)
        {
            this.ContentRendered -= FormInput_ContentRendered;

            this.Dispatcher.BeginInvoke(
                DispatcherPriority.Input,
                new Action(() =>
                {
                    this.Activate();

                    IInputFocusControl firstInput = ParameterPanl.Children
                        .OfType<IInputFocusControl>()
                        .FirstOrDefault();

                    firstInput?.FocusInput();
                }));
        }

        private string GitTip(AutoOut autoOut)
        {
            return autoOut?.OutState == EmAutoOutStste.Confirm && !HasEmpty() ? eCommonMessageType.ConfigSetWnd_xaml_cs_JiShiWeiCaoZuoJiangZiDongZhiXing.GetMessage() : eCommonMessageType.ConfigSetWnd_xaml_cs_JiShiWeiCaoZuoJiangZiDongZhiXing1.GetMessage();
        }

        public bool HasEmpty()
        {
            foreach (var children in ParameterPanl.Children)
            {
                if (children is IRequired required)
                {
                    if (required.IsEmpty(false))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void GenerateControl()
        {
            if (InputList != null)
            {
                foreach (var item in InputList)
                {
                    switch (item.Type)
                    {
                        case "TextBox":
                            var uCTextBox = new UCTextBox();
                            uCTextBox.Init(item);
                            ParameterPanl.Children.Add(uCTextBox);
                            break;
                        case "Password":
                            var uCPassword = new UCPasswordLook();
                            uCPassword.Init(item);
                            ParameterPanl.Children.Add(uCPassword);
                            break;
                        default:
                            break;
                    }
                }
            }
        }


        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Ok();
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "输入对话框报错");
            }
        }

        private void Ok()
        {
            foreach (var children in ParameterPanl.Children)
            {
                if (children is IRequired required)
                {
                    if (required.IsEmpty())
                    {
                        return;
                    }
                }
            }
            if (ParameterPanl.Children.Count > 0)
            {
                if (ParameterPanl.Children[0] is IGetInputModelControl imput)
                {
                    InputModel sub = imput.GetInputModel();
                    outJson = sub.Value;
                }
            }
            this.DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void DockPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (WindowState == WindowState.Maximized)
                    SystemCommands.RestoreWindow(this);
                else
                    SystemCommands.MaximizeWindow(this);
            }
            this.DragMove();
        }

        private void Min_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void RestoreWindow_Exec(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.RestoreWindow(this);
        }

        private void MaximizeWindow_Exec(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MaximizeWindow(this);
        }

        private void Windows_GotFocus(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is ScrollViewer)
            {
                e.Handled = true;
                return;
            }

            logger.LogInformation("Windows_GotFocus");
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
            if (autoOut.OutState == EmAutoOutStste.Confirm && !HasEmpty())
            {
                Ok();
            }
            else
            {
                this.Close();
            }
        }
    }
}

