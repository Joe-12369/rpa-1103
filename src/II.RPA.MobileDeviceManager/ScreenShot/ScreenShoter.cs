using II.RPA.Logging;
using II.RPA.MobileDeviceManager.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace II.RPA.MobileDeviceManager.ScreenShot
{
    public class ScreenShoter
    {
        public event EventHandler<FunctionEventArgs<ImageSource>> Snapped;
        private static readonly ILogger logger = Logger.OfContext();
        private readonly Thread m_UIThread;
        private Dispatcher m_UIDispatcher;
        private double m_Dpi;
        private readonly ManualResetEventSlim m_StartedEvent = new ManualResetEventSlim(false);
        private ScreenshotWindow m_ScreenshotWindow = null;
        private CapureShotToolWnd m_CapureShotToolWnd = null;
        public UsageScenarioType UsageScenario { get; set; } = UsageScenarioType.None;
        public ScreenShoter()
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
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
        }
        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            logger.LogInformation($"SystemEvents_DisplaySettingsChanged");
        }
        public void PreStart() => m_UIDispatcher.Invoke(() => { (m_CapureShotToolWnd = new CapureShotToolWnd(this)).Show(); });
        public void Start() => m_UIDispatcher.Invoke(() => { (m_ScreenshotWindow = new ScreenshotWindow(this)).Show(); });
        public void Stop() => m_UIDispatcher.Invoke(() => { m_ScreenshotWindow?.Close(); m_CapureShotToolWnd?.Close(); });

        internal void OnSnapped(ImageSource source, bool isDone) => Snapped?.Invoke(this, new FunctionEventArgs<ImageSource>(source, isDone));
    }
    public class FunctionEventArgs<T> : RoutedEventArgs
    {
        public FunctionEventArgs(T info, bool isDone)
        {
            Info = info;
            IsDone = isDone;
        }

        public FunctionEventArgs(RoutedEvent routedEvent, object source) : base(routedEvent, source)
        {
        }

        public T Info { get; set; }
        public bool IsDone { get; set; }
    }

    public enum UsageScenarioType
    {
        None = 0,
        ScreenShot = 1,//屏幕截图
        ScreenshotCapture = 2,//截图拾取元素
    }
}
