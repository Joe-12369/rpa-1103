using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace II.RPA.Core.Common
{
    public class UIThreadProvider
    {
        private readonly ManualResetEventSlim m_StartedEvent = new ManualResetEventSlim(false);
        public Thread UIThread
        {
            get;
            private set;
        }
        public Dispatcher UIDispatcher
        {
            get;
            private set;
        }

        public UIThreadProvider()
        {
            UIThread = new Thread(delegate ()
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                UIDispatcher = Dispatcher.CurrentDispatcher;
                m_StartedEvent.Set();
                Dispatcher.Run();
            });
            UIThread.SetApartmentState(ApartmentState.STA);
            UIThread.IsBackground = true;
            UIThread.Start();
            m_StartedEvent.Wait();
        }
        public static UIThreadProvider CreateNew()
        {
            return new UIThreadProvider();
        }
    }
}
