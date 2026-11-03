using II.RPA.Communication.Hosting;
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Threading;

namespace II.RPA.RuntimeHost
{
    /// <summary>
    /// ApplicationProvider
    /// </summary>
    public partial class ApplicationProvider : Form, IApplication
    {
        private SynchronizationContext _synchronizationContext;
        private readonly ManualResetEventSlim _loadedResetEvent;
        private const int LOADTIME = 1000;

        /// <summary>
        /// .ctor
        /// </summary>
        public ApplicationProvider()
        {
            _loadedResetEvent = new ManualResetEventSlim();
            Hide();
            SizeGripStyle = SizeGripStyle.Hide;
            WindowState = FormWindowState.Minimized;
            InitializeComponent();
            Load += UIThreadContextProvider_Load;
        }

        public SynchronizationContext MainUISynchronizationContext
        {
            get
            {
                if (_loadedResetEvent.Wait(LOADTIME))
                {
                    return _synchronizationContext;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            private set
            {
                _synchronizationContext = value;
            }
        }

        public Func<SynchronizationContext> CustomUISynchronizationContextBuilder
        {
            get
            {
                return BuildCustom;
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_APPWINDOW = 0x40000;
                const int WS_EX_TOOLWINDOW = 0x80;
                CreateParams cp = base.CreateParams;
                cp.ExStyle &= (~WS_EX_APPWINDOW);
                cp.ExStyle |= WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        /// <summary>
        /// UIThreadContextProvider_Load
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UIThreadContextProvider_Load(object sender, EventArgs e)
        {
            Hide();
            Location = new Point(-1000, -1000);
            MainUISynchronizationContext = SynchronizationContext.Current;
            _loadedResetEvent.Set();
        }

        /// <summary>
        /// BuildCustom
        /// </summary>
        /// <returns></returns>
        private SynchronizationContext BuildCustom()
        {
            SynchronizationContext synchronizationContext = null;
            ManualResetEventSlim manualResetEventSlim = new ManualResetEventSlim();

            var uiThread = new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                var uiDispatcher = Dispatcher.CurrentDispatcher;
                manualResetEventSlim.Set();
                synchronizationContext = SynchronizationContext.Current;
                Dispatcher.Run();
            });
            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.IsBackground = true;
            uiThread.Start();

            manualResetEventSlim.Wait();

            if (synchronizationContext != null)
            {
                return synchronizationContext;
            }
            else
            {
                throw new Exception("Fail to build ui thread!");
            }
        }
    }
}
