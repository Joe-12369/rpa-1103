using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Browsers.SuperBrowser.CreateBrowserBasic;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.CreateBrowserBasic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace II.RPA.Plugin.Browsers.SuperBrowser
{
    public class BrowserAction : BrowserActionBase
    {
        public override eGuiType GuiType => eGuiType.SuperBrowser;
        public override Browser Browser { get; }
        public override BrowserTabCreaterBase Creater { get; }
        public BrowserAction()
        {

        }
        public BrowserAction(Browser browser)
        {
            this.Browser = browser;
            this.Creater = new SuperCreater(browser);
        }
        public override string Create(string url, bool isWaitingload, int timeout = 1000)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var previousTabIds = this.Browser.IsRunBrowser ? this.Browser.TryGetActiveTabIds() : new List<int>();
            logger.LogDebug($"当前激活tabs{string.Join(",", previousTabIds)}");

            var startTime = Stopwatch.GetTimestamp();
            logger.LogDebug($"original url:{url}");
            if (!url.Contains("://"))
            {
                url = $"http://{url}";
                logger.LogInformation($"format url:{url}");
            }
            CreateProcess("");
            var endTime = Stopwatch.GetTimestamp();
            timeout -= Convert.ToInt32((endTime - startTime) * 1000D / Stopwatch.Frequency);

            do
            {
                Thread.Sleep(300);
                try
                {
                    if (previousTabIds?.Count == 0)
                    {
                        this.Browser.CheckNativeMeaageHostIsStarted();
                    }
                    var model = new ParamCreateTabModel() { url = url };
                    var targetTabJT = Browser.WebDataProvider.Create(model);
                    if (targetTabJT != null)
                    {
                        var targetTabId = targetTabJT.ToString().ToObject<int>();
                        logger.LogDebug($"New tab is arrived {targetTabId}");
                        this.TabPage = this.Browser.CreateBrowserTabPage(new ParamCreateBrowserTabPageModel() { TabId = targetTabId });
                    }
                    else
                    {
                        logger.LogDebug($"Wait new tab, previous tabId is {string.Join(", ", previousTabIds)}");
                        continue;
                    }


                    long waitingTimeout = timeout - stopWatch.ElapsedMilliseconds;
                    DocumentReadyState readState = DocumentReadyState.Complete;
                    if (!isWaitingload)
                    {
                        readState = readState | DocumentReadyState.Interactive;
                    }
                    if (this.TabPage.WaitToReadyState(readState, (int)waitingTimeout))
                    {
                        Maximized();
                        var result = this.TabPage.GetTabDetail().ToString();
                        this.TabPage.ActiveTab();
                        stopWatch.Stop();
                        return result;
                    }
                    else
                    {
                        logger.LogDebug("等待页面加载超时");
                        throw new RPAException(this.GuiType, eErrorMessageType.Error_Webbrowser_Create_LoadingTimeout_Error, eComponentType.Create);
                    }
                }
                catch (RPAException ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "创建浏览器异常 RPAException");
                    if (!(ex.ErrorMessageType == eErrorMessageType.Error_ChromeExtensionUnopen
                        || ex.ErrorMessageType == eErrorMessageType.Error_360ExtensionUnopen
                        || ex.ErrorMessageType == eErrorMessageType.Error_EdgeExtensionUnopen
                        || ex.ErrorMessageType == eErrorMessageType.Error_FireFoxExtensionUnopen
                        || ex.ErrorMessageType == eErrorMessageType.Error_SogouExtensionUnopen
                        || ex.ErrorMessageType == eErrorMessageType.Error_360XExtensionUnopen
                        || ex.ErrorMessageType == eErrorMessageType.Error_ExecuteJavaScriptCodeError
                        || ex.ErrorMessageType == eErrorMessageType.Error_BrowserPluginVersionTooLow
                        ))
                    {
                        throw ex;
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"创建浏览器异常");
                }
            } while (stopWatch.ElapsedMilliseconds <= timeout);
            if (this.TabPage != null)
            {
                var status = this.TabPage.ReadyState;
                logger.LogDebug($"打开网页，网页当前状态：{status}");
            }
            else
            {
                logger.LogInformation($"打开网页，未检测到新的激活页");
            }
            this.Browser.CheckNativeMeaageHostIsStarted();
            throw new RPAException(this.GuiType, eErrorMessageType.Error_WaitTimeout, eComponentType.Create);
        }

        public override void Close(bool isCloseAlert)
        {
            if (this.TabPage != null)
            {
                try
                {
                    this.TabPage.Close(isCloseAlert);
                    this.Browser.StopConnecttion();
                }
                catch (RPAException ex)
                {
                    if (ex.GuiType == eGuiType.None)
                        ex.GuiType = this.GuiType;
                    throw ex;
                }
                catch (Exception ex)
                {
                    throw new RPAException(this.GuiType, eErrorMessageType.Error_UNKOWN, ex);
                }
            }
            else
            {
                throw new RPAException(this.GuiType, eErrorMessageType.Error_PageNotFound);
            }

        }
    }
}

