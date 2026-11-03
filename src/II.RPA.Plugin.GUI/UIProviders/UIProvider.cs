using II.RPA.Plugin.GUI.UIProviders.ScanAnimation;
using II.RPA.Plugin.GUI.UIProviders.Tips;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace II.RPA.Plugin.Core.UI
{
    public class UIProvider
    {
        private readonly Thread m_UIThread;
        private Dispatcher m_UIDispatcher;
        private readonly ManualResetEventSlim m_StartedEvent = new ManualResetEventSlim(false);
        private DispatcherTimer m_Timer;

        private TipWindow m_TisWindow;
        private ScanAnimationWindow m_ScanAnimationWindow = null;
        private DesktopToolDialog m_DesktopToolDialog;
        public UIProvider()
        {
            m_UIThread = new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                m_UIDispatcher = Dispatcher.CurrentDispatcher;
                m_StartedEvent.Set();
                Dispatcher.Run();
            });
            m_UIThread.SetApartmentState(ApartmentState.STA);
            m_UIThread.IsBackground = true;
            m_UIThread.Start();
            m_StartedEvent.Wait();
        }
        public Dispatcher UIDispatcher => m_UIDispatcher;
        public void ShowTips(string aTitle, string aContent, TipLevel aLevel)
        {
            m_UIDispatcher.Invoke(new Action(() =>
            {
                if (m_TisWindow == null)
                {
                    m_TisWindow = new TipWindow(aTitle, aContent, aLevel);
                    //m_TisWindow.Show(aTitle, aContent, aLevel);
                    m_TisWindow.Show();
                    m_TisWindow.Top = 60;
                    StartCloseTimer(TimeSpan.FromSeconds(3));
                }
                else
                {
                    StopCloseTimer();
                    m_TisWindow.Close();
                    m_TisWindow = null;
                    m_TisWindow = new TipWindow(aTitle, aContent, aLevel);
                    m_TisWindow.Show();
                    m_TisWindow.Top = 60;
                    StartCloseTimer(TimeSpan.FromSeconds(3));
                }
            }));
        }
        public void ShowDeskTopToolWindow(string aProcessName,Action<bool> aCallback)
        {
            m_UIDispatcher.Invoke(() => 
            {
                if (m_DesktopToolDialog == null)
                {
                    m_DesktopToolDialog = new DesktopToolDialog(aProcessName);
                    m_DesktopToolDialog.Closed += (s,e)=> 
                    {
                        m_DesktopToolDialog.IsShowing = false;
                        aCallback?.Invoke(m_DesktopToolDialog.IsIgnoreNext);
                        m_DesktopToolDialog = null;
                    };
                }
                if (m_DesktopToolDialog.IsShowing)
                {
                    return;
                }
                m_DesktopToolDialog.Show();
                m_DesktopToolDialog.IsShowing = true;
            });
        }

        public void ShowScanAnimationWnd(double aTop, double aLeft, double aWidth, double aHeight, EventHandler completedCallback)
        {
            m_UIDispatcher.Invoke(new Action(() =>
            {
                m_ScanAnimationWindow = new ScanAnimationWindow(aTop, aLeft, aWidth, aHeight);
                m_ScanAnimationWindow.ScanAnimationCompleted += completedCallback;
                m_ScanAnimationWindow.Show();
            }));
        }
        public void CloseScanAnimationWnd()
        {
            m_UIDispatcher.Invoke(new Action(() =>
            {
                m_ScanAnimationWindow?.Close();
                m_ScanAnimationWindow = null;
            }));
        }
        private void StartCloseTimer(TimeSpan closeTimeout)
        {
            m_Timer = new DispatcherTimer
            {
                Interval = closeTimeout
            };
            m_Timer.Tick += TimerTick;
            m_Timer.Start();
        }
        private void StopCloseTimer()
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer.Tick -= TimerTick;
            }
        }
        private void TimerTick(object sender, EventArgs e)
        {
            m_UIDispatcher.Invoke(() =>
            {
                m_TisWindow?.Close();
                m_TisWindow = null;
            });
            StopCloseTimer();
        }
    }
}
