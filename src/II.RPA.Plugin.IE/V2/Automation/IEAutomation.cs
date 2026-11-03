using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using II.RPA.Plugin.IE.V2.Automation.Core;
using II.RPA.Plugin.IE.V2.Automation.Provider;
using II.RPA.Plugin.IE.V2.Extentions;
using II.RPA.Screenshot.Plugin.UnmanagedHelpers;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using MSHTML;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SHDocVw;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace II.RPA.Plugin.IE.V2.Automation
{
    internal class IEAutomation
    {
        private static ILogger logger = Logger.OfContext();
        private DocumentProvider documentProvider = new DocumentProvider();
        private IEAutomation()
        {

        }
        #region 单例 全局点
        private readonly static object objIEAutomationLock = new object();
        private static IEAutomation instance = null;
        public static IEAutomation Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (objIEAutomationLock)
                    {
                        if (instance == null)
                        {
                            instance = new IEAutomation();
                        }
                    }
                }
                return instance;
            }
        }

        #endregion
        private CacheDocument GetRPADocument2ByIntPtr(IntPtr handle)
        {
            var _htmlDocument = documentProvider.GetIHTMLDocument2(handle);
            return _htmlDocument;
        }
        private IHTMLDocument2 GetDocument2ByPoint(Point point)
        {
            IntPtr hWnd = NativeMethods.WindowFromPoint(point);
            var IhDoc = GetRPADocument2ByIntPtr(hWnd).IHTMLDocument2;
            return IhDoc;
        }
        private IHTMLDocument2 GetDocument2ByIntPtr(IntPtr handle)
        {
            var _htmlDocument = GetRPADocument2ByIntPtr(handle);
            return _htmlDocument.IHTMLDocument2;
        }
        private IHTMLDocument2 GetDocument2ByTabID(int tabId)
        {
            var IhDoc = GetRPADocument2ByIntPtr((IntPtr)tabId);
            if (IhDoc == null)
            {
                logger.LogWarning($"intptr convert document is null, current intptr {tabId}");
                throw new IEException(eErrorMessageType.Error_PageNotFound);
            }
            return IhDoc.IHTMLDocument2;
        }
        private IHTMLWindow2 GetWindow2ByTabId(int tabId)
        {
            var IhDoc = GetRPADocument2ByIntPtr((IntPtr)tabId);
            return IhDoc.IHTMLWindow2;
        }
        private IHTMLElement ElementFromUniqueId(int tabId, string uniqueId)
        {
            var doc2 = GetDocument2ByTabID(tabId);
            var doc = doc2 as HTMLDocument;
            var element = Utils.ElementFromUniqueId(uniqueId, doc);
            if (element == null)
                throw new IEException(eErrorMessageType.Error_NotFound);
            return element;
        }
        private IWebBrowser2 webBrowser2 = null;
        private bool IsFrameElement(object o)
        {
            try
            {
                if (o != null)
                {
                    var element = o as IHTMLElement;
                    var tagName = element.tagName.ToLower();
                    return tagName == "frame" || tagName == "iframe";
                }
                return false;
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "IsFrameElement Error");
                return false;
            }
        }
        #region [Find Element]
        private IHTMLWindow2 GetFrameWindow2(HTMLFrameElement frameElement)
        {
            IHTMLWindow2 win = null;
            win = WebWindowHelper.WebWindow(frameElement.contentWindow);
            return win;
        }
        [DisplayName("tabs.ClearIHtmlObject")]
        public void ClearIHtmlObject(JObject paramObject)
        {
            documentProvider.ClearRpaIHTMLDocument2Dic();
        }

        public List<SelectorProps> SubSelector(bool isAllNode, ref List<SelectorProps> selector)
        {
            var listSelector = new List<SelectorProps>();
            while (selector.Count > 0)
            {
                var first = selector.First();
                selector.Remove(first);
                if (first.Name == "tab")
                {
                    continue;
                }
                if (isAllNode)
                {
                    if ((first.Name == "iframe" || first.Name == "frame") && first.Accurate != "true")
                    {
                        continue;
                    }
                    listSelector.Add(first);
                }
                else
                {
                    if (first.Accurate != "true")
                    {
                        continue;
                    }
                    listSelector.Add(first);
                }
                if (first.Name == "iframe" || first.Name == "frame")
                {
                    break;
                }
            }
            return listSelector;
        }
        public List<IHTMLElement> FindElements(IHTMLWindow2 win, string selector, string cssSelector, string version, string parentId, bool discernHidden)
        {
            List<IHTMLElement> elements = new List<IHTMLElement>();
            logger.LogInformation("windows  :" + (win == null).ToString());
            var result = Utils.QuerySelector(win, selector, version, cssSelector, parentId, discernHidden);
            logger.LogInformation($"element Count {result}");
            //https://stackoverflow.com/questions/10903750/access-javascript-array-elements-from-c-sharp-via-webbrowser
            try
            {
                if (int.TryParse(result?.ToString(), out int elementIndex) || result == null)
                {
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    if (elementIndex != 0)
                    {
                        for (int idx = 0; idx < elementIndex; idx++)
                        {
                            try
                            {
                                var ele = Utils.GetElementByIndex(win, idx);
                                elements.Add(ele);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex);
                            }
                        }
                    }
                    else
                    {
                        List<IHTMLElement> eles;
                        logger.LogDebug($"selector version {version}");
                        if (RPAUtils.CompareVersionNumber(version, "1.2") >= 0)
                        {
                            var temp = stopWatch.ElapsedMilliseconds;
                            eles = LowKernelQuerySelector.GetInstance().QuerySelectorAll(selector, win.document, parentId);
                            logger.LogInformation($"QuerySelectorAll Count {eles.Count} stopWatch.ElapsedMilliseconds - temp {stopWatch.ElapsedMilliseconds - temp}");
                        }
                        else
                        {
                            eles = Utils.FindIHTMLElement(selector, win.document);
                        }
                        if (eles.Count > 0)
                        {
                            elements.AddRange(eles.Where(ele =>
                            {
                                /*这段逻辑是错误的，但是为了数据兼容留下了*/
                                var ele2currentStyle = (ele as IHTMLElement2).currentStyle;
                                return ele2currentStyle.width + ele2currentStyle.paddingLeft + ele2currentStyle.paddingRight + ele2currentStyle.borderLeftWidth + ele2currentStyle.borderRightWidth > 0
                                && ele2currentStyle.height + ele2currentStyle.paddingTop + ele2currentStyle.paddingBottom + ele2currentStyle.borderTopWidth + ele2currentStyle.borderBottomWidth > 0;

                                /*这段逻辑是正确的，但是正确的代码会带来与736及之前版本不样的结果，待后续做IE内容重构的时候再做支持*/
                                //var style = (ele as IHTMLElement2).currentStyle;
                                //double width = ParseCssPx(style.width)
                                //             + ParseCssPx(style.paddingLeft)
                                //             + ParseCssPx(style.paddingRight)
                                //             + ParseCssPx(style.borderLeftWidth)
                                //             + ParseCssPx(style.borderRightWidth);

                                //double height = ParseCssPx(style.height)
                                //              + ParseCssPx(style.paddingTop)
                                //              + ParseCssPx(style.paddingBottom)
                                //              + ParseCssPx(style.borderTopWidth)
                                //              + ParseCssPx(style.borderBottomWidth);

                                //return width > 0 && height > 0;
                            }));
                        }
                    }

                    stopWatch.Stop();
                    logger.LogInformation($"get element time {stopWatch.ElapsedMilliseconds}");
                }
                else
                {
                    if (elements.Count > 0)
                        elements.Clear();
                    elements.Add(result as IHTMLElement);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            return elements;
        }
        private static double ParseCssPx(string value)
        {
            if (string.IsNullOrEmpty(value) || value == "auto")
                return 0;

            // 去掉 "px" 等后缀，只保留数字部分
            var match = System.Text.RegularExpressions.Regex.Match(value, @"^[\d\.]+");
            if (match.Success && double.TryParse(match.Value, out var result))
                return result;

            return 0;
        }
        public IHTMLWindow2 FindIHTMLWindow2s(IHTMLElement element)
        {
            logger.LogInformation($"transform frame element");
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            HTMLFrameElement frameElement = element as HTMLFrameElement;
            var win = GetFrameWindow2(frameElement);
            Utils.InjectJavaScript(win);
            stopWatch.Stop();
            logger.LogInformation($"get frameElement time {stopWatch.ElapsedMilliseconds}");
            return win;
        }
        public List<IHTMLWindow2> FindIHTMLWindow2s(Dictionary<string, IHTMLElement> framesDic, IHTMLWindow2 defWin)
        {
            logger.LogInformation($"FindIHTMLWindow2s framesDic");
            List<IHTMLWindow2> windows = new List<IHTMLWindow2>();
            if (framesDic == null || framesDic.Count == 0)
            {
                windows.Add(defWin);
                return windows;
            }
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            foreach (var item in framesDic)
            {
                if (Utils.IsFrameElement(item.Value))
                {
                    logger.LogInformation($"transform frame element");
                    var win = FindIHTMLWindow2s(item.Value);
                    var frameIndex = Utils.GetFrameIndex(win);
                    windows.Add(win);
                }
            }
            stopWatch.Stop();
            logger.LogInformation($"get frameElement time {stopWatch.ElapsedMilliseconds}");
            return windows;
        }
        public List<IHTMLElement> QuerySelectorAll(List<SelectorProps> selector, string version, bool isAllNode, IHTMLWindow2 window, List<string> frameList, string cssSelector, ref Dictionary<string, IHTMLElement> query, string parentId = "", bool discernHidden = true)
        {
            List<SelectorProps> selectorClone = new List<SelectorProps>(selector);
            List<string> frameListClone = new List<string>(frameList);
            List<IHTMLElement> elements = new List<IHTMLElement>();
            var listSelector = SubSelector(isAllNode, ref selectorClone);
            var strSelector = JsonConvert.SerializeObject(listSelector);
            elements = FindElements(window, strSelector, cssSelector, version, parentId, discernHidden);
            if (elements != null && elements.Count > 0)
            {
                for (int i = 0; i < elements.Count; i++)
                {
                    var firstElement = elements[i];
                    if (Utils.IsFrameElement(firstElement) && selectorClone.Count > 0 && selectorClone.Count(s => s.Accurate == "true") > 0)
                    {
                        var framewin = FindIHTMLWindow2s(firstElement);
                        var frameIndex = Utils.GetFrameIndex(framewin);
                        if (framewin == null || frameIndex == -1)
                            continue;
                        frameListClone.Add(frameIndex.ToString());
                        QuerySelectorAll(selectorClone, version, isAllNode, framewin, frameListClone, cssSelector, ref query, parentId, discernHidden);
                        frameListClone.RemoveAt(frameListClone.Count - 1);
                    }
                    else
                    {
                        query.Add(Utils.SetUniqueId(window, frameListClone, firstElement), firstElement);
                        //break;
                    }
                }
            }
            return elements;
        }
        [STAThread]
        public Dictionary<string, IHTMLElement> QuerySelectorAll(List<SelectorProps> selector, string version, string cssSelector = "", IHTMLWindow2 win = null, int index = 1, string parentId = "", bool discernHidden = true)
        {
            var isAllNode = double.TryParse(version, out var doubleVersion);
            if (isAllNode)
            {
                isAllNode = doubleVersion > 1.0;
            }

            List<IHTMLElement> elements = null;
            List<string> frameList = new List<string>();
            frameList.Add("0");

            Dictionary<string, IHTMLElement> query = new Dictionary<string, IHTMLElement>();
            elements = QuerySelectorAll(selector, version, isAllNode, win, frameList, cssSelector, ref query, parentId, discernHidden);

            if (index == -1)
            {
                return query;
            }
            else
            {
                if (index < query.Count)
                {
                    var q = query.ElementAt(index);
                    var catchdic = new Dictionary<string, IHTMLElement>();
                    catchdic.Add(q.Key, q.Value);
                    return catchdic;
                }
            }
            return null;
        }

        /// <summary>
        /// IE tab数据封装
        /// </summary>
        /// <param name="web"></param>
        /// <returns></returns>
        //private JToken IEMessage(IWebBrowser2 web)
        //{
        //    IEModel ieModel = new IEModel();
        //    try
        //    {
        //        if (web != null)
        //        {
        //            ieModel.id = web.HWND.ToString();
        //            ieModel.title = web.LocationName;
        //            ieModel.url = web.LocationURL;
        //            ieModel.status = web.ReadyState.ToString().Replace("READYSTATE_", "");
        //        }
        //        logger.LogDebug($"Browser {(JToken)JsonConvert.SerializeObject(ieModel)}");
        //        return (JToken)JsonConvert.SerializeObject(ieModel);
        //    }
        //    catch (Exception)
        //    {
        //        return null;
        //    }
        //}
        private JToken IEMessage(IHTMLDocument2 doc, int tabID)
        {
            IEModel ieModel = new IEModel();
            try
            {
                if (doc != null)
                {
                    ieModel.id = tabID;
                    ieModel.title = doc.title;
                    ieModel.url = doc.url;
                    ieModel.status = doc.readyState.ToString().Replace("READYSTATE_", "");
                }
                logger.LogDebug($"Browser {(JToken)JsonConvert.SerializeObject(ieModel)}");
                return (JToken)JsonConvert.SerializeObject(ieModel);
            }
            catch (Exception)
            {
                return null;
            }
        }
        #endregion

        #region 浏览器操作 相关
        [DisplayName("tabs.closeAll")]
        public void CloseAll(JObject paramObject)
        {
            try
            {
                var wbSearched = new ShellWindowsClass().Cast<IWebBrowser2>();
                foreach (var wb in wbSearched)
                {
                    if (wb.Document as IHTMLDocument2 != null)
                    {
                        wb.Quit();
                        Marshal.ReleaseComObject(wb);

                    }
                }

            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "IE CloseAll Error");
            }
            logger.LogDebug("准备关闭 IE 浏览器");
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                System.Diagnostics.Process p = new System.Diagnostics.Process();
                p.StartInfo.FileName = "taskkill";
                p.StartInfo.Arguments = "/F /IM iexplore.exe";
                p.StartInfo.UseShellExecute = false;    //是否使用操作系统shell启动
                p.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
                p.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
                p.StartInfo.RedirectStandardError = true;//重定向标准错误输出
                p.StartInfo.CreateNoWindow = true;//不显示程序窗口
                p.Start();//启动程序
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            while (UiCommon.GetProcessesBySessionId("iexplore").Length > 0 && stopWatch.Elapsed.TotalMilliseconds < 10000)
            {
                Thread.Sleep(1000);
            }
            if (stopWatch.Elapsed.TotalMilliseconds > 10000)
            {
                stopWatch.Stop();
                logger.LogDebug("关闭 IE 浏览器超时或异常");
                throw new IEException(eErrorMessageType.Error_CloseAllFail);
            }
            stopWatch.Stop();
        }
        [DisplayName("windows.minimized")]
        public void Minimized(JObject paramObject)
        {
            IntPtr handle = NativeMethods.GetForegroundWindow();
            NativeMethods.ShowWindow(webBrowser2 == null ? handle : (IntPtr)webBrowser2.HWND, NativeMethods.SW_MINIMIZE);
        }
        [DisplayName("windows.maximize")]
        public void Maximized(JObject paramObject)
        {
            IntPtr handle = NativeMethods.GetForegroundWindow();
            NativeMethods.ShowWindow(webBrowser2 == null ? handle : (IntPtr)webBrowser2.HWND, NativeMethods.SW_MAXIMIZE);
        }
        [DisplayName("cookies.getAll")]
        public JToken GetCookies(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsGetCookiesModel>();
            JArray arrayCookie = new JArray();
            try
            {
                if (webBrowser2 != null)
                {
                    IHTMLDocument2 doc = (IHTMLDocument2)webBrowser2.Document;
                    string cookies = doc.cookie;
                    foreach (var cookie in cookies.Split(';'))
                    {
                        var values = cookie.Split('=');
                        if (values.Count() > 1)
                        {
                            var prop = new
                            {
                                name = values[0],
                                value = values[1]
                            };
                            arrayCookie.Add(JsonConvert.DeserializeObject(prop.ToJson()));
                        }
                        else
                        {
                            var prop = new
                            {
                                name = values[0]
                            };
                            arrayCookie.Add(JsonConvert.DeserializeObject(prop.ToJson()));
                        }
                    }
                    if (!string.IsNullOrEmpty(paramsModel.Name))
                    {
                        foreach (var cookie in arrayCookie)
                        {
                            if (cookie["name"].ToString().Equals(paramsModel.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                return cookie;
                            }
                        }
                        return string.Empty;
                    }

                    return arrayCookie.ToJson();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            return arrayCookie.ToJson();
        }
        [DisplayName("cookies.set")]
        public void SetCookie(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsSetCookieModel>();

            if (webBrowser2 != null)
            {
                IHTMLDocument2 doc = (IHTMLDocument2)webBrowser2.Document;
                doc.cookie = $"{paramsModel.name}={paramsModel.value}";
            }
        }
        public void RemoveCookie(string url = null, string name = null)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region 获取tab信息 相关

        [DisplayName("tabs.getDetail")]
        public JToken GetTabDetail(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            var document = GetDocument2ByTabID(paramsModel.TabId);
            webBrowser2 = FindWebBrowser.GetIEWebBrowser(document);
            return IEMessage(document, paramsModel.TabId);
        }
        public JToken GetAllTabIds()
        {
            return this.GetTabIds(new JObject { });
        }
        public JToken NormalTabIds()
        {
            return this.GetTabIds(new JObject {
                { "windowType", "normal" }
            });
        }
        public JToken GetActiveTabIds()
        {
            return this.GetTabIds(new JObject {
                { "active", true }
            });
        }
        [DisplayName("tabs.query")]
        public JToken GetTabIds(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsGetTabIdsModel>();
            var handles = DocumentHelper.GetIEDocumentWnds(paramsModel.wndHandles, paramsModel.active);
            return handles.Select(h => (int)h).ToJson();
        }
        public int GetTotalHeight(int tabId)
        {
            return this.ExecuteScriptOnFrame(tabId, "document.body.scrollHeight").Value<int>();
        }
        public int GetTotalWidth(int tabId)
        {
            return this.ExecuteScriptOnFrame(tabId, "document.body.scrollWidth").Value<int>();
        }
        public JToken ExecuteScriptOnFrame(int tabId, string code)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region tabpage操作 相关
        [DisplayName("elements.executeScript")]
        public string Executejs(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsExecutejsModel>();
            IHTMLDocument2 IhDoc = GetDocument2ByTabID(paramsModel.TabId);
            if (!string.IsNullOrEmpty(paramsModel.ElementId))
            {
                var element = ElementFromUniqueId(paramsModel.TabId, paramsModel.ElementId);
                IhDoc = element.document;
            }
            IHTMLWindow2 window = WebWindowHelper.WebDocumentWindow(IhDoc as HTMLDocument);
            return Utils.InvokeScript(window, paramsModel.code);
        }

        [DisplayName("tabs.switch")]
        public JToken SwitchTab(JObject paramObject)
        {
            //IE浏览器切换tab是使用模拟点击方式，因此调用ActiveTab
            return ActiveTab(paramObject);
        }
        [DisplayName("tabs.active")]
        public JToken ActiveTab(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            var IhDoc = GetDocument2ByTabID(paramsModel.TabId);
            var browser = FindWebBrowser.GetIEWebBrowser(IhDoc);
            if (browser != null)
            {
                try
                {
                    var hWnd = (IntPtr)browser.HWND;
                    new TabActivator(hWnd, IhDoc.title).SelectTabItem(paramsModel.TabId);
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "get webbrowser handle error");
                }
            }
            return string.Empty;
        }
        [DisplayName("tabs.closeRight")]
        public virtual void CloseRight(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            var IhDoc = GetDocument2ByTabID(paramsModel.TabId);
            var browser = FindWebBrowser.GetIEWebBrowser(IhDoc);
            var internetExplorer = browser as InternetExplorer;
            var wbSearched = new ShellWindowsClass().Cast<InternetExplorer>().ToList();
            wbSearched.Reverse();
            foreach (var item in wbSearched)
            {
                if (!item.Equals(browser))
                {
                    try
                    {
                        var doc = item.Document as IHTMLDocument;
                        if (doc != null)
                            item.Quit();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex.Message);
                    }
                }
                else
                {
                    break;
                }
            }
        }
        [DisplayName("tabs.closeOther")]
        public void CloseOther(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            var IhDoc = GetDocument2ByTabID(paramsModel.TabId);
            var browser = FindWebBrowser.GetIEWebBrowser(IhDoc);
            var internetExplorer = browser as InternetExplorer;
            var wbSearched = new ShellWindowsClass().Cast<InternetExplorer>().ToList();
            wbSearched.Reverse();
            foreach (var item in wbSearched)
            {

                if (!item.Equals(browser))
                {
                    try
                    {
                        var doc = item.Document as IHTMLDocument;
                        if (doc != null)
                            item.Quit();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex.Message);
                    }

                }
            }
        }
        [DisplayName("tabs.closeList")]
        public void CloseList(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamMultipleCloseModel>();
            foreach (var tabId in paramsModel.TabIds)
            {
                var IhDoc = GetDocument2ByTabID(tabId);
                var browser = FindWebBrowser.GetIEWebBrowser(IhDoc);
                browser.Quit();
            }
        }

        [DisplayName("tabs.close")]
        public void Close(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            var IhDoc = GetDocument2ByTabID(paramsModel.TabId);
            var browser = FindWebBrowser.GetIEWebBrowser(IhDoc);
            if (browser != null)
            {
                browser.Quit();
            }
        }
        [DisplayName("tabs.scroll")]
        public JToken Scroll(JObject paramObject)
        {
            try
            {
                var paramsModel = paramObject.ToObject<ParamsScrollModel>();
                var window = GetWindow2ByTabId(paramsModel.TabId);
                Utils.InvokeScript(window, "IIRPA.api.scroll", "", paramsModel.direction.ToString().ToLower(), paramsModel.value.ToString());
            }
            catch (Exception e)
            {
                logger.LogError(e);
                throw new Exception(e.Message);
            }
            return null;
        }
        [DisplayName("tabs.scroll_V2")]
        public JToken Scroll_V2(JObject paramObject)
        {
            try
            {
                var paramsModel = paramObject.ToObject<ParamsScrollModel>();
                var document2 = GetDocument2ByTabID(paramsModel.TabId);
                if (document2 != null)
                {
                    var d = WebWindowHelper.WebWindow(document2.parentWindow);
                    Utils.InjectJavaScript(d);
                    Utils.InvokeScript(d, "Scroll", paramsModel.elementId, paramsModel.direction.ToString().ToLower(), paramsModel.scrollType, paramsModel.targetElementId, paramsModel.offSetX.ToString(), paramsModel.offSetY.ToString());
                }
            }
            catch (Exception e)
            {
                logger.LogError(e);
                throw new Exception(e.Message);
            }
            return null;
        }
        [DisplayName("tabs.getScroll")]
        public JToken GetBodyScroll(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsGetBodyScrollModel>();
            var document2 = GetDocument2ByTabID(paramsModel.TabId);
            if (document2 != null)
            {
                var d = WebWindowHelper.WebWindow(document2.parentWindow);
                Utils.InjectJavaScript(d);
                return Utils.InvokeScript(d, "GetScroll_V2", paramsModel.elementId, paramsModel.Direction) ?? "0";
            }
            return null;
        }
        [DisplayName("tabs.scrollTo")]
        public void ScrollTo(int tabId, int top, int left)
        {
            return;
        }
        [DisplayName("tabs.getDocument")]
        public JToken GetDocument(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            var IhDoc = GetDocument2ByTabID(paramsModel.TabId);
            if (IhDoc != null)
                return IhDoc.body.innerHTML;
            else
                return string.Empty;
        }
        [DisplayName("tabs.goBack")]
        public void GoBack(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            var IhDoc = GetDocument2ByTabID(paramsModel.TabId);
            var browser = FindWebBrowser.GetIEWebBrowser(IhDoc);
            if (browser != null)
            {
                browser.GoBack();
            }
        }
        [DisplayName("tabs.goForward")]
        public void GoForward(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            var IhDoc = GetDocument2ByTabID(paramsModel.TabId);
            var browser = FindWebBrowser.GetIEWebBrowser(IhDoc);
            if (browser != null)
            {
                browser.GoForward();
            }
        }
        [DisplayName("tabs.navigate")]
        public void Navigate(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsNavigateModel>();
            var IhDoc = GetDocument2ByTabID(paramsModel.TabId);
            var browser = FindWebBrowser.GetIEWebBrowser(IhDoc);
            if (browser != null)
            {
                browser.Navigate(paramsModel.Url);
            }
        }
        [DisplayName("tabs.reload")]
        public void Reload(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            var IhDoc = GetDocument2ByTabID(paramsModel.TabId);
            var browser = FindWebBrowser.GetIEWebBrowser(IhDoc);
            if (browser != null)
            {
                browser.Refresh();
            }
        }
        [DisplayName("tabs.stop")]
        public JToken StopLoad(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            var IhDoc = GetDocument2ByTabID(paramsModel.TabId);
            var browser = FindWebBrowser.GetIEWebBrowser(IhDoc);
            if (browser != null)
            {
                browser.Stop();
            }
            return string.Empty;
        }

        #region 数据采集
        public List<string> BuildBatchSelector(string uniqueIds, string attributeName = "", IHTMLWindow2 win2 = null)
        {
            try
            {
                List<string> batchSelector = new List<string>();
                HTMLDocument docCache = win2.document as HTMLDocument;
                HTMLDocument doc = win2.document as HTMLDocument;
                var firstUniqueId = uniqueIds.Split(',')[0];
                var uniqueArray = firstUniqueId.Split('/');
                for (int i = 1; i < uniqueArray.Length - 2; i++)
                {
                    try
                    {
                        var frame = Utils.GetChildFrameByIndex(doc, Convert.ToInt32(uniqueArray[i]));
                        if (frame != null)
                        {
                            win2 = WebWindowHelper.WebFrameWindow(frame);
                            doc = win2.document as HTMLDocument;
                        }

                    }
                    catch (Exception)
                    {
                    }
                }
                JArray selector = new JArray();
                //获取浏览器webBrowser
                var element = Utils.ElementFromUniqueId(firstUniqueId, docCache);
                var batchPaths = Utils.BuildBatchSelector(win2, uniqueIds, "-1", attributeName);
                foreach (var paths in batchPaths)
                {
                    logger.LogDebug($"IE BuildSelector: {paths}");
                    if (!string.IsNullOrEmpty(attributeName))
                    {
                        batchSelector.Add(paths);
                        //return element.getAttribute(attributeName);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(paths))
                        {
                            var docselector = JsonConvert.DeserializeObject(paths);
                            selector = JArray.Parse(docselector.ToString());
                        }
                        else
                            throw new Exception("selector is null");
                        //var element = Utils.ElementFromUniqueId(uniqueId, docCache);

                        //BuildFrameSelector(element, selector);
                        BuildFrameSelector(firstUniqueId, selector, element);

                        batchSelector.Add(BuildSelectorItem(selector, doc as IHTMLDocument2));
                    }
                }
                return batchSelector;
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "IE BuildSelector Error");
                return null;
            }
        }

        [DisplayName("tabs.queryElementsAll")]
        public JToken QueryElementsAll(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsQueryElementsAllModel>();
            var IhDoc = documentProvider.GetIHTMLDocument2((IntPtr)paramsModel.TabId);
            var result = Utils.QueryElementsAll(IhDoc.IHTMLWindow2, "[0]", paramsModel.tagNames, paramsModel.attrKeywords);
            return result.ToString();
        }
        [DisplayName("tabs.queryCollectionData")]
        public JToken QueryCollectionData(JObject paramObject)
        {
            int isDataCollect = 1;
            List<List<string>> collectDataList = new List<List<string>>();
            var paramsModel = paramObject.ToObject<ParamsQueryCollectionDataModel>();
            var selectorDataList = JsonConvert.DeserializeObject<List<CollectDataSelectorData>>(paramsModel.selectorDataList.ToString());
            var selectorVersion = selectorDataList.FirstOrDefault()?.Version;
            //var IhDoc = FindDocument.GetDocumentByIntPtr((IntPtr)paramsModel.TabId);
            var IhDoc = documentProvider.GetIHTMLDocument2((IntPtr)paramsModel.TabId);

            for (int i = 0; i < selectorDataList.Count; i++)
            {
                List<string> collectData;
                try
                {
                    var selectorData = selectorDataList[i];
                    IHTMLWindow2 win = IhDoc.IHTMLWindow2;
                    var dics = QuerySelectorAll(selectorData.Selector, selectorVersion, "", win, -1);
                    JArray elements = new JArray();
                    if (dics != null)
                    {
                        List<string> elementIds = dics.Keys.ToList<string>();
                        if (elementIds.Count == 0)
                            throw new IEException(eErrorMessageType.Error_NotFoundElement);
                        if (isDataCollect != 0)
                        {
                            string elementIdsStr = string.Join(",", elementIds);
                            var resultSelectors = BuildBatchSelector(elementIdsStr, selectorData.CollectAttrName, win);
                            foreach (var content in resultSelectors)
                            {
                                elements.Add(content);
                            }
                        }
                        else
                        {
                            return elementIds.ToString();
                        }
                    }

                    if (elements == null)
                    {
                        collectData = new List<string>();
                    }
                    else
                    {
                        collectData = elements.ToObject<List<string>>();
                    }

                }
                catch (RPAException ex)
                {
                    //eErrorMessageType.Error_NotFoundElement
                    if (ex.ErrorMessageType == eErrorMessageType.Error_NotFoundElement)
                    {
                        collectData = new List<string>();
                    }
                    else
                    {
                        throw ex;
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "获取采集数据异常");
                    throw ex;
                }
                collectDataList.Add(collectData);

            }
            return JsonConvert.SerializeObject(collectDataList);
        }

        [DisplayName("tabs.queryCollectionDataV2")]
        public JToken QueryCollectionData_V2(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsQueryCollectionDataV2Model>();
            var win = GetWindow2ByTabId(paramsModel.TabId);
            var frmesDic = QueryCollectionData_FindFrame(paramsModel, win);
            List<IHTMLWindow2> windows = FindIHTMLWindow2s(frmesDic, win);
            foreach (var item in windows)
            {
                IHTMLElement lastElement = this.GetAndScrollLastElement(item, paramsModel);
                if (lastElement == null)
                {
                    continue;
                }
                var retObj = Utils.queryCollectionDataV2(item, paramsModel.ToJsonNoneFormating());
                return JToken.FromObject(retObj);
            }
            return "[]";
        }
        [DisplayName("tabs.scrollAndCollectionData")]
        public JToken ScrollAndCollectionData(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsQueryCollectionDataV2Model>();
            Dictionary<IHTMLWindow2, string> winFromTreeDic = new Dictionary<IHTMLWindow2, string>();
            List<IHTMLWindow2> windows;
            var markedElementId = paramsModel.markedElementId;
            if (string.IsNullOrEmpty(markedElementId))
            {
                var win = GetWindow2ByTabId(paramsModel.TabId);
                var framesDic = QueryCollectionData_FindFrame(paramsModel, win);
                if (framesDic?.Count > 0)
                {
                    foreach (var item in framesDic)
                    {
                        var uniqueIdArray = item.Key.Split('/');
                        var frameList = uniqueIdArray.Take(uniqueIdArray.Length - 2).ToList();
                        var frameWin = FindIHTMLWindow2s(item.Value);
                        var frameIndex = Utils.GetFrameIndex(frameWin);
                        frameList.AddRange(new[] { frameIndex.ToString(), "-1", "custom" });
                        winFromTreeDic.Add(frameWin, string.Join("/", frameList));
                    }
                }
                else
                {
                    winFromTreeDic.Add(win, "0/-1/custom");
                }
            }
            else
            {
                var IhDoc = GetDocument2ByTabID(paramsModel.TabId) as HTMLDocument;
                IHTMLWindow2 window = WebWindowHelper.WebElementWindow(paramsModel.markedElementId, IhDoc);
                winFromTreeDic.Add(window, markedElementId);
            }
            //IE瀑布流滚动一次 是去一次，滚动等待设置为0 
            paramsModel.collectionConfig.ScrollVerticalDelay = 0;
            foreach (var item in winFromTreeDic)
            {
                IHTMLElement lastElement = this.GetAndScrollLastElement(item.Key, paramsModel);
                Task.Delay(100).Wait();
                if (lastElement == null)
                {
                    continue;
                }
                var retObj = Utils.queryCollectionDataV2(item.Key, paramsModel.ToJsonNoneFormating());
                return new JObject
                {
                    { "data", JToken.FromObject(retObj) },
                    { "markedElementId", ElementFrom(item.Value, lastElement) }
                };
            }
            return new JObject
                {
                    { "data", new JArray() },
                    { "markedElementId", "" }
                };
        }
        private Dictionary<string, IHTMLElement> QueryCollectionData_FindFrame(ParamsQueryCollectionDataV2Model paramsModel, IHTMLWindow2 win = null)
        {
            win = win ?? GetWindow2ByTabId(paramsModel.TabId);
            Utils.InjectJavaScript(win);

            var isTable = paramsModel.collectionConfig.TableSelector != null && paramsModel.collectionConfig.TableSelector.Count > 0;
            var selector = isTable ? paramsModel.collectionConfig.TableSelector : paramsModel.collectionConfig.CommonSelector;
            var iframeTags = ",iframe,frame,";
            var iframeLastIdx = selector.FindLastIndex(sp => iframeTags.IndexOf($",{sp.Name},", StringComparison.OrdinalIgnoreCase) >= 0);
            if (iframeLastIdx >= 0)
            {
                var iframeSelector = selector.Take(iframeLastIdx + 1).ToList();
                selector.RemoveRange(0, iframeLastIdx + 1);
                var eleDic = QuerySelectorAll(iframeSelector, paramsModel.collectionConfig.SelectorVersion, "", win, -1);
                if (eleDic.Count == 0)
                {
                    throw new IEException(eErrorMessageType.Error_NotFoundElement);
                }
                return eleDic;
            }

            return new Dictionary<string, IHTMLElement>();
        }



        private IHTMLElement GetAndScrollLastElement(IHTMLWindow2 window, ParamsQueryCollectionDataV2Model paramsModel)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Stopwatch stopwatch_total = Stopwatch.StartNew();
            IHTMLElement lastElement = null;
            try
            {
                var isTable = paramsModel.collectionConfig.TableSelector != null && paramsModel.collectionConfig.TableSelector.Count > 0;
                List<SelectorProps> selector;
                if (isTable)
                {
                    selector = paramsModel.collectionConfig.TableSelector;
                }
                else
                {
                    selector = paramsModel.collectionConfig.CommonSelector;
                    if (paramsModel.collectionConfig.ColumnSelectorArray.Count > 0)
                    {
                        selector = selector.Concat(paramsModel.collectionConfig.ColumnSelectorArray[0]).ToList();
                    }
                }
                var selectorStr = selector.ToJsonNoneFormating();
                var scroll = paramsModel.colorGuidanceConfig == null ? "true" : "false";
                int scrollTop = -100, scrollHeight = -100;
                var scrollType = paramsModel.collectionConfig.ScrollType;
                do
                {
                    Task.Delay(100).Wait();
                    if (lastElement == null || scrollType == ScrollType.AutoScrollToBottom)
                    {
                        var _lastElement = Utils.getAndScrollLastElement(window, selectorStr, paramsModel.collectionConfig.SelectorVersion, ((int)scrollType).ToString());
                        if (_lastElement == null)
                        {
                            continue;
                        }
                        if (lastElement != _lastElement)
                        {
                            lastElement = _lastElement;
                            stopwatch.Restart();
                        }
                    }
                    if (paramsModel.colorGuidanceConfig != null || scrollType == ScrollType.NoScroll)
                    {
                        break;
                    }
                    if (scrollType == ScrollType.AutoScrollToBottom)
                    {
                        continue;
                    }
                    var infoRet = Utils.InvokeScript(window, "IIRPA.dom.areaInfo()");
                    var getDomAreaInfo = JObject.Parse(infoRet);
                    var curScrollTop = getDomAreaInfo.Value<int>("scrollTop");
                    var curScrollHeight = getDomAreaInfo.Value<int>("scrollHeight");

                    logger.LogDebug($"curScrollTop：{curScrollTop},curScrollHeight：{curScrollHeight},scrollHeight：{scrollHeight},scrollTop：{scrollTop}");

                    if (scrollHeight != curScrollHeight || scrollTop != curScrollTop)
                    {
                        scrollHeight = curScrollHeight;
                        scrollTop = curScrollTop;
                        //滚动一次
                        Utils.InvokeScript(window, "scrollToTop", "", (curScrollTop + 500).ToString());
                        stopwatch.Restart();
                    }
                } while (stopwatch.ElapsedMilliseconds < paramsModel.collectionConfig.ScrollVerticalDelay);
                logger.LogDebug($"GetAndScrollLastElement stopwatch_total：{stopwatch_total.ElapsedMilliseconds}ms");
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                stopwatch.Stop();
                stopwatch_total.Stop();
            }
            return lastElement;
        }

        #endregion

        #endregion

        #region 构建selector 相关
        [DisplayName("elements.buildCssSelector")]
        public JToken BuildCssSelector(JObject paramObject)
        {
            try
            {
                var paramsModel = paramObject.ToObject<ParamsModel>();
                var IhDoc = GetDocument2ByTabID(paramsModel.TabId);
                var doc = IhDoc as HTMLDocument;
                var win2 = WebWindowHelper.WebDocumentWindow(doc);
                var uniqueArray = paramsModel.ElementId.Split('/');
                for (int i = 1; i < uniqueArray.Length - 2; i++)
                {
                    try
                    {
                        var frame = Utils.GetChildFrameByIndex(doc, Convert.ToInt32(uniqueArray[i]));
                        if (frame != null)
                        {
                            win2 = WebWindowHelper.WebFrameWindow(frame);
                            doc = win2.document as HTMLDocument;
                        }
                    }
                    catch (Exception e)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "IE BuildCssSelector Error");
                    }
                }
                JArray selector = new JArray();
                //获取浏览器webBrowser
                var cssSelector = Utils.BuildCssSelector(win2, paramsModel.ElementId, "-1");
                JObject cssSelectorPath = new JObject();
                cssSelectorPath.Add("path", cssSelector);
                return cssSelectorPath;
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "IE BuildCssSelector Error");
                return null;
            }
        }
        [DisplayName("elements.buildSelector")]
        public JToken BuildSelector(JObject paramObject)
        {
            try
            {
                var paramsModel = paramObject.ToObject<ParamsBuildSelectorAllModel>();
                if (paramsModel.ValidNodeAttributes != null && Utils.LastInjectIEVersion <= 8)
                {
                    logger.LogInformation($"IEVersion:{Utils.LastInjectIEVersion}，不执行精简结构生成 ");
                    return null;
                }

                var IhDoc = GetDocument2ByTabID(paramsModel.TabId);
                var doc = IhDoc as HTMLDocument;
                var win2 = WebWindowHelper.WebDocumentWindow(doc);

                HTMLDocument docCache = win2.document as HTMLDocument;
                //HTMLDocument doc = win2.document as HTMLDocument;
                var uniqueArray = paramsModel.ElementId.Split('/');
                for (int i = 1; i < uniqueArray.Length - 2; i++)
                {
                    try
                    {
                        var frame = Utils.GetChildFrameByIndex(doc, Convert.ToInt32(uniqueArray[i]));
                        if (frame != null)
                        {
                            win2 = WebWindowHelper.WebFrameWindow(frame);
                            doc = win2.document as HTMLDocument;
                        }
                        //_document = doc;
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug("IE BuildSelector FindFirame Error: " + ex);
                    }
                }
                JArray selector = new JArray();
                //获取浏览器webBrowser
                var paths = Utils.BuildSelector(win2,
                                                paramsModel.ElementId,
                                                "-1",
                                                paramsModel.innerTextFirst,
                                                paramsModel.rawData,
                                                paramsModel.excludeInnerText,
                                                paramsModel.excludeHiddenEle,
                                                paramsModel.ValidNodeAttributes
                                                );
                logger.LogDebug("IE BuildSelector: " + paths);
                if (!string.IsNullOrEmpty(paths))
                {
                    var docselector = JsonConvert.DeserializeObject(paths);
                    selector = JArray.Parse(docselector.ToString());
                }
                else
                    throw new Exception("selector is null");
                var element = Utils.ElementFromUniqueId(paramsModel.ElementId, docCache);
                //if (!string.IsNullOrEmpty(attributeName))
                //{
                //    return element.getAttribute(attributeName);
                //}
                //BuildFrameSelector(element, selector);
                BuildFrameSelector(paramsModel.ElementId, selector, element);
                return BuildSelectorItem(selector, IhDoc);
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "IE BuildSelector Error");
                return null;
            }
        }
        private string BuildSelectorItem(JArray selector, IHTMLDocument2 doc)
        {
            if (selector != null)
            {
                var json = JsonConvert.DeserializeObject(selector.ToString());
                JArray selectors = JArray.Parse(json.ToString());
                //var selectorProp = new
                //{
                //    name = "tab",
                //    props = new List<Props>()
                //};
                JObject jobj = new JObject();
                jobj.Add("name", "tab");
                jobj.Add("accurate", "true");
                Props title = new Props()
                {
                    Name = "title",
                    Value = doc.title,
                    Pattern = Pattern.EQUAL.GetPatternAttribute(),
                    Accurate = "true"

                };
                Props url = new Props()
                {
                    Name = "url",
                    Value = doc.url,
                    Pattern = Pattern.EQUAL.GetPatternAttribute(),
                    Accurate = "false"
                };
                JArray array = new JArray();
                array.Add(JsonConvert.DeserializeObject(JsonConvert.SerializeObject(title)));
                array.Add(JsonConvert.DeserializeObject(JsonConvert.SerializeObject(url)));
                jobj.Add("props", array);


                var wndStr = JsonConvert.SerializeObject(jobj);
                var wndObj = JsonConvert.DeserializeObject(wndStr);
                selectors.AddFirst(wndObj);
                return selectors.ToString();
            }
            return null;
        }
        private void BuildFrameSelector(string uniqueId, JArray selector, IHTMLElement element, ParamsBuildSelectorAllModel paramsModel = null)
        {
            try
            {
                paramsModel = paramsModel ?? new ParamsBuildSelectorAllModel();

                IHTMLDocument2 document = element.document;

                IHTMLWindow2 window = document.parentWindow;
                if (window != null)
                {
                    var tokens = uniqueId.Split('/');
                    if (tokens.Length < 3)
                    {
                        throw new IEException();
                    }
                    var tagName = tokens[tokens.Length - 1];
                    Array.Clear(tokens, tokens.Length - 1, 1);
                    var value = string.Join("/", tokens);
                    var uniqueValue = value.Remove(value.Length - 1);
                    var uniqueArray = uniqueId.Split('/');
                    // 0/1/0/2/A    1/0  为iframe编号 生成selector时 需要反向遍历通过编号 生成 iframe的selector 
                    for (int i = uniqueArray.Length - 3; i >= 1; i--)
                    {
                        window = WebWindowHelper.WebWindow(window.parent);
                        IHTMLFrameBase2 frame = Utils.GetChildFrameByIndex(window.document as HTMLDocument, Convert.ToInt32(uniqueArray[i]));
                        var frameSelector = JArray.Parse(Utils.BuildSelector(
                            window,
                            "",
                            uniqueArray[i].ToString(),
                            paramsModel.innerTextFirst,
                            paramsModel.rawData,
                            paramsModel.excludeInnerText,
                            paramsModel.excludeHiddenEle,
                            paramsModel.ValidNodeAttributes));
                        for (int j = frameSelector.Count - 1; j >= 0; j--)
                        {
                            selector.AddFirst(frameSelector[j]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "BuildFrameSelector");
            }
        }
        [DisplayName("tabs.captureVisibleTab")]
        public JToken TabCapture(int tabId)
        {
            return string.Empty;
        }
        public JToken TabCaptureFull(int tabId, JObject rect = null, int timeout = 5000)
        {
            return string.Empty;
        }
        #endregion

        #region 根据selector匹配元素相关
        [DisplayName("tabs.querySelector")]
        public JToken QuerySelector(JObject paramObject)
        {

            var paramsModel = paramObject.ToObject<ParamsQuerySelectorModel>();
            var win2 = GetWindow2ByTabId(paramsModel.TabId);
            Utils.InjectJavaScript(win2);
            var selector = JsonConvert.DeserializeObject<List<SelectorProps>>(paramsModel.selector.ToString());
            var eles = QuerySelectorAll(selector, paramsModel.version, paramsModel.cssSelector, win2, paramsModel.index - 1, "", paramsModel.discernHidden);
            foreach (var e in eles)
            {
                return e.Key;
            }
            return null;
        }
        //public string QuerySelector(int tabId, string selector, string version, int index = 0, string parentElementId = null)
        //{
        //    return string.Empty;
        //    //return this.Send("tabs.querySelector",
        //    //    new JObject {
        //    //        { "tabId", tabId },
        //    //        { "selector", JArray.Parse(selector) },
        //    //        { "parent", parentElementId },
        //    //        { "index", index },
        //    //        { "version", version }
        //    //    }).ToString();
        //}
        public string QuerySelectorOld(int tabId, string browserType, string selector, int index = 0, string parentElementId = null)
        {
            return string.Empty;
            //var buildselector = BrowserSelectorItem.GetAllSelectorJson(selector, browserType);
            //var retJToken = this.Send("tabs.querySelector",
            //    new JObject {
            //        { "tabId", tabId },
            //        { "selector", JArray.Parse(buildselector.ToString()) },
            //        { "parent", parentElementId },
            //        { "index", index }
            //    });
            //if (retJToken == null || string.IsNullOrEmpty(retJToken.ToString()))
            //{
            //    buildselector = BrowserSelectorItem.GetAllSelectorJson(selector, browserType, "regular");
            //    retJToken = this.Send("tabs.querySelector",
            //        new JObject {
            //            { "tabId", tabId },
            //            { "selector", buildselector },
            //            { "parent", parentElementId },
            //            { "index", index }
            //        });
            //    return retJToken.ToString();
            //}
            //return retJToken.ToString();
        }
        public string QueryCssSelector(int tabId, string browserType, string selector, int index, string parent)
        {
            return string.Empty;
            //var jsonObj = (JToken)JsonHelper.JsonDesByNewtonJson(selector);
            //string cssSelector = string.Empty;
            //string outertext = string.Empty;
            //JArray nodes = new JArray();
            //foreach (var items in jsonObj)
            //{
            //    if (items.Value<string>("cssSelector") != null || items.Value<string>("cssSelectorEx") != null
            //        || !string.IsNullOrEmpty(items.Value<string>("tagName")?.ToString()))
            //    {
            //        var node = new JObject();
            //        foreach (JProperty item in items)
            //        {
            //            if (item.Name == "cssSelector")
            //                cssSelector = item.Value.ToString();
            //            else if (item.Name == "cssSelectorEx")
            //                cssSelector = item.Value.ToString();
            //            else if (item.Name == "outertext")
            //                outertext = item.Value.ToString();
            //            else if (item.Name == "outertextEx")
            //                outertext = item.Value.ToString();
            //            else
            //                node.Add(item.Name, item.Value);
            //        }
            //        nodes.Add(node);
            //    }
            //}
            //JObject css = new JObject();
            //css.Add("tagName", "document");
            //css.Add("cssSelector", cssSelector);
            //if (!string.IsNullOrEmpty(outertext))
            //    css.Add("outertext", outertext);
            //nodes.Add(css);

            //var buildselector = BrowserSelectorItem.GetAllSelectorJson(nodes.ToString(), browserType: browserType);
            //var retJToken = this.Send("tabs.queryCSSSelector",
            //    new JObject {
            //        { "tabId", tabId },
            //        { "selector", buildselector },
            //        { "index", index },
            //        { "parent", parent }
            //    });
            //return retJToken.ToString();
        }
        [DisplayName("tabs.querySelectorAll")]
        public JToken QuerySelectorAll(JObject paramObject)
        {

            var paramsModel = paramObject.ToObject<ParamsQuerySelectorAllModel>();
            JArray array = new JArray();
            var win2 = GetWindow2ByTabId(paramsModel.TabId);
            Utils.InjectJavaScript(win2);
            var selector = JsonConvert.DeserializeObject<List<SelectorProps>>(paramsModel.selector.ToString());
            var eles = QuerySelectorAll(selector, paramsModel.version, paramsModel.cssSelector, win2, paramsModel.index, parentId: paramsModel.parent, paramsModel.discernHidden);

            //滚动
            //if (paramsModel.resultAttrName == "bounding")
            //{
            //    var element = eles.First().Value;
            //    var elementId = eles.First().Key;
            //    var zoom = Utils.ZoomLevel(win2);
            //    var rect = GetElementBounding(element, elementId, zoom);
            //    var rectValue = new
            //    {
            //        left = rect.Left,
            //        top = rect.Top,
            //        width = rect.Width,
            //        height = rect.Height
            //    };
            //    var win = ElementInWindow(element, elementId);
            //    Utils.ScrollIntoView(win, elementId, "center", rectValue.left, rectValue.top);
            //}

            if (eles == null)
            {
                throw new RPAException(eErrorMessageType.Error_NotFoundElement);
            }
            foreach (var ele in eles)
            {
                if (paramsModel.resultAttrName == "bounding")
                {
                    var zoom = Utils.ZoomLevel(win2);
                    var rect = GetElementBounding(ele.Value, ele.Key, zoom);
                    var rectValue = new
                    {
                        left = rect.X,
                        top = rect.Y,
                        width = rect.Width,
                        height = rect.Height
                    };
                    array.Add(JsonConvert.SerializeObject(rectValue));
                }
                else
                {
                    array.Add(ele.Key);
                }
            }
            return JsonConvert.SerializeObject(array);
        }
        public string QuerySelectorAll(int tabId, string selector, string version, int index = -1, string parentElementId = null, string resultAttrName = null, string filter = null, ElementRelativeType relativeType = ElementRelativeType.Similar, string positionNum = null)
        {
            return string.Empty;
            //return this.Send("tabs.querySelectorAll", new JObject {
            //        { "tabId", tabId },
            //        { "selector", JArray.Parse(selector) },
            //        { "parent", parentElementId },
            //        { "index", index },
            //        { "resultAttrName", resultAttrName },
            //        { "filter", filter },
            //        { "relativeType", (int)relativeType } ,
            //        { "positionNum", positionNum },
            //        { "version", version }
            //}).ToString();
        }
        public string QuerySelectorAllOld(int tabId, string selector, int index = -1, string parentElementId = null)
        {
            return string.Empty;
            //var buildselector = BrowserSelectorItem.GetAllSelectorJson(selector, "Chrome");
            //var result = this.Send("tabs.querySelectorAll",
            //    new JObject {
            //        { "tabId", tabId },
            //        { "selector", buildselector },
            //        { "parent", parentElementId },
            //        { "index", index }
            //    });
            //var content = JArray.Parse(result.ToString());
            //if (content.Count == 0)
            //{
            //    buildselector = BrowserSelectorItem.GetAllSelectorJson(selector, "Chrome", "regular");
            //    result = this.Send("tabs.querySelectorAll",
            //        new JObject {
            //            { "tabId", tabId },
            //            { "selector", buildselector },
            //            { "parent", parentElementId },
            //            { "index", index }
            //        });
            //    return result.ToString();
            //}
            //return result.ToString();
        }
        #endregion

        #region [cross-domain]
        internal int GetFrameIndex(IHTMLWindow2 win)
        {
            if (win.parent == win || win.parent == null)
            {
                return -1;
            }
            else
            {
                IHTMLWindow2 webWindow = WebWindowHelper.WebWindow(win);
                IHTMLWindow2 win2 = WebWindowHelper.WebParentWindow(webWindow.document as HTMLDocument);
                var cons = win2.frames;
                for (int i = 0; i < cons.length; i++)
                {
                    IHTMLWindow2 w = cons.item(i) as IHTMLWindow2;
                    if (w == win)
                    {
                        return i;
                    }
                }
                return -1;
            }
        }
        #endregion
        public IHTMLElement ElementFromPoint(HTMLDocument document, Point point, List<string> frameList, ref IHTMLWindow2 win2, out Rectangle eleBounding)
        {
            var stopWatch = new Stopwatch();
            eleBounding = new Rectangle(0, 0, 0, 0);
            try
            {
                stopWatch.Start();
                var zoom = Utils.ZoomLevel(win2);
                Utils.pointClientToPage(ref point, zoom);
                IHTMLElement ele = null;
                ele = document.elementFromPoint(point.X, point.Y);
                var ele2 = ele as IHTMLElement2;
                IHTMLRect rect;
                var left = ele2.currentStyle.paddingLeft;
                var top = ele2.currentStyle.paddingTop;
                //double.TryParse(left?.Replace("px", ""), out double panddingleft);
                //double.TryParse(top?.Replace("px", ""), out double panddingtop);
                logger.LogDebug($"Get element location {stopWatch.ElapsedMilliseconds}");
                while (true)
                {
                    if (IsFrameElement(ele))
                    {
                        logger.LogDebug($"Start frame {stopWatch.ElapsedMilliseconds}");
                        IHTMLFrameBase2 base2 = ele as IHTMLFrameBase2;
                        win2 = WebWindowHelper.WebFrameWindow(base2);
                        Utils.InjectJavaScript(win2);
                        var doc = win2.document as HTMLDocument;
                        var frameId = Utils.GetFrameIndex(win2);
                        if (frameId >= 0)
                            frameList.Add(frameId.ToString());

                        IHTMLElement2 frameElement = ele as IHTMLElement2;
                        var frameRect = frameElement.getBoundingClientRect();
                        HTMLRectEX offsetRect = Utils.LastInjectIEVersion < 8 ? new HTMLRectEX() : frameElement.GetOffsetByPadding();
                        //var offsetRect = frameElement.GetOffsetByPadding();
                        point.X -= frameRect.left + offsetRect.Left;
                        point.Y -= frameRect.top + offsetRect.Top;
                        eleBounding.X += (frameRect.left + offsetRect.Left);
                        eleBounding.Y += (frameRect.top + offsetRect.Top);


                        ele = doc.elementFromPoint((int)(point.X), (int)(point.Y));
                        logger.LogDebug($"Stop frame {stopWatch.ElapsedMilliseconds}");
                    }
                    else
                    {
                        rect = ((IHTMLElement2)ele).getBoundingClientRect();
                        eleBounding = new Rectangle(
                                (int)((rect.left + eleBounding.X) * zoom),
                                (int)((rect.top + eleBounding.Y) * zoom),
                                (int)((rect.right - rect.left) * zoom),
                                (int)((rect.bottom - rect.top) * zoom)
                            );
                        break;
                    }
                }
                stopWatch.Stop();
                return ele;
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "ElementFromPoint Error");
                stopWatch.Stop();
                return null;
            }
        }
        #region 元素 数据获取和操作 相关
        [DisplayName("elements.isDisplayed")]
        public string IsDisplayed(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            try
            {
                var IhDoc = GetDocument2ByTabID(paramsModel.TabId);
                var element = ElementFromUniqueId(paramsModel.TabId, paramsModel.ElementId);
                HTMLDocument doc2 = element.document as HTMLDocument;
                var win = WebWindowHelper.WebDocumentWindow(doc2);
                var isDisplayed = Utils.InvokeScript(win, "isDisplayed", paramsModel.ElementId);
                return isDisplayed;
            }
            catch (Exception)
            {
                return "true";
            }
        }
        [DisplayName("tabs.elementFromPoint")]
        public JToken ElementFromPoint(JObject paramObject)
        {
            try
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                var paramsModel = paramObject.ToObject<ParamsElementFormPointModel>();
                var point = new Point(paramsModel.X, paramsModel.Y);
                var IhDoc = GetDocument2ByTabID(paramsModel.TabId);
                var doc = IhDoc as HTMLDocument;
                var _window = GetWindow2ByTabId(paramsModel.TabId);

                List<string> frameList = new List<string>();
                frameList.Add("0");
                var element = ElementFromPoint(doc, point, frameList, ref _window, out var eleBounding);
                logger.LogDebug($"ElementFormPoint {stopWatch.ElapsedMilliseconds}");
                stopWatch.Stop();
                if (element != null)
                {
                    var uniqueId = Utils.SetUniqueId(_window, frameList, element);
                    var rectValue = new
                    {
                        left = eleBounding.Left,
                        top = eleBounding.Top,
                        width = eleBounding.Width,
                        height = eleBounding.Height
                    };
                    return new JObject {
                        { "uniqueId",uniqueId},
                        { "bounding",JObject.FromObject(rectValue)},
                        { "attributes",JObject.FromObject(new { type=element.getAttribute("type")}) }
                    };
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "IE ElementFromPoint Error");
                return null;
            }
        }

        private IHTMLWindow2 ElementInWindow(IHTMLElement element, string uniqueId)
        {
            IHTMLWindow2 window = WebWindowHelper.WebDocumentWindow(element.document);
            if (window != null)
            {
                var tokens = uniqueId.Split('/');
                if (tokens.Length < 3)
                {
                    throw new IEException();
                }
                var tagName = tokens[tokens.Length - 1];
                Array.Clear(tokens, tokens.Length - 1, 1);
                var value = string.Join("/", tokens);
                var uniqueValue = value.Remove(value.Length - 1);
                var uniqueArray = uniqueId.Split('/');
                for (int i = uniqueArray.Length - 3; i >= 1; i--)
                {
                    try
                    {
                        window = WebWindowHelper.WebWindow(window.parent);
                        IHTMLFrameBase2 frame = Utils.GetChildFrameByIndex(window.document as HTMLDocument, Convert.ToInt32(uniqueArray[i]));
                        window = WebWindowHelper.WebFrameWindow(frame);
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "GetElementBounding");
                    }
                }
                return window;
            }
            return null;
        }
        [DisplayName("elements.scrollIntoViewIfNeeded")]
        public void ScrollIntoViewIfNeeded(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsScrollIntoViewIfNeededModel>();
            var IhDoc = GetDocument2ByTabID(paramsModel.TabId);
            var doc = IhDoc as HTMLDocument;
            var win2 = WebWindowHelper.WebDocumentWindow(doc);
            var width = Utils.InvokeScriptObject(win2, "document.documentElement.clientWidth") ?? 0;
            var height = Utils.InvokeScriptObject(win2, "document.documentElement.clientHeight") ?? 0;


            var element = ElementFromUniqueId(paramsModel.TabId, paramsModel.ElementId);
            var docR = Rectangle.FromLTRB(0, 0, (int)width, (int)height);
            var zoom = Utils.ZoomLevel(win2);
            var eleR = GetElementBounding(element, paramsModel.ElementId, zoom);
            if (!docR.Contains(eleR))
            {
                element.scrollIntoView(Type.Missing);
                var top = Utils.InvokeScriptObject(win2, "GetScroll", "top") ?? 0;
                var topValue = (int)top - ((int)height / 2);
                Utils.InvokeScript(win2, "IIRPA.api.scroll", "", "top", topValue.ToString());
            }
        }
        [DisplayName("tabs.getIntoViewPortRect")]
        public JToken GetIntoViewPortRect(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            var IhDoc = GetDocument2ByTabID(paramsModel.TabId);
            var doc = IhDoc as HTMLDocument;
            var win2 = WebWindowHelper.WebDocumentWindow(doc);
            var innerHeight = Utils.InvokeScript(win2, "window.innerHeight") ?? "0";
            var innerWidth = Utils.InvokeScript(win2, "window.innerWidth") ?? "0";
            var rectValue = new
            {
                left = 0,
                top = 0,
                width = innerWidth,
                height = innerHeight
            };
            return (JToken)JsonConvert.SerializeObject(rectValue);
        }
        private Rectangle ElementRectangle(IHTMLElement element)
        {
            IHTMLElement2 ele2 = element as IHTMLElement2;
            if (ele2 != null)
            {
                var ele2Rect = ele2.getBoundingClientRect();
                return Rectangle.FromLTRB(ele2Rect.left, ele2Rect.top, ele2Rect.right, ele2Rect.bottom);
            }
            else
            {
                return new Rectangle();
            }
        }

        [DisplayName("elements.scrollIntoViewIfNeeded_V2")]
        public void ScrollIntoViewIfNeeded_V2(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsScrollIntoViewIfNeededModel>();
            var IhDoc = GetDocument2ByTabID(paramsModel.TabId);
            var doc = IhDoc as HTMLDocument;
            var win2 = WebWindowHelper.WebDocumentWindow(doc);
            var element = ElementFromUniqueId(paramsModel.TabId, paramsModel.ElementId);

            IHTMLElement2 ele2 = element as IHTMLElement2;
            if (ele2 == null)
                return;

            var innerHeight = Utils.InvokeScript(win2, "window.innerHeight  || document.documentElement.clientHeight || document.body.clientHeight") ?? "0";
            var innerWidth = Utils.InvokeScript(win2, "window.innerWidth || document.documentElement.clientWidth || document.body.clientWidth") ?? "0";
            int.TryParse(innerWidth, out int innerWidthValue);
            int.TryParse(innerHeight, out int innerHeightValue);
            var innerRect = new Rectangle(0, 0, innerWidthValue, innerHeightValue);
            var zoom = Utils.ZoomLevel(win2);
            var rect = GetElementBounding(element, paramsModel.ElementId, zoom);
            var center = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
            if (!innerRect.Contains(center))
            {
                element.scrollIntoView();
                Thread.Sleep(500);//防止 滚动效果影响获取元素范围
            }

            var top = Utils.InvokeScript(win2, "GetScroll", "top") ?? "0";
            var left = Utils.InvokeScript(win2, "GetScroll", "left") ?? "0";
            var isCovered = Utils.InvokeScript(win2, "IsCovered", paramsModel.ElementId) ?? "False";
            logger.LogDebug($"top {top} left {left} innerHeight {innerHeight} innerWidth{innerWidth}");
            if (int.TryParse(top, out int topValue)
                && int.TryParse(left, out int leftValue))
            {
                if (isCovered.As<bool>())
                {
                    win2.scrollTo(leftValue - innerWidthValue / 2, topValue - innerHeightValue / 2);
                }
            }


        }


        public Rectangle GetElementBounding(IHTMLElement element, string uniqueId, double zoom = 1.00)
        {
            var ele = element as IHTMLElement2;
            if (ele != null)
            {
                var rect = ele.getBoundingClientRect();
                int panddingleft = 0;
                int panddingtop = 0;

                IHTMLWindow2 window = WebWindowHelper.WebDocumentWindow(element.document);
                if (window != null)
                {
                    var tokens = uniqueId.Split('/');
                    if (tokens.Length < 3)
                    {
                        throw new IEException();
                    }
                    var tagName = tokens[tokens.Length - 1];
                    Array.Clear(tokens, tokens.Length - 1, 1);
                    var value = string.Join("/", tokens);
                    var uniqueValue = value.Remove(value.Length - 1);
                    var uniqueArray = uniqueId.Split('/');
                    for (int i = uniqueArray.Length - 3; i >= 1; i--)
                    {
                        try
                        {
                            window = WebWindowHelper.WebWindow(window.parent);
                            IHTMLFrameBase2 frame = Utils.GetChildFrameByIndex(window.document as HTMLDocument, Convert.ToInt32(uniqueArray[i]));
                            IHTMLElement2 frameElement = frame as IHTMLElement2;
                            var frameRect = frameElement.getBoundingClientRect();
                            HTMLRectEX offsetRect = Utils.LastInjectIEVersion < 8 ? new HTMLRectEX() : frameElement.GetOffsetByPadding();
                            logger.LogDebug($"element bounding left:{frameRect.left} top:{frameRect.top}");
                            panddingleft += frameRect.left + offsetRect.Left;
                            panddingtop += frameRect.top + offsetRect.Top;
                        }
                        catch (Exception ex)
                        {
                            Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "GetElementBounding");
                        }
                    }
                    return new Rectangle((int)((rect.left + panddingleft) * zoom), (int)((rect.top + panddingtop) * zoom), (int)((rect.right - rect.left) * zoom), (int)((rect.bottom - rect.top) * zoom));
                }
            }
            return new Rectangle();
        }
        [DisplayName("elements.getBoundingAll")]
        public JToken BuildElementBoundingAll(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            JArray bounds = new JArray();
            foreach (var elementId in paramsModel.elementIds)
            {
                paramObject["ElementId"] = elementId;
                var bound = BuildElementBounding(paramObject);
                var result = JToken.Parse(bound.ToString());
                bounds.Add(result);
            }
            return bounds;
        }
        [DisplayName("elements.getBounding")]
        public JToken BuildElementBounding(JObject paramObject)
        {
            var stopWatch = new Stopwatch();
            var paramsModel = paramObject.ToObject<ParamsModel>();
            var win2 = GetWindow2ByTabId(paramsModel.TabId);
            var element = ElementFromUniqueId(paramsModel.TabId, paramsModel.ElementId);
            var zoom = Utils.ZoomLevel(win2);
            var rect = GetElementBounding(element, paramsModel.ElementId, zoom);
            var rectValue = new
            {
                left = rect.Left,
                top = rect.Top,
                width = rect.Width,
                height = rect.Height
            };
            stopWatch.Stop();
            logger.LogDebug($"BuildElementBounding {stopWatch.ElapsedMilliseconds}");
            return (JToken)JsonConvert.SerializeObject(rectValue);
        }
        [DisplayName("elements.getTextAll")]
        public JToken GetTextAll(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            JArray texts = new JArray();
            foreach (var elementId in paramsModel.elementIds)
            {
                paramObject["ElementId"] = elementId;
                var result = GetText(paramObject);
                texts.Add(result);
            }
            return texts;
        }
        [DisplayName("elements.getText")]
        public string GetText(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            var element = ElementFromUniqueId(paramsModel.TabId, paramsModel.ElementId);
            string txt = "";
            try
            {
                logger.LogInformation("Begin GetText ");
                var tagName = element.tagName.ToLower();
                if (element.tagName.ToLower() != "body")
                {
                    switch (tagName.ToLower())
                    {
                        case "input":
                        case "textarea":
                            HTMLInputElement ele = (HTMLInputElement)element;
                            txt = ele.value;
                            break;
                        default:
                            txt = element.innerText;
                            break;
                    }
                }
                else
                {
                    logger.LogInformation($"Get InnerText");
                    txt = element.innerText;
                }
            }
            catch (Exception ex)
            {
                throw new IEException(eErrorMessageType.Error_GetTextError, ex);
            }

            if (txt == null)
            {
                txt = "";
            }
            logger.LogInformation($"End GetText  {txt}");
            return txt;
        }
        [DisplayName("elements.getNextElementBuildSelector")]
        public JToken GetNextElementBuildSelector(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsGetNextElementBuildSelectorModel>();
            var window = GetWindow2ByTabId(paramsModel.TabId);
            var nextSelector = Utils.InvokeScript(window, "getNextElementBuildSelector", paramsModel.cssSelector, paramsModel.innerTextFirst.ToString());
            return nextSelector;
        }
        [DisplayName("elements.getTurningPageElement")]
        public JToken GetTurningPageElement(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsGetTurningPageElementModel>();
            var eleUid = string.Empty;
            try
            {
                var window = GetWindow2ByTabId(paramsModel.TabId);
                var element = ElementFromUniqueId(paramsModel.TabId, paramsModel.ElementId);
                HTMLDocument doc2 = element.document as HTMLDocument;
                var win = WebWindowHelper.WebDocumentWindow(doc2);
                var ele = Utils.getTurningPageElement(win, paramsModel.cssSelectors.ToJsonNoneFormating()) as IHTMLElement;
                if (ele != null)
                {
                    eleUid = ElementFrom(paramsModel.ElementId, ele);
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug($"GetTurningPageElement Error:{ex}");
            }
            return eleUid;
        }
        [DisplayName("elements.getUsableAttributes")]
        public JToken GetUsableAttributes(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsGetSimilarElementModel>();
            var eleUid = string.Empty;
            try
            {
                var window = GetWindow2ByTabId(paramsModel.TabId);
                var element = ElementFromUniqueId(paramsModel.TabId, paramsModel.ElementId);
                HTMLDocument doc2 = element.document as HTMLDocument;
                var win = WebWindowHelper.WebDocumentWindow(doc2);
                return (string)Utils.InvokeScriptObject(win, "getUsableAttributes", paramsModel.ElementId);
            }
            catch (Exception ex)
            {
                logger.LogDebug($"GetTurningPageElement Error:{ex}");
            }
            return eleUid;
        }
        [DisplayName("tabs.getSimilarElement")]
        public JToken GetSimilarElement(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsGetSimilarElementModel>();
            var eleUid = string.Empty;
            try
            {
                var window = GetWindow2ByTabId(paramsModel.TabId);
                var element = ElementFromUniqueId(paramsModel.TabId, paramsModel.ElementId);
                HTMLDocument doc2 = element.document as HTMLDocument;
                var win = WebWindowHelper.WebDocumentWindow(doc2);
                var ele = Utils.InvokeScriptObject(win, "getSimilarElement", paramsModel.ElementId) as IHTMLElement;
                if (ele != null)
                {
                    eleUid = ElementFrom(paramsModel.ElementId, ele);
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug($"GetTurningPageElement Error:{ex}");
            }
            return eleUid;
        }
        public string ElementFrom(string uniqueId, IHTMLElement element, int frameIndex = 0)
        {
            try
            {
                var uniqueIdArray = uniqueId.Split('/');
                List<string> frameList = new List<string>();
                for (int i = 0; i < uniqueIdArray.Length - 2; i++)
                {
                    frameList.Add(uniqueIdArray[i]);
                }
                var win = WebWindowHelper.WebDocumentWindow(element.document);
                return Utils.SetUniqueId(win, frameList, element);

            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "IE ElementFromPoint Error");
                return null;
            }
        }
        [DisplayName("elements.getIsTableName")]
        public JToken GetIsTableName(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            var tbUid = string.Empty;
            try
            {
                var window = GetWindow2ByTabId(paramsModel.TabId);
                var element = ElementFromUniqueId(paramsModel.TabId, paramsModel.ElementId);
                HTMLDocument doc2 = element.document as HTMLDocument;
                var win = WebWindowHelper.WebDocumentWindow(doc2);
                var ele = Utils.InvokeScriptObject(win, "getIsTableName", paramsModel.ElementId) as IHTMLElement;
                if (ele != null)
                {
                    tbUid = ElementFrom(paramsModel.ElementId, ele);
                    var istb = Convert.ToBoolean(ele != null);
                    return tbUid;
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug($"GetIsTableName Error:{ex}");
            }
            return tbUid;
        }

        [DisplayName("elements.getTableData")]
        public JToken GetTableData(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            try
            {
                var window = GetWindow2ByTabId(paramsModel.TabId);
                var element = ElementFromUniqueId(paramsModel.TabId, paramsModel.ElementId);
                HTMLDocument doc2 = element.document as HTMLDocument;
                var win = WebWindowHelper.WebDocumentWindow(doc2);
                var dataStr = Utils.InvokeScript(win, "getTableData", paramsModel.ElementId);
                return dataStr;
            }
            catch (Exception)
            {
                return "";
            }
        }
        [DisplayName("elements.getAttributes")]
        public JToken GetAttributes(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            return JsonConvert.SerializeObject(GetAttributes(paramsModel.TabId, paramsModel.ElementId));
        }
        public Dictionary<string, string> GetAttributes(int tabId, string unqueId)
        {
            var element = ElementFromUniqueId(tabId, unqueId);
            Dictionary<string, string> attributeDic = new Dictionary<string, string>();
            try
            {
                IHTMLAttributeCollection attributes = (element as IHTMLDOMNode).attributes as IHTMLAttributeCollection;
                foreach (IHTMLDOMAttribute attr in attributes)
                {
                    try
                    {
                        attributeDic.Add(attr.nodeName, attr.nodeValue ?? "");
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                // maybe，not suppert attribute
                //throw new IEException(eErrorMessageType.Error_GetAttributeError, ex);
            }
            return attributeDic;
        }
        public Dictionary<string, string> GetAttributes(IHTMLElement element)
        {
            Dictionary<string, string> attributeDic = new Dictionary<string, string>();
            try
            {
                IHTMLAttributeCollection attributes = (element as IHTMLDOMNode).attributes as IHTMLAttributeCollection;
                foreach (IHTMLDOMAttribute attr in attributes)
                {
                    try
                    {
                        attributeDic.Add(attr.nodeName, attr.nodeValue ?? "");
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                // maybe，not suppert attribute
                //throw new IEException(eErrorMessageType.Error_GetAttributeError, ex);
            }
            return attributeDic;
        }
        [DisplayName("elements.getAttributeAll")]
        public JToken GetAttributeAll(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsGetAttributeModel>();
            JArray attrs = new JArray();
            foreach (var elementId in paramsModel.elementIds)
            {
                paramObject["ElementId"] = elementId;
                var result = GetAttribute_V1(paramObject);
                attrs.Add(paramsModel.apiVersion == "v2" ? result.Value<string>("value") : result);
            }
            return attrs;
        }
        public JToken GetAttribute_V2(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsGetAttributeModel>();
            var IhDoc = GetDocument2ByTabID(paramsModel.TabId);
            var doc = IhDoc as HTMLDocument;
            var win2 = WebWindowHelper.WebDocumentWindow(doc);
            var ret = Utils.InvokeScript(win2, "getAttribute_V2", paramsModel.ElementId, paramsModel.Name, paramsModel.isIntact ? "1" : "0");
            return JObject.Parse(ret);
        }
        [DisplayName("elements.getAttribute_V1")]
        public JToken GetAttribute_V1(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsGetAttributeModel>();
            if (paramsModel.apiVersion == "v2")
            {
                return GetAttribute_V2(paramObject);
            }

            var element = ElementFromUniqueId(paramsModel.TabId, paramsModel.ElementId);
            var dicAttr = GetAttributes(paramsModel.TabId, paramsModel.ElementId);
            if (paramsModel.Name == "src")
            {
                if (dicAttr.ContainsKey("src"))
                {
                    return dicAttr["src"];
                }
                else
                {
                    return null;
                }
            }
            else if (paramsModel.Name == "absoluteurl")
            {
                if (dicAttr.ContainsKey("href") || dicAttr.ContainsKey("src"))
                {
                    if (paramsModel.isIntact)
                    {
                        return dicAttr["href"] ?? dicAttr["src"];
                    }
                    return element.getAttribute("href") as string ?? element.getAttribute("src") as string ?? string.Empty;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                if (dicAttr.ContainsKey(paramsModel.Name))
                    return dicAttr[paramsModel.Name];
                else
                    return null;
            }
        }
        [DisplayName("elements.getAttribute")]
        public JToken GetAttribute(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsGetAttributeModel>();
            if (paramsModel.apiVersion == "v2")
            {
                return GetAttribute_V2(paramObject);
            }
            var dic = GetAttributes(paramsModel.TabId, paramsModel.ElementId);
            if (dic.ContainsKey(paramsModel.Name))
            {
                return dic[paramsModel.Name];
            }
            return null;
            // maybe，not suppert attribute
            //throw new IEException(eErrorMessageType.Error_GetAttributeError);
        }
        [DisplayName("elements.getHtmlAll")]
        public JToken GetHtmlAll(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            JArray htmls = new JArray();
            foreach (var elementId in paramsModel.elementIds)
            {
                paramObject["ElementId"] = elementId;

                var result = GetHtml(paramObject);
                htmls.Add(result);
            }
            return htmls;
        }
        [DisplayName("elements.getHtml")]
        public JToken GetHtml(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            var element = ElementFromUniqueId(paramsModel.TabId, paramsModel.ElementId);
            return element.outerHTML;
            //var paramsModel = paramObject.ToObject<ParamsModel>();
            //string strjs = "function (){";
            //strjs += "var test = document.getElementsByTagName('html')[0].outerHTML;";
            //strjs += "return test;}";
            //JavaScriptExecutor js = new JavaScriptExecutor();
            //return js.DoExecJavaScript("", "", strjs, "");
        }
        [DisplayName("elements.getParent")]
        public JToken GetParentIdList(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            throw new NotImplementedException();
        }
        [DisplayName("elements.getSiblings")]
        public JToken GetSiblingsIdList(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            throw new NotImplementedException();
        }
        [DisplayName("elements.getChildren")]
        public JToken GetChildrenIdList(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            var window = GetWindow2ByTabId(paramsModel.TabId);
            var element = Utils.ElementFromUniqueId(paramsModel.ElementId, window.document as HTMLDocument);
            string strSubElements = "";
            JArray jsonAllSelector = new JArray();
            try
            {
                IHTMLElementCollection elementCollection = element.children;
                if (elementCollection != null && elementCollection.length > 0)
                {
                    for (int i = 0; i < elementCollection.length; i++)
                    {
                        IHTMLElement child = elementCollection.item(i) as MSHTML.IHTMLElement;

                        var elementId = ElementFrom(paramsModel.ElementId, child);
                        //var selector = doc.BuildSelector(elementId);
                        //var selectorAll = ExtractViewData.AddCommonProps(selector, eGuiType.IE.ToString());
                        jsonAllSelector.Add(elementId);

                        strSubElements = string.Join("|", elementId);
                    }
                }
                else
                {
                    return "[]";
                }
            }
            catch (IEException e)
            {
                throw new IEException(eErrorMessageType.Error_GetChildElementError, e);
            }
            return strSubElements;
        }
        [DisplayName("elements.input")]
        public void Input(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsInputModel>();
            var element = ElementFromUniqueId(paramsModel.TabId, paramsModel.ElementId);
            var nodeName = element.tagName;
            var replace = paramsModel.replace;
            if (nodeName == "textarea" || nodeName == "input")
            {
                IHTMLInputElement input = (IHTMLInputElement)element;
                var value = input.value;
                if (replace)
                {
                    input.value = paramsModel.value;
                }
                else
                {
                    input.value = value + paramsModel.value;
                }
            }
            else
            {
                var value = element.innerText;
                if (replace)
                {
                    element.innerText = paramsModel.value;
                }
                else
                {
                    element.innerText = value + paramsModel.value;
                }
            }
        }
        [DisplayName("elements.isEditable")]
        public JToken IsEditable(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            var element = ElementFromUniqueId(paramsModel.TabId, paramsModel.ElementId);
            var input = (IHTMLInputElement)element;
            var nodeName = element.tagName;
            if (nodeName == "textarea" ||
                (nodeName == "input" && Regex.Match(element.getAttribute("type"), @"/^(?:text|email|number|search|tel|url|password)$/") ||
                element.isTextEdit))
            {
                return input.disabled != true && input.readOnly != true;
            }
            else
            {
                return false;
            }
        }
        [DisplayName("elements.getProperty")]
        public JToken GetProperty(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            var element = ElementFromUniqueId(paramsModel.TabId, paramsModel.ElementId);

            bool bExist = true;
            string strDisabled = "";
            try
            {
                strDisabled = element.outerHTML;
                int nPos1 = strDisabled.IndexOf("<");
                int nPos2 = strDisabled.IndexOf(">");
                int nPos = strDisabled.IndexOf("disabled");
                if (nPos > nPos1 && nPos < nPos2)
                {
                    bExist = false;
                }
            }
            catch (Exception ex)
            {
                bExist = true;
            }

            if (bExist == true)
                return "false";
            else
                return "true";

        }
        [DisplayName("elements.getCheckStatus")]
        public JToken GetCheckStatus(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            var element = ElementFromUniqueId(paramsModel.TabId, paramsModel.ElementId);

            string result = string.Empty;

            try
            {
                string typeName = element.getAttribute("type", 2)?.ToString();
                if (typeName.ToLower() == "radio" || typeName.ToLower() == "checkbox")
                {
                    IHTMLInputElement input = element as IHTMLInputElement;
                    if (input != null)
                    {
                        if (input.@checked == true)
                            return "true";
                        else
                            return "false";
                    }
                    else
                    {
                        string strClassName = element.className;
                        if (strClassName.ToLower() == "checkbox")
                            return "false";

                        if (strClassName.ToLower() == "checkbox checked")
                            return "true";
                    }
                }
                else
                {
                    throw new IEException(eErrorMessageType.Error_ElementNotOperation);
                }
            }
            catch (Exception ex)
            {
                throw new IEException(eErrorMessageType.Error_ElementNotOperation, ex);
            }
            return result;
        }
        [DisplayName("elements.getSelectOptions")]
        public JToken GetSelectOptions(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsGetSelectOptionsModel>();
            var element = ElementFromUniqueId(paramsModel.TabId, paramsModel.ElementId);
            if (element == null)
            {
                throw new IEException(eErrorMessageType.Error_NotFoundElement);
            }
            IHTMLSelectElement eleSelect = null;
            if (!element.tagName.Equals("select", StringComparison.OrdinalIgnoreCase))
            {
                throw new IEException(eErrorMessageType.Error_ElementNotOperation);
            }
            eleSelect = element as IHTMLSelectElement;
            List<string> optionList = new List<string>();
            for (int i = 0; i < eleSelect.length; i++)
            {
                try
                {
                    IHTMLElement e = eleSelect.item(i);

                    //add by wenyi for ZOCO-23568
                    var selected = e.getAttribute("selected");
                    if (paramsModel.selected == true && !selected) { continue; }

                    if (paramsModel.optionType == "text")
                    {
                        optionList.Add(e.innerText);
                    }
                    else if (paramsModel.optionType == "value")
                    {
                        optionList.Add(e.getAttribute("value", 2) ?? "");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }
            }
            return JsonConvert.SerializeObject(optionList);
        }
        [DisplayName("elements.getSelectOption")]
        public JToken GetSelectOption(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsGetSelectOptionModel>();
            var element = ElementFromUniqueId(paramsModel.TabId, paramsModel.ElementId);
            string result = string.Empty;
            IHTMLSelectElement eleSelect = null;
            if (!element.tagName.Equals("select", StringComparison.OrdinalIgnoreCase))
            {
                throw new IEException(eErrorMessageType.Error_ElementNotOperation);
            }
            eleSelect = element as IHTMLSelectElement;
            if (eleSelect == null)
                throw new IEException(eErrorMessageType.Error_ElementNotOperation);

            int index = eleSelect.selectedIndex;
            if (index < 0)
            {
                throw new IEException(eErrorMessageType.Error_ElementNotOperation);
            }
            IHTMLElement e = eleSelect.item(index);
            if (paramsModel.optionType == "text")
            {
                result = e.innerText;
            }
            else if (paramsModel.optionType == "value")
            {
                result = e.getAttribute("value", 2) ?? "";
            }
            return result;
        }
        [DisplayName("elements.click")]
        public void Click(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            var element = ElementFromUniqueId(paramsModel.TabId, paramsModel.ElementId);
            element.click();
        }
        [DisplayName("elements.rclick")]
        public void RightClick(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            var element = ElementFromUniqueId(paramsModel.TabId, paramsModel.ElementId);
        }
        [DisplayName("elements.rclick")]
        public void RightDoubleClick(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            throw new RPAException(eErrorMessageType.Error_NotImplemented);
        }
        [DisplayName("elements.dblclick")]
        public void DoubleClick(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            var element = ElementFromUniqueId(paramsModel.TabId, paramsModel.ElementId);
            HTMLDocument doc2 = element.document as HTMLDocument;
            var win = WebWindowHelper.WebDocumentWindow(doc2);
            InitEvent(win, paramsModel.ElementId, "dblclick");
        }
        [DisplayName("elements.setAttribute")]
        public JToken SetAttribute(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsSetAttributeModel>();
            var element = ElementFromUniqueId(paramsModel.TabId, paramsModel.ElementId);
            element.setAttribute(paramsModel.name, paramsModel.value);
            return string.Empty;
        }
        [DisplayName("elements.setCheckStatus")]
        public JToken SetCheckStatus(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            var element = ElementFromUniqueId(paramsModel.TabId, paramsModel.ElementId);

            try
            {
                string typeName = element.getAttribute("type");
                if (typeName.ToLower() == "radio" || typeName.ToLower() == "checkbox")
                {
                    HTMLInputElement chk = (HTMLInputElement)element;
                    if (chk.@checked)
                    {
                        //chk.click();
                        chk.@checked = false;
                    }
                    else
                    {
                        //chk.click();
                        chk.@checked = true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new IEException(eErrorMessageType.Error_SetElementCheckError, ex);
            }
            return string.Empty;
        }
        [DisplayName("elements.focus")]
        public void Focus(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsModel>();
            var element = ElementFromUniqueId(paramsModel.TabId, paramsModel.ElementId);
            IHTMLElement2 e = (IHTMLElement2)element;
            e.focus();
        }
        [DisplayName("elements.setSelectOptions")]
        public JToken SetSelectOptions(JObject paramObject)
        {
            var paramsModel = paramObject.ToObject<ParamsSetSelectOptionsModel>();
            var document = GetDocument2ByTabID(paramsModel.TabId);
            var element = ElementFromUniqueId(paramsModel.TabId, paramsModel.ElementId);
            if (!element.tagName.Equals("select", StringComparison.OrdinalIgnoreCase))
            {
                throw new IEException(eErrorMessageType.Error_ElementNotOperation);
            }

            bool changed = false;
            IHTMLElement2 ele2 = element as IHTMLElement2;
            IHTMLElementCollection elememtCollection = ele2.getElementsByTagName("option");
            if (element.getAttribute("multiple", 2) != null)
            {
                foreach (var item in paramsModel.options)
                {
                    var optionElement = elememtCollection.item((int)item);
                    if (optionElement != null && !(bool)optionElement.selected)
                    {
                        changed = true;
                        optionElement.selected = true;
                    }
                }
            }
            else
            {
                if (paramsModel.options.Count > 1)
                {
                    changed = true;
                    elememtCollection.item((int)paramsModel.options[0]).selected = true;
                }
            }
            if (changed)
            {
                HTMLDocument doc = element.document as HTMLDocument;
                var win = WebWindowHelper.WebDocumentWindow(doc);
                if (!InitEvent(win, paramsModel.ElementId, "change"))
                    FireEvent(document, element);
            }
            return string.Empty;
        }
        public bool InitEvent(IHTMLWindow2 window2, string uniqueId, string eventName)
        {
            return Utils.InvokeScript(window2, "initEvent", uniqueId, eventName).As<bool>();
        }
        public void FireEvent(IHTMLDocument2 document, IHTMLElement element)
        {
            IHTMLDocument4 doc4 = document as IHTMLDocument4;
            IHTMLElement3 element3 = element as IHTMLElement3;
            object dummy = null;
            object eventObj = doc4.CreateEventObject(ref dummy);
            element3.FireEvent("onchange", ref eventObj);
        }
        #endregion

        #region 网页监听

        public JToken SendCommand(int tabId, string method, JObject commandParam, int timeout = 5000)
        {
            throw new NotImplementedException();
        }
        public void Detach(int tabId)
        {
            throw new NotImplementedException();
        }
        public void EnableNetwork(int tabId)
        {
            throw new NotImplementedException();
        }

        public void AddNetworkCache(int tabId)
        {
            throw new NotImplementedException();
        }
        public List<JObject> GetInspectNetwork(int tabId, string type, string url, bool isRegex)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region 下载
        public JToken Downloading()
        {
            throw new NotImplementedException();
        }
        public JToken DownLoadSearch(int id)
        {
            throw new NotImplementedException();
        }
        public JToken DownLoadUrl(string url)
        {
            throw new NotImplementedException();

        }
        #endregion
    }
}

