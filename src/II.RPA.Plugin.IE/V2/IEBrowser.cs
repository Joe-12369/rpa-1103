using II.RPA.Plugin.IE.V2.Automation.Provider;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WebBrowser.Common.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using V2Browser = II.RPA.WebBrowser.Common.Basic;

namespace II.RPA.Plugin.IE.V2
{
    public class IEBrowser : V2Browser.Browser
    {
        #region 浏览器基础配置参数
        public override bool IgorneCreateSetting => true;
        public override string NativeMessageHostProcessName => string.Empty;
        public override string BrowserProcessName => "iexplore";
        protected override string ServiceKey => string.Empty;
        protected override string ExtensionsRegistryKey => string.Empty;
        protected override string[] ExtensionIDs => new string[2];
        public override eGuiType GuiType => eGuiType.IE;

        public override List<string> IgnoreUrl => new List<string>();
        #endregion

        #region 单例 全局点
        //private readonly static object objBrowserLock = new object();
        //protected static V2Browser.Browser instance = null;
        //public static V2Browser.Browser Instance
        //{
        //    get
        //    {
        //        if (instance == null)
        //        {
        //            lock (objBrowserLock)
        //            {
        //                if (instance == null)
        //                {
        //                    instance = new IEBrowser();
        //                }
        //            }
        //        }
        //        return instance;
        //    }
        //}

        public override WebDataProvider WebDataProvider { get; set; }
        #endregion

        #region 【重写】获取浏览器执行程序地址
        public override string GetBrowserAppPath()
        {
            try
            {
                if (!string.IsNullOrEmpty(BrowserAppPath))
                {
                    return BrowserAppPath;
                }
                RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\IEXPLORE.EXE", false);
                var resultPath = ((registryKey != null) ? registryKey.GetValue(null) : null) as string;
                return resultPath;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "iexplore browser Regedit select error");
                return null;
            }
        }
        #endregion
        public IEBrowser()
        {
            ConnectionConfig connConfig = this.GetConnectionConfig();
            this.WebDataProvider = new IEWebDataProvider(connConfig);
        }
        //public override IBrowserConnectionNew InitConnecttion()
        //{
        //    IBrowserConnectionNew browserConnectionCOM = new BrowserConnectionCOM();
        //    return browserConnectionCOM;
        //}
        #region 浏览器tab操作
        #region 获取激活tab对象
        public override BrowserTabPage AcquireCurrentActiveTab(List<IntPtr> wndHandles = null, PredicateFilter<BrowserTabPage> tabFilter = null)
        {
            try
            {
                //TODO:tabFilter未使用，IE可以根据句柄获取窗口下的tab,这里考虑是否需要使用 tabFilter
                int activeTabId = (int)DocumentHelper.GetIEDocumentWnds(wndHandles, true).FirstOrDefault();
                var tabpage = this.CreateBrowserTabPage(new ParamCreateBrowserTabPageModel() { TabId = activeTabId });
                return tabpage;
            }
            catch (RPAException ex)
            {
                throw ex;
            }
            catch (Exception e)
            {
                throw new RPAException(eErrorMessageType.Error_PageNotFound, e);
            }
        }

        #endregion

        public override BrowserTabPage CreateBrowserTabPage(ParamCreateBrowserTabPageModel paramModel)
        {
            return new IEBrowserTabPage(this, paramModel);
        }

        #endregion

        internal void ClearIHtmlObject()
        {
            ((IEWebDataProvider)this.WebDataProvider).ClearIHtmlObject();
        }
    }
}

