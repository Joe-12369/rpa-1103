using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.WebBrowser.Common.Basic.CreateBrowserBasic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WebBrowser.Common.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace II.RPA.WebBrowser.Common.Basic
{
    public abstract class BrowserActionBase : IActionBase
    {
        protected ILogger logger = Logger.OfContext();


        public abstract eGuiType GuiType { get; }
        public abstract Browser Browser { get; }
        public BrowserTabPage TabPage { get; protected set; }
        public abstract BrowserTabCreaterBase Creater { get; }
        #region 异常公共处理方法
        private T Wrapper<T>(Func<T> func)
        {

            if (this.TabPage != null)
            {
                try
                {
                    return func.Invoke();
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

        protected void Wrapper(Action action)
        {
            Wrapper(() =>
            {
                action.Invoke();
                return 0;
            });
        }
        #endregion
        public virtual string Acquire(string value, string mode = "title", int index = 1, bool isActive = true, bool isMatchAll = false, string browserType = "")
        {
            try
            {
                var title = mode.Equals("title") ? value : null;
                var url = mode.Equals("url") ? value : null;
                if (!string.IsNullOrEmpty(mode) && isMatchAll)
                {
                    var tabs = this.Browser.MatchTabPages(title, url);
                    if (tabs.Count() <= 0)
                    {
                        throw new RPAException(this.GuiType, eErrorMessageType.Error_PageNotFound);
                    }
                    if (isActive)
                    {
                        tabs.First().ActiveTab();
                    }
                    return tabs.Select(tab => tab.GetCompleteTabDetail()).ToJson();
                }

                BrowserTabPage tabPage = null;
                if (string.IsNullOrEmpty(mode))
                {

                    //AcquireCurrentActiveTabId 和 MatchsTabId  优先级里边，后者会考虑popup的优先级，所以这个地方还是区分使用获取当前页
                    tabPage = this.Browser.AcquireCurrentActiveTab();
                }
                else
                {
                    tabPage = this.Browser.MatchTabPage(title, url);
                }
                if (tabPage == null)
                {
                    throw new RPAException(this.GuiType, eErrorMessageType.Error_PageNotFound);
                }

                if (isActive)
                    tabPage.ActiveTab();
                var tabPageDetail = tabPage.GetCompleteTabDetail();
                if (browserType == "0")
                {
                    if (tabPageDetail != null)
                    {
                        if (tabPageDetail["type"] == null)
                        {
                            tabPageDetail["type"] = this.GuiType.ToString().ToLower();
                        }
                    }
                }
                return tabPageDetail?.ToString();
            }
            catch (Exception ex)
            {
                if (ex is RPAException)
                    throw ex;
                throw new RPAException(this.GuiType, eErrorMessageType.Error_PageNotFound, ex);
            }
        }
        public virtual void Active()
        {
            Wrapper(() =>
            {
                this.TabPage.ActiveTab();
            });
        }
        public virtual void Switch(bool isActive)
        {
            Wrapper(() =>
            {
                if (isActive)
                {
                    this.TabPage.ActiveTab();
                }
                else
                {
                    this.TabPage.SwitchTab();
                }
            });
        }
        public virtual void CaptureScreenshot(bool saveToClp, int cutRange, string savePath, bool cover, int height)
        {
            Wrapper(() =>
            {
                Image image = null;
                try
                {
                    switch (cutRange)
                    {
                        case 0:
                            image = this.TabPage.CaptureByTab();
                            break;
                        case 1:
                            image = this.TabPage.CaptureByDebugger(height);
                            break;
                        default:
                            throw new RPAException(this.GuiType, eErrorMessageType.Error_NotImplemented);
                    }

                    if (saveToClp)
                    {
                        UiCommon.ClipboardSetImage(image);
                    }
                    else
                    {
                        if (File.Exists(savePath) && !cover)
                        {
                            return;
                        }
                        Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                        image.Save(savePath);
                    }
                }
                catch (RPAException rpaEx)
                {
                    throw rpaEx;
                }
                catch (Exception e)
                {
                    throw new RPAException(this.GuiType, eErrorMessageType.Error_UnauthorizedAccessError, e);
                }
                finally
                {
                    if (image != null)
                    {
                        image.Dispose();
                    }
                }
            });
        }
        public virtual void CloseRight(bool isCloseAlert)
        {
            Wrapper(() =>
            {
                this.TabPage.CloseRight(this.TabPage.TabId, isCloseAlert);
                this.Browser.StopConnecttion();
            });
        }
        public virtual void CloseOther(bool isCloseAlert)
        {
            Wrapper(() =>
            {
                this.TabPage.CloseOther(this.TabPage.TabId, isCloseAlert);
                this.Browser.StopConnecttion();
            });
        }
        public virtual void CloseList(List<int> tabIds, bool isCloseAlert)
        {
            Wrapper(() =>
            {
                this.TabPage.CloseList(tabIds, isCloseAlert);
                this.Browser.StopConnecttion();
            });
        }
        public virtual void Close(bool isCloseAlert)
        {
            Wrapper(() =>
            {
                var allTabIds = this.Browser.GetAllTabIds();
                if (allTabIds.Count() < 2)
                {
                    CloseAll(true, isCloseAlert: isCloseAlert);
                    return;
                }
                if (allTabIds.Contains(this.TabPage.TabId))
                {
                    this.TabPage.Close(isCloseAlert);
                    this.Browser.StopConnecttion();
                }
            });
        }
        public virtual void CloseAll(bool killProcess, int timeout = 10000, bool isCloseAlert = false)
        {
            this.Browser.CloseAll(killProcess, timeout, isCloseAlert);
            this.Browser.StopConnecttion();
        }
        public virtual void CreateProcess(string url)
        {
            var resultPath = this.Browser.GetBrowserAppPath();
            WindowCreateProcess.CreateBrowser(resultPath,
                this.Browser.BrowserProcessName,
                url,
                this.Browser.CreateProcessArgs,
                this.Browser.IgorneCreateSetting,
                out _,
                Browser.NeedCareProfileOccupation);
            this.Browser.CreateProcessArgs = string.Empty;
        }
        public virtual void AddCreateProcessArgs(string arguments)
        {
            arguments = " " + arguments;
            this.Browser.CreateProcessArgs += arguments;
        }

        public virtual string Create(string url, bool isWaitingload, int timeout = 1000)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var previousTabIds = this.Browser.IsRunBrowser ? this.Browser.TryGetActiveTabIds() : new List<int>();
            logger.LogDebug($"当前激活tabs{string.Join(",", previousTabIds)}");

            var startTime = Stopwatch.GetTimestamp();
            CreateProcess(url);
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
                    var currentTabIds = this.Browser.GetActiveTabIds();
                    if (currentTabIds.All(c => previousTabIds.Contains(c)))
                    {
                        logger.LogDebug($"Wait new tab, previous tabId is {string.Join(", ", previousTabIds)}");
                        continue;
                    }
                    var targetTabId = currentTabIds.FirstOrDefault(c => !previousTabIds.Contains(c));
                    logger.LogDebug($"New tab is arrived {targetTabId}");
                    this.TabPage = this.Browser.CreateBrowserTabPage(new ParamCreateBrowserTabPageModel() { TabId = targetTabId });

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

        public virtual string Create(ParamCreateTabModel param)
        {
            BrowserTabCreaterBase browserCreater = this.Creater;
            param.CreateProcessArgs = $"{this.Browser.CreateProcessArgs} {param.CreateProcessArgs}";

            this.TabPage = browserCreater.BuilderBrowserTab(param);
            this.TabPage.SetForeground();
            return this.TabPage.GetCompleteTabDetail().ToString();
        }

        public virtual string Download(string selector, string saveFile = "", string fileName = "", int timeout = 10000)
        {
            ExtractViewData.SelectCommonProps(selector, out string selectorType, out string version, out _, out _, out var discernHidden);
            var tabPage = this.Browser.CreateBrowserTabPage(new ParamCreateBrowserTabPageModel());
            ElementGuiNode guiNode;
            if (version.StartsWith("1."))
            {
                var cssSelector = ExtractViewData.SelectorCssSelector(selector);
                selector = UiCommon.ExtractSelectors(selector);
                guiNode = tabPage.GetElementFromSelector(selector, cssSelector, true, version, discernHidden);
            }
            else
            {
                guiNode = tabPage.GetElementFromSelectorOld(selector);
            }
            return guiNode.ElementDownload(saveFile, fileName, timeout);

        }
        public virtual string DownloadUrl(string downLoadUrl, string saveFile = "", int timeout = 10000)
        {
            var paramModel = new ParamDownloadModel()
            {
                url = downLoadUrl,
                saveFile = saveFile,
                RunParam = new Plugin.Core.Entities.RunParamModel
                {
                    TotalTime = timeout
                }
            };
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var tabPage = this.Browser.AcquireCurrentActiveTab();
            var downloadId = tabPage.DownLoadUrl(paramModel);
            try
            {
                while (stopWatch.ElapsedMilliseconds <= paramModel.Timeout)
                {
                    int id = 0;
                    int.TryParse(downloadId, out id);
                    paramModel.id = id;
                    var downloaditem = tabPage.DownLoadSearch(paramModel);
                    foreach (var item in downloaditem)
                    {
                        if (downloadId == item.Value<string>("id").ToString())
                        {
                            if (item.Value<string>("filename") != null)
                            {
                                var path = item.Value<string>("filename").ToString();
                                if (File.Exists(item.Value<string>("filename").ToString()))
                                {
                                    File.Copy(item.Value<string>("filename").ToString(), paramModel.saveFile, overwrite: true);
                                    stopWatch.Stop();
                                    return item.Value<string>("id");
                                }
                            }
                        }
                    }
                }
                throw new RPAException(this.GuiType, eErrorMessageType.Error_DownloadTimeOut);
            }
            catch (RPAException ex)
            {
                if (ex is RPAException)
                {
                    throw ex;
                }
                throw new RPAException(this.GuiType, eErrorMessageType.Error_DownloadError, ex);
            }
        }
        public virtual string ElementExecutejs(string selector, string scriptCode, string @params)
        {

            ExtractViewData.SelectCommonProps(selector, out string selectorType, out string version, out _, out _, out var discernHidden);
            var tabPage = this.Browser.CreateBrowserTabPage(new ParamCreateBrowserTabPageModel());
            ElementGuiNode guiNode;
            if (version.StartsWith("1."))
            {
                var cssSelector = ExtractViewData.SelectorCssSelector(selector);
                selector = UiCommon.ExtractSelectors(selector);
                guiNode = tabPage.GetElementFromSelector(selector, cssSelector, true, version, discernHidden);
            }
            else
            {
                guiNode = tabPage.GetElementFromSelectorOld(selector);
            }
            return guiNode.Executejs(scriptCode, @params);
        }
        [Obsolete]
        public virtual string Executejs(string title, string url, string scriptCode, string @params)
        {
            //chromeTab.IsActive();
            var tabPage = this.Browser.AcquireCurrentActiveTab();
            ElementGuiNode guiNode = tabPage.CreateElementGuiNode("");
            var code = string.Format("scriptCode={0}", scriptCode);
            guiNode.PageElement.Executejs(code);
            System.Threading.Thread.Sleep(100);
            if (string.IsNullOrEmpty(@params))
            {
                return guiNode.PageElement.Executejs(string.Format("scriptCode()"));
            }
            else
            {
                string[] param = @params.Split(',');
                StringBuilder stringBuilder = new StringBuilder(256);
                stringBuilder.Append("scriptCode(");
                for (int i = 0; i < param.Length; i++)
                {
                    int result = 0;
                    if (int.TryParse(param[i], out result))
                    {
                        if (i != param.Length - 1)
                            stringBuilder.Append(result + ",");
                        else
                            stringBuilder.Append(result);
                    }
                    else
                    {
                        if (i != param.Length - 1)
                            stringBuilder.Append("\"" + param[i] + "\"" + ",");
                        else
                            stringBuilder.Append("\"" + param[i] + "\"");
                    }
                }
                stringBuilder.Append(")");
                return guiNode.PageElement.Executejs(stringBuilder.ToString());
            }
        }
        public virtual IActionBase GetActionBase(BrowserParams args)
        {
            var browserAction = (BrowserActionBase)Activator.CreateInstance(this.GetType());
            browserAction.SetTabPageInfo(args);
            return browserAction;
        }
        public virtual void SetTabPageInfo(BrowserParams args)
        {
            if (!string.IsNullOrEmpty(args.webData))
            {
                JObject jObject = JObject.Parse(args.webData);
                if (jObject.ContainsKey("id"))
                {
                    var tabid = jObject.Value<int>("id");
                    var mainWindowHandle = new IntPtr(jObject.Value<long>("mainWindowHandle"));
                    var webWindowInfo = new BrowserWindow(mainWindowHandle);
                    webWindowInfo.SpecifiedTabId = tabid;
                    webWindowInfo.WindowType = (WindowType)Enum.Parse(typeof(WindowType), this.GuiType.ToString());
                    webWindowInfo.TabDetailJson = args.webData;
                    if (!BrowserWindowProvider.BrowserWindowTypeCheck(webWindowInfo))
                    {
                        throw new RPAException(eErrorMessageType.Error_BrowserWindowTypeCheck_Error);
                    }
                    this.TabPage = this.Browser.CreateBrowserTabPage(new ParamCreateBrowserTabPageModel() { TabId = tabid, WebWindowInfo = webWindowInfo });
                    this.TabPage.GetTabDetail();//若查不到有效信息，将抛出Error_PageNotFound异常
                }
            }
        }
        public virtual string GetCookies(string url, string name = null)
        {
            var model = new ParamCookieModel() { url = url, name = name };
            return Wrapper(() =>
            {
                return this.TabPage.GetCookies(model);
            });
        }
        public virtual string GetDocument()
        {
            return Wrapper(() =>
            {
                return this.TabPage.GetDocument();
            });
        }
        public virtual string GetScroll(bool isElementOfScroll, int directionType, string selector)
        {
            return Wrapper(() =>
            {
                eScrollDirection eScrollDirection = directionType == 0 ? eScrollDirection.Left : eScrollDirection.Top;
                JObject j = new JObject();
                if (isElementOfScroll)
                {
                    j.Add("value", this.TabPage.GetBodyScroll(eScrollDirection, selector));
                    return j.ToString();
                }
                else
                {
                    j.Add("value", this.TabPage.GetBodyScroll(eScrollDirection, ""));
                }
                return j.ToString();
            });
        }
        public virtual string GetScroll()
        {
            return Wrapper(() =>
            {
                JObject j = new JObject();
                j.Add("scrollTop", this.TabPage.GetBodyScroll(eScrollDirection.Top));
                j.Add("scrollLeft", this.TabPage.GetBodyScroll(eScrollDirection.Left));
                return j.ToString();
            });
        }
        public virtual string GetTabDetail(string value = null)
        {
            return Wrapper(() =>
            {
                return this.TabPage.GetTabDetail(true).ToString();
            });
        }
        public virtual string GetTitle()
        {
            return Wrapper(() =>
            {
                this.TabPage.GetTabDetail(true);//实时获取一遍详情
                return this.TabPage.Title;
            });
        }
        public virtual string GetUrl()
        {
            return Wrapper(() =>
            {
                this.TabPage.GetTabDetail(true);//实时获取一遍详情
                return this.TabPage.Url;
            });
        }
        public virtual void GoBack()
        {
            Wrapper(() =>
            {
                this.TabPage.GoBack();
            });
        }
        public virtual void GoForward()
        {
            Wrapper(() =>
            {
                this.TabPage.GoForward();
            });
        }
        public virtual bool IsLoaded()
        {
            return Wrapper(() =>
             {
                 return this.TabPage.IsLoaded(0);
             });
        }
        public virtual void Maximized()
        {
            Wrapper(() =>
            {
                this.Browser.Maximized();
            });
        }
        public virtual void Minimized()
        {
            Wrapper(() =>
            {
                this.Browser.Minimized();
            });
        }
        public virtual string Navigate(string url, int timeout = 1000)
        {
            var model = new ParamNavigateModel()
            {
                url = UtilsHelper.ValidURL(url),
                RunParam = new Plugin.Core.Entities.RunParamModel
                {
                    TotalTime = timeout
                }
            };
            return Wrapper(() =>
             {
                 this.TabPage.Navigate(model);
                 this.TabPage.IsLoaded(timeout);
                 return this.TabPage.GetCompleteTabDetail(true).ToString();
             });
        }
        public virtual void ReLoad()
        {
            Wrapper(() =>
            {
                this.TabPage.Reload();
            });
        }
        public virtual void RemoveCookie(string url, string name = "")
        {
            var model = new ParamCookieModel() { url = url, name = name };
            Wrapper(() =>
            {
                this.TabPage.RemoveCookie(model);
            });
        }
        public virtual void Screenshot(string savePath = "")
        {
            Wrapper(() =>
            {
                var title = GetTitle();
                this.TabPage.SyncDocumentRectangle();
                ImageHelper.Screenshot(this.TabPage.DocumentRectangle, savePath);
            });
        }
        public virtual void Scroll(bool isElementOfScroll, int directionType, string scrollType, string selector, string targetSelect, int offSetX, int offSetY)
        {
            Wrapper(() =>
            {
                if (!isElementOfScroll)
                    selector = "";
                if (!scrollType.Equals("cuselement", StringComparison.OrdinalIgnoreCase))
                    targetSelect = "";
                this.TabPage.Scroll(directionType, selector, targetSelect, scrollType.ToLower(), offSetX, offSetY);
                //var direction = JObject.Parse(value);
                //foreach (var item in direction)
                //{
                //    int result = 0;
                //    int.TryParse(item.Value.ToString(), out result);
                //    if (item.Key == "scrollTop")
                //    {
                //        if (result > 0)
                //            this.TabPage.ScrollTop(result);
                //    }
                //    else if (item.Key == "scrollLeft")
                //    {
                //        if (result > 0)
                //            this.TabPage.ScrollTop(result);
                //    }
                //}
            });
        }
        public virtual void Scroll(string value)
        {
            Wrapper(() =>
            {
                var direction = JObject.Parse(value);
                foreach (var item in direction)
                {
                    int result = 0;
                    int.TryParse(item.Value.ToString(), out result);
                    if (item.Key == "scrollTop")
                    {
                        if (result > 0)
                            this.TabPage.ScrollTop(result);
                    }
                    else if (item.Key == "scrollLeft")
                    {
                        if (result > 0)
                            this.TabPage.ScrollTop(result);
                    }
                }
            });
        }
        public virtual void SetCookie(string url, string value)
        {
            ParamCookieModel model = new ParamCookieModel()
            {
                url = url ?? this.TabPage.Url,
                value = value
            };
            Wrapper(() =>
                {
                    this.TabPage.SetCookie(model);
                });
        }
        public virtual string StartCapture(string jsPath, string config = null, string url = null)
        {
            throw new NotImplementedException();
        }
        public virtual string StartConfig(string jsPath, string config = null, string url = null)
        {
            throw new NotImplementedException();
        }
        public virtual void Stop()
        {
            Wrapper(() =>
            {
                this.TabPage.StopLoad();
            });
        }
        public bool Wait(int timeout = 10000)
        {
            return Wrapper(() =>
            {
                DocumentReadyState readState = DocumentReadyState.Complete;
                //等待加载只使用complete
                //readState = readState | DocumentReadyState.Interactive;
                return this.TabPage.WaitToReadyState(readState, timeout);
            });
        }

        public virtual void StartInspectNetwork()
        {
            Wrapper(() =>
            {
                this.TabPage.StartInspectNetwork();
            });
        }

        public virtual string GetInspectNetwork(string type, string url, bool isRegex, string filePath)
        {
            return Wrapper(() => this.TabPage.GetInspectNetwork(type, url, isRegex, filePath));
        }

        public virtual void EndInspectNetwork()
        {
            Wrapper(() => this.TabPage.EndInspectNetwork());
        }
        public virtual string DownloadDialogExcute(ParamDownloaderModel param)
        {
            this.TabPage.Downloader.VerifyFilePath(param.saveFile);
            this.TabPage.Downloader.VerifyFileName(param.fileName);
            this.TabPage.Downloader.AddCreatedEvent(param);
            int buttonType = param.buttonType == 0 ? NativeMethods.SAVE_BUTTON_ID : NativeMethods.CANCEL_BUTTON_ID;
            return this.TabPage.Downloader.DownloadDialog(param.saveFile, param.fileName, param.interval, param.InputMode, param.SaveMode, param.timeout, param.waitDialogTime, buttonType);
        }
        public virtual void UploadDialogExcute(ParamFileUploaderModel param)
        {
            int buttonType = param.buttonType == 0 ? NativeMethods.SAVE_BUTTON_ID : NativeMethods.CANCEL_BUTTON_ID;
            this.TabPage.Downloader.UploaderDialog(param.uploadType, param.filePath, param.interval, param.InputMode, param.waitDialogTime, buttonType);
        }
    }
}

