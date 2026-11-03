using II.RPA.Plugin.Chrome.BrowserProfile;
using II.RPA.WebBrowser.Common.Basic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.MultiInstance
{
    internal class BrowserContext : IDisposable
    {
        private NMHProcessLifeWatcher _watcher = null;
        private int _nativeMessagingHostId = -1;

        public BrowserSpec BrowserSpec { get; set; }
        public Browser Browser { get; set; }
        public GuiOperatorBase Operator { get; set; }

        public List<IntPtr> BrowserWndHandles { get; set; }
        public int MainProcessId { get; set; }
        public int NativeMessagingHostId
        {
            get
            {
                return _nativeMessagingHostId;
            }
            set
            {
                _watcher?.Dispose();
                _watcher = null;
                _nativeMessagingHostId = value;
            }
        }
        internal BrowserContext(int nativeMessagingHostId)
        {
            this.NativeMessagingHostId = nativeMessagingHostId;
            this.BrowserWndHandles = new List<IntPtr>();
            if (NativeMessagingHostId == -1)
            {
                BrowserWndHandles.Add(IntPtr.Zero);
            }
        }
        public bool IsAlive()
        {
            try
            {
                if (_watcher == null)   // pid 换了才重新 new
                {
                    _watcher = new NMHProcessLifeWatcher(NativeMessagingHostId);
                }
                return !_watcher.HasExited;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public void Dispose() => _watcher?.Dispose();

        public static BrowserContext CreateContext(int hostId, BrowserSpec browserSpec)
        {
            var context = new BrowserContext(hostId);
            var browser = (Browser)Activator.CreateInstance(browserSpec.BrowserType);
            browser.NativeMessageHostProcessID = context.NativeMessagingHostId;
            var guiOperator = (GuiOperatorBase)Activator.CreateInstance(browserSpec.GuiOperatorType, browser);
            context.Browser = browser;
            context.Operator = guiOperator;
            return context;
        }
    }
}
