using II.RPA.Logging;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WinAPI;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.MultiInstance
{
    public class BrowserContextProvider
    {
        private static ILogger logger = Logger.OfContext();
        private readonly static Lazy<BrowserContextProvider> instance = new Lazy<BrowserContextProvider>(() => new BrowserContextProvider() { MultiInstanceMatcher = new MultiInstanceMatcher() });
        public static BrowserContextProvider Instance => instance.Value;
        public bool AllowMappingViaWindowActivation
        {
            get { return MultiInstanceMatcher.AllowMappingViaWindowActivation; }
            set { MultiInstanceMatcher.AllowMappingViaWindowActivation = value; }
        }

        private MultiInstanceMatcher MultiInstanceMatcher { get; set; }
        private BrowserContextInventory ContextInventory => BrowserContextInventory.Instance;
        public GuiOperatorBase GetGuiOperator(BrowserWindow browserWindow)
        {
            if (browserWindow == null) return null;
            var browserContext = GetOrBuildBrowserContext(browserWindow);
            return browserContext?.Operator;
        }
        public GuiOperatorBase GetDefaultGuiOperator(BrowserWindow browserWindow)
        {
            if (browserWindow == null) return null;
            var browserContext = ContextInventory.GetBrowserContextByHostId(-1 * (int)browserWindow.WindowType);
            return browserContext?.Operator;
        }
        private BrowserContext GetOrBuildBrowserContext(BrowserWindow browserWindow)
        {
            BrowserContext browserContext = GetBrowserContextByWindow(browserWindow);

            if (browserContext == null)
            {
                this.MultiInstanceMatcher.BuildBrowserWindowMap(browserWindow);
                return GetBrowserContextByWindow(browserWindow);
            }
            else if (ContextInventory.RemoveIfHostIdInvalid(browserContext.NativeMessagingHostId, out _))
            {
                return GetOrBuildBrowserContext(browserWindow);
            }
            return browserContext;
        }
        private BrowserContext GetBrowserContextByWindow(BrowserWindow browserWindow)
        {
            BrowserContext browserContext;
            if (browserWindow.WindowType.HasFlag(WindowType.IE))
            {
                browserContext = ContextInventory.GetBrowserContextByHostId(-1 * (int)WindowType.IE);
            }
            else if (browserWindow.Handle == IntPtr.Zero
               || BrowserSpecRepository.GetBrowserSepc(browserWindow)?.NMHProcessNames?.Length == 0)
            {
                browserContext = ContextInventory.GetBrowserContextByHostId(-1 * (int)browserWindow.WindowType);
            }
            else
            {
                browserContext = ContextInventory.GetBrowserContextByWindowHandle(browserWindow.Handle);
            }
            return browserContext;
        }
        /// <summary>
        /// 这个方法将提供和浏览器窗口具有同一个父进程的hostId，但是在多实例场景下这个ID，并不一定是和目标窗口是对应关系
        /// </summary>
        /// <param name="browserWindow"></param>
        /// <returns></returns>
        public int GetBrowserHostId(BrowserWindow browserWindow) => this.MultiInstanceMatcher.GetHostProcessIdsByBrowWnd(browserWindow).FirstOrDefault();

        public BrowserActionBase GetBrowserAction(BrowserParams args)
        {
            logger.LogDebug("获取chrome BrowserAction");
            var mainWindowHandle = IntPtr.Zero;
            BrowserWindow browserWindow = null;
            if (!Enum.TryParse<WindowType>(args.browserType, true, out var windowType))
            {
                windowType = WindowType.Chromium;
            }
            if (string.IsNullOrEmpty(args?.webData))
            {
                browserWindow = BrowserWindowProvider.GetFirstBrowserByZOrder(windowType);
                //如果没有找到，且是chromium类型，尝试从NMH发现（有可能浏览器特征未配置）
                if (browserWindow == null && windowType == WindowType.Chromium)
                {
                    ChromiumBrowserDiscovery.TryDiscoverFromNMH();
                    browserWindow = BrowserWindowProvider.GetFirstBrowserByZOrder(windowType);
                }
                browserWindow = browserWindow ?? new BrowserWindow(IntPtr.Zero) { WindowType = windowType };
                this.MultiInstanceMatcher.CheckProfileInstance(browserWindow, out _);//未指定网页对象，有可能需要操作所有，检测所有host
            }
            else
            {
                JObject jObject = JObject.Parse(args?.webData);
                if (!jObject.ContainsKey("mainWindowHandle"))
                {
                    throw new ArgumentException("parameter mainWindowHandle does not exist");
                }
                mainWindowHandle = new IntPtr(jObject.Value<int>("mainWindowHandle"));
                if (!BrowserWindowProvider.IsBrowserWindow(mainWindowHandle, windowType, out browserWindow) && windowType == WindowType.Chromium)
                {
                    //如果没有找到，尝试从NMH发现（有可能浏览器特征未配置）
                    ChromiumBrowserDiscovery.TryDiscover(mainWindowHandle);
                    BrowserWindowProvider.IsBrowserWindow(mainWindowHandle, windowType, out browserWindow);
                }
            }
            var browserContext = GetOrBuildBrowserContext(browserWindow);
            //if (mainWindowHandle == IntPtr.Zero)
            //{
            //    browserContext.Browser.NativeMessageHostProcessID = -1;
            //}
            if (browserContext == null)
            {
                logger.LogDebug("根据窗口信息，未获取到对应实例信息");
                throw new RPAException(eErrorMessageType.Error_ChromeExtensionUnopen);
            }
            BrowserActionBase browserActionBase = ContextInventory.CreateBrowserAction(browserContext);
            browserActionBase.SetTabPageInfo(args);
            return browserActionBase;
        }
        public List<BrowserActionBase> GetAllBrowserAction()
        {
            return ContextInventory.GetAllBrowserContext().Select(browserContext =>
            {
                return ContextInventory.CreateBrowserAction(browserContext);
            }).ToList();
        }
        public IEnumerable<GuiOperatorBase> GuiOperatorEnumerable => ContextInventory.GetAllBrowserContext().Select(entry => entry.Operator);
        public IEnumerable<Browser> BrowserEnumerable => ContextInventory.GetAllBrowserContext().Select(entry => entry.Browser);

        public bool CheckProfileNMHProcId(int NMHProcId, out int newNMHProcId) => this.MultiInstanceMatcher.CheckProfileNMHProcId(NMHProcId, out newNMHProcId);
    }
}
