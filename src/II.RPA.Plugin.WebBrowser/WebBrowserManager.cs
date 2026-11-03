using II.RPA.Plugin.GUI.Commons;
using II.RPA.WebBrowser.Common;
using System;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.MultiInstance;

namespace II.RPA.Plugin.WebBrowser
{
    public class WebBrowserManager
    {
        private static ILogger logger = Logger.OfContext();
        private static readonly object _lock = new object();
        private static WebBrowserManager _webManager = null;
        /// <summary>
        /// web类型selectortype
        /// </summary>
        internal static string[] selectorType_Web = new string[] { "web", "chrome", "ie", "firefox", "zexplorer", "web360se", "sogou", "edge", "web360x", "superbrowser", "chromium" };
        internal static string supportGuiType = ",chrome,edge,firefox,web360se,web360x,superbrowser,chromium,";
        private WebBrowserManager()
        {
            try
            {
                UiCommon.UIAAutomation.Equals(null);
            }
            catch (Exception)
            {
                logger.LogWarning("通过调用的方式初始化UIACore失败");
            }
        }
        public static BrowserActionBase GetBrowserAction(BrowserParams browserParams)
        {
            if (II.RPA.Plugin.Core.GlobleConfigurations.UseSZBrowser == true)
            {
                browserParams.browserType = eGuiType.ZExplorer.ToString();
            }

            return BrowserContextProvider.Instance.GetBrowserAction(browserParams);

            //var activeBaseList = WebBrowserManager.Info.ActionBaseList;
            //logger.LogDebug($"GetActionBase: {activeBaseList.Count}");
            //var pubBrowserAction = activeBaseList.FirstOrDefault(ab => ab.GuiType.ToString().Equals(browserParams.browserType, StringComparison.OrdinalIgnoreCase))
            //                    ?? activeBaseList.FirstOrDefault(ab => ab.GuiType == eGuiType.Chromium);
            //var browserAction = (BrowserActionBase)pubBrowserAction?.GetActionBase(browserParams);
            //logger.LogDebug($"找到BrowserAction:{browserAction?.GuiType}");
            //return browserAction;
        }
        public static WebBrowserManager Info
        {
            get
            {
                if (_webManager == null)
                {
                    lock (_lock)
                    {
                        if (_webManager == null)
                        {
                            _webManager = new WebBrowserManager();
                        }
                    }
                }
                return _webManager;
            }
        }
    }
}