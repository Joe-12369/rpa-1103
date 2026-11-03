using II.RPA.Logging;
using II.RPA.Plugin.GUI.ProcessCreator.ElementTrack;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace II.RPA.Plugin.GUI.ProcessCreator.Screenshot
{
    public class ImageTracker
    {
        private static readonly ILogger logger = Logger.OfContext();
        private readonly Thread m_UIThread;
        private Dispatcher m_UIDispatcher;
        private CaptureWindow m_CurrentCaptureWindow;
        private readonly ManualResetEventSlim m_StartedEvent = new ManualResetEventSlim(false);
        private readonly ShotPolicy m_ShotPolicy;
        public ImageTracker()
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
            logger.LogInformation($"UI线程ID:{m_UIThread.ManagedThreadId}");
            logger.LogInformation($"当前线程ID:{Thread.CurrentThread.ManagedThreadId}");
            m_StartedEvent.Wait();
        }
        public ImageTracker(ShotPolicy policy):this()
        {
            m_ShotPolicy = policy;
        }

        public bool IsStarted
        {
            get;
            private set;
        } = false;

        public void Start(ElementTracker anElementTracker)
        {
            IsStarted = true;
            m_UIDispatcher.InvokeAsync(() => 
            {
                CaptureWindow captureWindow = new CaptureWindow();
                captureWindow.Show();
                CaptureWindow currWin = m_CurrentCaptureWindow;
                if (currWin != null)
                {
                    currWin.Close();
                }
                m_CurrentCaptureWindow = captureWindow;
                m_CurrentCaptureWindow.Closed += (s, e) => 
                {
                    IsStarted = false;
                    if (anElementTracker.IsStart)
                    {
                        anElementTracker.Start().ConfigureAwait(false);
                    }
                };
            }); 
        }
        public string StartOnceTime()
        {
            IsStarted = true;
            string selector = string.Empty;
            ManualResetEventSlim manualResetEventSlim = new ManualResetEventSlim(false);
            m_UIDispatcher.BeginInvoke(new Action(() =>
            {
                CaptureWindow captureWindow = new CaptureWindow(true, manualResetEventSlim, m_ShotPolicy);
                captureWindow.Show();
                CaptureWindow currWin = m_CurrentCaptureWindow;
                if (currWin != null)
                {
                    currWin.Close();
                }
                m_CurrentCaptureWindow = captureWindow;
                m_CurrentCaptureWindow.Closed += (s, e) =>
                {

                };
            }));
            Thread.Sleep(1);
            manualResetEventSlim.Wait();
            selector = m_CurrentCaptureWindow.SelectorResult;
            IsStarted = false;
            return selector;
        }
        public string StartOnceTime(ElementTracker anElementTracker)
        {
            IsStarted = true;
            string selector = string.Empty;
            ManualResetEventSlim manualResetEventSlim = new ManualResetEventSlim(false);
            m_UIDispatcher.BeginInvoke(new Action(() =>
            {
                CaptureWindow captureWindow = new CaptureWindow(true, manualResetEventSlim);
                captureWindow.Show();
                CaptureWindow currWin = m_CurrentCaptureWindow;
                if (currWin != null)
                {
                    currWin.Close();
                }
                m_CurrentCaptureWindow = captureWindow;
                m_CurrentCaptureWindow.Closed += (s, e) =>
                {
                    if (anElementTracker.IsStart)
                    {
                        anElementTracker.Start().ConfigureAwait(false);
                    }
                };
            }));
            Thread.Sleep(1);
            manualResetEventSlim.Wait();
            selector = m_CurrentCaptureWindow.SelectorResult;
            IsStarted = false;
            return selector;
        }
    }
}
