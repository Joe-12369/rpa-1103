using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using V2Browser = II.RPA.WebBrowser.Common.Basic;
using SHDocVw;
using MSHTML;
using II.RPA.WebBrowser.Common.Basic.CreateBrowserBasic;
using II.RPA.Plugin.IE.V2.CreateBrowserBasic;
using II.RPA.Plugin.Core.Commons;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.Plugin.IE.V2.Automation.Provider;

namespace II.RPA.Plugin.IE.V2
{
    #region [webBrowser Automation]
    public class IEAction : BrowserAction
    {

    }
    #endregion

    public class BrowserAction : BrowserActionBase
    {
        public override eGuiType GuiType => eGuiType.IE;
        public override Browser Browser { get; }
        public override BrowserTabCreaterBase Creater { get; }
        public BrowserAction()
        {

        }
        public BrowserAction(Browser browser)
        {
            this.Browser = browser;
            this.Creater = new IECreater(browser);
        }
        public override string Create(ParamCreateTabModel param)
        {
            if (param.IsOpenNewTab)
            {
                return Create(param.url, param.IsWaitingload, param.RunParam.Timeout);
            }
            else
            {
                var targetTabIds = this.Browser.GetActiveTabIds();
                if (targetTabIds.Count == 0)
                {
                    Create(param.url, param.IsWaitingload, param.RunParam.Timeout);
                }
                var targetTabId = this.Browser.GetActiveTabIds().FirstOrDefault();
                this.TabPage = this.Browser.CreateBrowserTabPage(new ParamCreateBrowserTabPageModel() { TabId = targetTabId });
                if (this.TabPage.ReadyState != DocumentReadyState.Complete)
                    this.TabPage.StopLoad();
                this.TabPage.Navigate(new V2Browser.Params.ParamNavigateModel() { url = param.url });
                return this.TabPage.GetTabDetail().ToString();

            }
        }
        public override string Create(string url, bool isWaitingload, int timeout = 1000)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            this.CreateProcess(url);
            do
            {
                Thread.Sleep(300);
                try
                {
                    var wbSearched = new ShellWindowsClass().Cast<IWebBrowser2>().ToList();
                    //获取最新打开的浏览器
                    wbSearched.Reverse();
                    int targetTabId = 0;
                    foreach (var wb in wbSearched)
                    {
                        if (wb != null && wb.Document != null)
                        {
                            var doc = wb.Document as IHTMLDocument;
                            if (doc != null)
                            {
                                targetTabId = (int)DocumentHelper.GetIEDocumentWnd((IntPtr)wb.HWND, true);
                                break;
                            }
                        }
                    }
                    logger.LogDebug($"IE New tab is arrived {targetTabId}");
                    if (targetTabId <= 0)
                        continue;

                    this.TabPage = this.Browser.CreateBrowserTabPage(new ParamCreateBrowserTabPageModel() { TabId = targetTabId });

                    long waitingTimeout = timeout - stopWatch.ElapsedMilliseconds;
                    DocumentReadyState readState = DocumentReadyState.Complete;
                    if (!isWaitingload)
                    {
                        readState = readState | DocumentReadyState.Interactive;
                    }
                    logger.LogDebug($"等待页面加载，当前状态：{this.TabPage.ReadyState}，目标状态：{readState}，剩余等待时间：{waitingTimeout}");
                    if (this.TabPage.WaitToReadyState(readState, (int)waitingTimeout))
                    {
                        logger.LogDebug($"页面加载完成，当前状态：{this.TabPage.ReadyState}");
                        Maximized();
                        var result = this.TabPage.GetTabDetail().ToString();
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
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "创建浏览器异常");
                    if (!(ex.ErrorMessageType == eErrorMessageType.Error_ChromeExtensionUnopen
                        || ex.ErrorMessageType == eErrorMessageType.Error_360ExtensionUnopen
                        || ex.ErrorMessageType == eErrorMessageType.Error_EdgeExtensionUnopen
                        || ex.ErrorMessageType == eErrorMessageType.Error_FireFoxExtensionUnopen
                        || ex.ErrorMessageType == eErrorMessageType.Error_SogouExtensionUnopen
                        || ex.ErrorMessageType == eErrorMessageType.Error_360XExtensionUnopen
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
            //this.Browser.CheckNativeMeaageHostIsStarted();
            throw new RPAException(this.GuiType, eErrorMessageType.Error_WaitTimeout, eComponentType.Create);
        }
        public override void CaptureScreenshot(bool saveToClp, int cutRange, string savePath, bool cover, int height)
        {
            if (Browser != null)
            {
                Image image = null;
                try
                {
                    switch (cutRange)
                    {
                        case 0:
                            base.Screenshot(savePath);
                            break;
                        case 1:
                            var result = this.TabPage.TabId;
                            image = II.RPA.Screenshot.CaptureHelper.CaptureIe(height, true, result);
                            break;
                        default:
                            throw new IEException(eErrorMessageType.Error_NotImplemented);
                    }

                    if (saveToClp)
                    {
                        UiCommon.ClipboardSetImage(image);
                    }
                    else
                    {
                        if (System.IO.File.Exists(savePath) && !cover)
                        {
                            return;
                        }
                        image.Save(savePath);
                    }
                }
                catch (IEException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw new IEException(eErrorMessageType.Error_UNKOWN, e);
                }
                finally
                {
                    if (image != null)
                    {
                        image.Dispose();
                    }
                }
            }
            else
            {
                throw new IEException(eErrorMessageType.Error_PageNotFound);
            }
        }
    }
}

