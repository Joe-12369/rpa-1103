using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace II.RPA.Plugin.GUI.DataCollection.Windows
{
    /// <summary>
    /// ErrorMessageWnd.xaml 的交互逻辑
    /// </summary>
    public partial class ErrorMessageWnd : Window
    {
        public ErrorMessageWnd()
        {
            InitializeComponent();
            InitDispatcherTimer();
            //m_strMessage = _Message;
        }

        System.Timers.Timer CTimer = null;
        private static ILogger logger = Logger.OfContext();

        public void InitDispatcherTimer()
        {
            CTimer = new System.Timers.Timer();
            CTimer.Interval = 5000;
            CTimer.AutoReset = false;
            CTimer.Elapsed += Timer_Tick;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //StartTimer();
        }

        public void ResetErrorMessage(string _ErrorMessage)
        {
            if (this.CheckAccess())
            {
                try
                {
                    tb_ErrorMessage.Text = _ErrorMessage;
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "ResetErrorMessage");
                }
            }
            else
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)delegate ()
                {
                    try
                    {
                        tb_ErrorMessage.Text = _ErrorMessage;
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "ResetErrorMessage");
                    }
                });
            }
            RefreshTimer();
        }

        private void StartTimer()
        {
            try
            {
                if (CTimer == null)
                {
                    InitDispatcherTimer();
                }

                CTimer.Start();
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "StartTimer");
            }
        }

        private void RefreshTimer()
        {
            try
            {
                if (CTimer != null)
                {
                    CTimer.Stop();
                    CTimer.Start();
                }
                else
                {
                    StartTimer();
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "RefreshTimer exception");
            }
        }



        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (this.CheckAccess())
                {
                    try
                    {
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "Timer_Tick22 exception");
                    }
                }
                else
                {
                    Dispatcher.Invoke(DispatcherPriority.Normal, (Action)delegate ()
                    {
                        try
                        {
                            this.Close();
                        }
                        catch (Exception ex)
                        {
                            Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"Timer_Tick22 exception");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "Timer_Tick exception");
            }
        }
    }
}

