using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.Win32Tools;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WebBrowser.Common.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic.CreateBrowserBasic
{
    public abstract class BrowserTabCreaterBase
    {
        protected ILogger logger = Logger.OfContext();
        public abstract eGuiType GuiType { get; }
        public abstract Browser Browser { get; }
        public string CreateProcessArgs { get; set; }
        public BrowserTabPage TabPage { get; protected set; }
        private List<eErrorMessageType> errorList = new List<eErrorMessageType>() {
            eErrorMessageType.Error_ChromeExtensionUnInstalled,
            eErrorMessageType.Error_EdgeExtensionUnInstalled,
            eErrorMessageType.Error_FireFoxExtensionUnInstalled,
            eErrorMessageType.Error_SogouExtensionUnInstalled,
            eErrorMessageType.Error_ChromeExtensionUnInstalled,
            eErrorMessageType.Error_360ExtensionUnInstalled,
            eErrorMessageType.Error_360XExtensionUnInstalled,
            eErrorMessageType.Error_BrowserPluginVersionTooLow
        };
        public string ValidURL(string url)
        {
            string actualUrl = string.Empty;
            try
            {
                new Uri(url);
                actualUrl = url;
            }
            catch (UriFormatException)
            {
                if (new Regex("^[a-z,A-Z]:[\\/\\\\].*").IsMatch(url))
                {
                    actualUrl = url;
                }
                else
                {
                    actualUrl = "http://" + url;
                }
            }
            return actualUrl;
        }
        public virtual string DefaultChromeProcessArgs => "--silent-debugger-extension-api --hide-crash-restore-bubble --allow-pre-commit-input --disable-backgrounding-occluded-windows --disable-renderer-backgrounding --disable-background-timer-throttling --disable-features=PreferencesReset";
        public virtual bool VerifyBrowserMainWindowHandle()
        {
            return this.Browser.GetFirstBrowserByZOrder() != null;
        }


        public virtual void CreateBrowserProcess(out int code)
        {
            code = 0;
            var resultPath = this.Browser.GetBrowserAppPath();
            if (!this.Browser.IsRunBrowser || !VerifyBrowserMainWindowHandle())
            {
                WindowCreateProcess.CreateBrowser(resultPath, this.Browser.BrowserProcessName, string.Empty, CreateProcessArgs, this.Browser.IgorneCreateSetting, out code, Browser.NeedCareProfileOccupation);
            }
        }

        public virtual bool WaitBrowserProcess(ParamCreateTabModel paramCreate, out int code)
        {
            code = 0;
            if (!RPAUtils.Retry(() => RPAUtils.GetProcessesBySessionId(this.Browser.BrowserProcessName).Length > 0, paramCreate.RunParam.Timeout / 2))
            {
                logger.LogDebug($"再次打开浏览器");
                this.CreateBrowserProcess(out code);
                logger.LogDebug($"再次打开浏览器结束");
            }
            return RPAUtils.Retry(() => VerifyBrowserMainWindowHandle(), paramCreate.RunParam.Timeout);
        }

        public virtual bool WaitBrowserHost(int timeout)
        {
            logger.LogDebug($"WaitBrowserHost timeout:{timeout}");
            if (RPAUtils.Retry(() =>
            {
                try
                {
                    if (this.Browser.CheckNativeMeaageHostIsStarted() && this.Browser.WebDataProvider.Ping())
                        return true;
                }
                catch (Exception ex)
                {
                    if (ex is RPAException rpaEx && errorList.Contains(rpaEx.ErrorMessageType))
                    {
                        throw rpaEx;
                    }
                    else
                    {
                        logger.LogDebug($"WaitBrowserHost exception:{ex.Message}");
                    }
                }
                return false;
            }, timeout))
            {
                return true;
            }
            Browser.CheckNativeMeaageHostIsStarted();
            logger.LogDebug("WaitBrowserHost timeout");
            throw new RPAException(this.GuiType, eErrorMessageType.Error_WaitTimeout);
        }
        private int CreateTabInWindow(string url)
        {
            int windowId = this.Browser.GetNormalWindowAll().FirstOrDefault();
            this.Browser.WindowId = windowId;
            logger.LogDebug($"WindowID{windowId}");
            if (windowId > 0)
            {
                this.Browser.ActiveWindow(windowId);
                return this.Browser.CreateTabInWindow(new ParamCreateTabModel() { url = url, windowId = windowId });
            }
            else
            {
                return this.Browser.Create(new ParamCreateTabModel() { url = url });
            }
        }
        public virtual BrowserTabPage CreateBrowserTab(string url, ParamCreateTabModel paramCreate)
        {
            int tabId = -1;
            var isVerify = RPAUtils.Retry(() => VerifyBrowserMainWindowHandle(), 5000);
            logger.LogDebug($"isVerify:{isVerify}");
            if (!isVerify)
            {
                logger.LogDebug($"Execute:CreateBrowserProcess");
                // 针对进程残留场景 需要先打开浏览器 再创建Tab
                CreateBrowserProcess(out _);
            }
            tabId = CreateTabInWindow(url);
            return this.Browser.CreateBrowserTabPage(new ParamCreateBrowserTabPageModel() { TabId = tabId, RunParam = paramCreate.RunParam });
        }

        public virtual BrowserTabPage GetCurrentActiveTabId()
        {
            PredicateFilter<BrowserTabPage> tabFilter = new PredicateFilter<BrowserTabPage>();
            if (this.Browser.IgnoreUrl?.Count > 0)
            {
                tabFilter.AddLast((tabPage) => !this.Browser.IgnoreUrl.Any(e => tabPage.Url.Contains(e)));
            }
            //一方面网络较差存在拿不到TabId的情况,但是等一等，会拿到TabID
            //另一方面 就是浏览器存在但是没有TabId 打开的空白页，
            // 等待获取TabID的时间不能太长，先给个三秒 看看效果
            if (RPAUtils.Retry(() => this.Browser.GetActiveTabIds().Count > 0, 3000, 300))
            {
                var activeTabIds = this.Browser.GetActiveTabIds();
                return this.Browser.MatchTabPageByPriority(activeTabIds, tabFilter);
            }
            else
            {
                return null;
            }
        }

        public virtual void StopLoading()
        {
            if (this.TabPage != null)
            {
                if (this.TabPage.ReadyState != DocumentReadyState.Complete)
                    this.TabPage.StopLoad();
            }
            else
            {
                throw new RPAException(this.GuiType, eErrorMessageType.Error_PageNotFound, eComponentType.Create);
            }
        }
        /// <summary>
        /// 仅用于打开网页组件的是否加载完成判断
        /// </summary>
        /// <param name="timeout"></param>
        /// <exception cref="RPAException"></exception>
        public virtual void WattingTabLoad(int timeout)
        {
            PageLoadStatus targetStatus = PageLoadStatus.Success;
            var result = RPAUtils.Retry(() =>
            {
                return TabPage.LoadStatus == targetStatus;
            }, timeout, 50);
            if (result)
            {
                Browser.Maximized(Browser.WindowId);
                return;
            }
            else
            {
                logger.LogDebug("等待页面加载超时");
                throw new RPAException(GuiType, eErrorMessageType.Error_Webbrowser_Create_LoadingTimeout_Error);
            }
        }
        public virtual void Navigate(string url)
        {
            if (this.TabPage != null)
            {
                this.TabPage.Navigate(new ParamNavigateModel() { url = url });
            }
            else
            {
                throw new RPAException(this.GuiType, eErrorMessageType.Error_PageNotFound, eComponentType.Create);
            }
        }
        private void WaitProcess(ParamCreateTabModel paramCreate)
        {
            if (!this.WaitBrowserProcess(paramCreate, out int code))
            {
                if (code == WindowCreateProcess.ExitCode_ProfileOccupation)
                {
                    throw new RPAException(this.GuiType, eErrorMessageType.Error_WebBrowser_Create_Occupation, eComponentType.Capture);
                }
                if (code == WindowCreateProcess.ExitCode_AccessDeny)
                {
                    throw new RPAException(this.GuiType, eErrorMessageType.Error_ProcessRefuseAccess, eComponentType.Capture);
                }
                else
                {
                    throw new RPAException(this.GuiType, eErrorMessageType.Error_Webbrowser_Create_NoNewTab_Error, eComponentType.Capture);
                }
            }
            if (paramCreate.RunParam.Timeout <= 0)
                throw new RPAException(this.GuiType, eErrorMessageType.Error_WaitTimeout, eComponentType.Capture);
            this.WaitBrowserHost(paramCreate.RunParam.Timeout);
        }
        private string MergeBySplitMergeDedupe(string defaults, string userArgs)
        {
            return string.Join(" ",
                    (userArgs ?? "").Split(' ')
                    .Concat((defaults ?? "").Split(' '))
                    .Where(x => x.Length > 0)
                    .Distinct());
        }

        public virtual BrowserTabPage BuilderBrowserTab(ParamCreateTabModel paramCreate)
        {
            if (this.GuiType != eGuiType.IE && GuiType != eGuiType.FireFox && GuiType != eGuiType.ZExplorer)
            {
                paramCreate.CreateProcessArgs = MergeBySplitMergeDedupe(DefaultChromeProcessArgs, paramCreate.CreateProcessArgs);
            }
            var stopWatch = Stopwatch.StartNew();
            string url = ValidURL(paramCreate.url);
            this.CreateProcessArgs = paramCreate.CreateProcessArgs;
            this.CreateBrowserProcess(out _);
            WaitProcess(paramCreate);

            if (paramCreate.RunParam.Timeout <= 0)
                throw new RPAException(this.GuiType, eErrorMessageType.Error_WaitTimeout, eComponentType.Capture);

            if (paramCreate.IsOpenNewTab)
            {
                this.TabPage = CreateBrowserTab(url, paramCreate);
            }
            else
            {
                var tab = GetCurrentActiveTabId();
                if (tab == null)
                    this.TabPage = CreateBrowserTab(url, paramCreate);
                else
                {
                    tab.ParamExtensionModel.RunParam = paramCreate.RunParam;
                    this.TabPage = tab;
                    this.StopLoading();
                    this.Navigate(url);
                }
            }
            if (paramCreate.IsWaitingload)
            {
                var elapsedTime = (int)stopWatch.ElapsedMilliseconds;
                var remainTime = paramCreate.timeout - elapsedTime;
                remainTime = remainTime < 0 ? 1 : remainTime;
                WattingTabLoad(remainTime);
            }
            else
                Task.Delay(1000).Wait();
            stopWatch.Stop();
            return this.TabPage;
        }
    }
}
