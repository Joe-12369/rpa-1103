using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.WebBrowser.Common.Basic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace II.RPA.WebBrowser.Common.MultiInstance
{
    internal class BrowserContextInventory
    {
        private readonly ConcurrentDictionary<int, BrowserContext> _hostIdMap;
        private readonly ConcurrentDictionary<IntPtr, int> _wndHandleToHostIdMap;
        private readonly static Lazy<BrowserContextInventory> instance = new Lazy<BrowserContextInventory>(() => new BrowserContextInventory());
        public static BrowserContextInventory Instance => instance.Value;
        public BrowserContextInventory()
        {
            _hostIdMap = new ConcurrentDictionary<int, BrowserContext>();
            _wndHandleToHostIdMap = new ConcurrentDictionary<IntPtr, int>();
        }
        public void GetOrRegister(BrowserContext context)
        {
            _hostIdMap.AddOrUpdate(context.NativeMessagingHostId, context, (key, value) => context);
            UpdateWindowHandles(context);
        }
        public List<BrowserContext> GetAllBrowserContext()
        {
            return _hostIdMap.Values.ToList();
        }
        public BrowserContext GetBrowserContextByHostId(int hostId)
        {
            return _hostIdMap.TryGetValue(hostId, out var entry) ? entry : null;
        }
        public BrowserContext GetBrowserContextByWindowHandle(IntPtr wndHandle)
        {
            return _wndHandleToHostIdMap.TryGetValue(wndHandle, out var hostId) ? GetBrowserContextByHostId(hostId) : null;
        }

        public GuiOperatorBase GetOperatorByHostId(int hostId)
        {
            return _hostIdMap.TryGetValue(hostId, out var entry) ? entry.Operator : null;
        }

        public GuiOperatorBase GetOperatorByWindowHandle(IntPtr wndHandle)
        {
            if (_wndHandleToHostIdMap.TryGetValue(wndHandle, out var hostId))
                return GetOperatorByHostId(hostId);
            return null;
        }

        public void UpdateWindowHandles(BrowserContext context)
        {
            foreach (var handle in context.BrowserWndHandles)
            {
                _wndHandleToHostIdMap.AddOrUpdate(handle, context.NativeMessagingHostId, (key, val) => context.NativeMessagingHostId);
            }
        }
        public void ClearAll()
        {
            _hostIdMap.Clear();
            _wndHandleToHostIdMap.Clear();
        }

        public bool RemoveByHostId(int hostId)
        {
            if (_hostIdMap.TryRemove(hostId, out var browserContext))
            {
                foreach (var handle in browserContext.BrowserWndHandles)
                {
                    _wndHandleToHostIdMap.TryRemove(handle, out _);
                }
                return true;
            }
            return false;
        }
        public bool RemoveIfHostIdInvalid(int nativeMessagingHostId, out BrowserContext browserContext)
        {
            bool exited = false;
            browserContext = default;

            if (nativeMessagingHostId <= 0)
            {
                return exited;
            }
            browserContext = GetBrowserContextByHostId(nativeMessagingHostId);
            if (browserContext == null)
            {
                exited = IsProcessGone(nativeMessagingHostId);
            }
            else
            {
                exited = !browserContext.IsAlive();
            }
            if (exited)
            {
                browserContext?.Dispose();
                RemoveByHostId(nativeMessagingHostId);
                return true;
            }
            return false;
        }
        public bool IsProcessGone(int pid)
        {
            try
            {
                using (var p = System.Diagnostics.Process.GetProcessById(pid))
                {
                    return p.HasExited;
                }
            }
            catch (ArgumentException)
            {
                return true;
            }
        }
        public int RemoveInvalidContext()
        {
            int removeCount = 0;
            var invalidKeys = _hostIdMap.Where(item => item.Key > 0 && !item.Value.IsAlive())
                                        .Select(item => item.Key)
                                        .ToList();
            foreach (var key in invalidKeys)
            {
                if (_hostIdMap.TryRemove(key, out _))
                {
                    removeCount++;
                    RemoveByHostId(key);
                }
            }
            return removeCount;
        }
        public void TryRegisterEntries(List<int> validHostIds, BrowserWindow browserWindow)
        {
            foreach (var processId in validHostIds)
            {
                if (GetBrowserContextByHostId(processId) == null)
                {
                    GetOrRegister(CreateContext(processId, browserWindow));
                }
            }
        }
        internal BrowserContext CreateContext(int hostId, BrowserWindow browserWindow)
        {

            var context = new BrowserContext(hostId);
            var browserSpec = BrowserSpecRepository.GetBrowserSepc(browserWindow);
            Browser browser;
            if (browserWindow.WindowType == WindowType.Chromium)
            {
                browser = (Browser)Activator.CreateInstance(browserSpec.BrowserType, browserWindow.HitMatchHandler?.MatchConfig);
            }
            else
            {
                browser = (Browser)Activator.CreateInstance(browserSpec.BrowserType);
            }
            browser.NativeMessageHostProcessID = context.NativeMessagingHostId;
            browser.BrowserAppPath = browserWindow.AppExePath;
            var guiOperator = (GuiOperatorBase)Activator.CreateInstance(browserSpec.GuiOperatorType, browser);
            context.Browser = browser;
            context.Operator = guiOperator;
            context.BrowserSpec = browserSpec;
            return context;
        }
        internal BrowserActionBase CreateBrowserAction(BrowserContext context)
        {
            return (BrowserActionBase)Activator.CreateInstance(context.BrowserSpec.BrowserActionType, context.Browser);
        }

    }
}
