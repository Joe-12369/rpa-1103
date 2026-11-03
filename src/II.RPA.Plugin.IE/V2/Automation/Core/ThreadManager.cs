using System;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Threading;

namespace II.RPA.Plugin.IE.V2.Automation.Core
{
    public static class ThreadManager
    {

        private static readonly Thread m_UIThread;
        private static Dispatcher m_UIDispatcher;
        static ThreadManager()
        {
            ManualResetEventSlim m_StartedEvent = new ManualResetEventSlim(false);
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
            m_StartedEvent.Wait(500);
            Task.Run(() =>
            {
                Task.Delay(2000).Wait();
                m_StartedEvent.Dispose();
            });
        }
        //GUI 层 不该在这使用，暂时使用原有线程回归 保证还原Window对象统一
        public static T BackToUIThread<T>(Func<T> func)
        {

            return m_UIDispatcher.Invoke(() =>
            {
                return func();
            });
            //if (DataCollectingManager.dataCollectOverlayWnd == null)
            //{
            //    return func();
            //}
            //else
            //{
            //    return DataCollectingManager.dataCollectOverlayWnd.Dispatcher.Invoke(() =>
            //    {
            //        return func();
            //    });
            //}
            //return default(T);
        }
    }
}
