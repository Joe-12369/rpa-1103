using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.WindowsAPI;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Documents;
using System.Threading;
using System.Collections.Concurrent;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using II.RPA.WebBrowser.Common.Basic.Params;
using FlaUI.Core.Tools;
using II.RPA.Plugin.GUI.Commons.WindowMatch.Entities;
using II.RPA.Plugin.GUI.Commons.WindowMatch;

namespace II.RPA.WebBrowser.Common.Basic
{

    public abstract class GuiOperatorBase : IGuiOperator_6_9_0, IGuiWebOperator
    {
        #region 公共使用的全局变量
        private static ILogger logger = Logger.OfContext();
        #endregion
        public abstract eGuiType GuiType { get; }
        protected abstract Browser Browser { get; }
        public int Sort { get; set; }
        /// <summary>
        /// 计时 首次判断浏览996 时，start
        /// </summary>
        protected Stopwatch sw_ChromeExtensionUnopen = new Stopwatch();
        protected bool IsExecutable { get; set; } = false;

        protected bool InspectIsTimeout { get; set; } = false;
        protected ConcurrentDictionary<IntPtr, BrowserTabPage> BrowserTabpageCache = new ConcurrentDictionary<IntPtr, BrowserTabPage>();
        public virtual void Release()
        {
            try
            {
                InspectIsTimeout = false;
                IsExecutable = false;
                //this.Browser.NativeMessageHostProcessID = 0;
                //logger.LogInformation("关闭Chrome管道");
                BrowserTabpageCache.Clear();
                //this.Browser.StopConnecttion();
                if (sw_ChromeExtensionUnopen.IsRunning)
                {
                    sw_ChromeExtensionUnopen.Stop();
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"操作后释放资源异常，{this.GuiType}");
            }
        }

        #region 拾取相关
        protected virtual bool SpecialScenariosInspectNode(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement, string selector, out IGuiNode inspectNode)
        {
            var inspectResult = InspectWindowsNode(screenPoint, rootElement, out inspectNode);
            logger.LogDebug($"specialScenarios win32 inspect 耗时：{RunContext.TimeInertval}");
            return inspectResult;
        }

        protected bool InspectWindowsNode(Point screenPoint, AutomationElement rootElement, out IGuiNode inspectNode)
        {
            return GuiRuntime.WindowsOperator.InspectNodeFromElement(screenPoint, rootElement, out inspectNode);
        }

        public virtual void ReadyToCapture()
        {
            IsExecutable = false;
            //this.Browser.NativeMessageHostProcessID = 0;
            BrowserTabpageCache.Clear();
            sw_ChromeExtensionUnopen.Reset();
            sw_ChromeExtensionUnopen.Restart();
        }
        public virtual bool IsCanUse(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement)
        {
            throw new NotImplementedException();
        }
        public virtual bool InspectNode(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement, string selector, out IGuiNode inspectNode)
        {
            var curActiveTab = this.GetCurActiveTab(NativeMethods.WindowFromPoint(screenPoint));
            var inspectResult = curActiveTab.InspectNode(rootElement.Properties.NativeWindowHandle, screenPoint, selector, out inspectNode);
            //超时时，拾取返回false
            this.InspectIsTimeout = !inspectResult;
            return inspectResult;
        }
        #endregion

        #region 匹配获取 元素 相关
        public virtual string GetElementsAll(GetElementsAllParams args)
        {
            BrowserTabPage tabpage;
            if (args.TabId > 0)
            {
                //tabpage = this.Browser.CreateBrowserTabPage(args.TabId);
                tabpage = this.Browser.CreateBrowserTabPage(new ParamCreateBrowserTabPageModel() { TabId = args.TabId });
            }
            else
            {
                tabpage = this.Browser.AcquireCurrentActiveTab();
            }
            var elementsJson = tabpage.GetElementsAll(args.TagNames, args.AttrKeywords);

            return new JObject() {
                { "elements",JArray.Parse( elementsJson)},
                { "img_id",""}
            }.ToJsonNoneFormating();
        }
        public virtual ViewElementsDataModel GetViewElementsData(GetElementsAllParams args)
        {
            BrowserTabPage tabpage;
            if (args.TabId > 0)
            {
                //tabpage = this.Browser.CreateBrowserTabPage(args.TabId);
                tabpage = this.Browser.CreateBrowserTabPage(new ParamCreateBrowserTabPageModel() { TabId = args.TabId });
            }
            else
            {
                tabpage = this.Browser.AcquireCurrentActiveTab();
            }
            var elementsJson = tabpage.GetViewElementsData();
            return elementsJson;
        }

        public virtual string GetWebCode(GetElementsAllParams args)
        {
            BrowserTabPage tabpage;
            if (args.TabId > 0)
            {
                //tabpage = this.Browser.CreateBrowserTabPage(args.TabId);
                tabpage = this.Browser.CreateBrowserTabPage(new ParamCreateBrowserTabPageModel() { TabId = args.TabId });
            }
            else
            {
                tabpage = this.Browser.AcquireCurrentActiveTab();
            }
            tabpage.GetViewElementsData();
            var webCode = tabpage.GetWebCode();
            return webCode;
        }

        public virtual IGuiNode GetElementFromSelector(GetElementFromSelectorParams args)
        {
            ElementGuiNode node = null;
            logger.LogInformation($"{args.SelectorType} 进入 GetElementFromSelector");
            ExtractViewData.SelectCommonProps(args.Selector, out string selectorType, out string version);
            //实例tab对象，在匹配元素过程中，后边补tab的必要数据，如tabID
            BrowserTabPage tabPage = this.Browser.CreateBrowserTabPage(new ParamCreateBrowserTabPageModel
            {
                RunParam = args.RunParam,
                WebWindowInfo = (BrowserWindow)args.WindowInfo,
                TabId = ((BrowserWindow)args.WindowInfo).SpecifiedTabId
            });
            try
            {
                if (version.StartsWith("1."))
                {
                    var cssSelector = ExtractViewData.GetQueryModeSelector(args.Selector, out string queryMode);
                    var selector = UiCommon.ExtractSelectors(args.Selector);
                    node = tabPage.GetElementFromSelector(selector, cssSelector, args.isActive, version, queryMode, args.UserDefinedId, args.DiscernHidden);
                }
                else
                {
                    node = tabPage.GetElementFromSelectorOld(args.Selector);
                }
            }
            catch (Exception ex)
            {
                if (ex.IsRPAException(eErrorMessageType.Error_EmbeddedIEMode, out _))
                {
                    args.WindowInfo = tabPage.BrowserWindowInfo ?? args.WindowInfo;
                }
                throw ex;
            }
            if (args.isMove)
            {
                var center = node.BuildRelativeElementBounding().Center();
                if (!tabPage.GetIntoViewPortRect().Contains(center))
                    node.ScrollIntoViewIfNeeded_V2();
            }
            return node;
        }

        public string GetElementFromSelectorAll(GetElementFromSelectorParams args)
        {
            logger.LogInformation($"{this.GuiType.ToString()} 进入GetElementFromSelector");
            string retData = string.Empty;//返回值数据，根据resultAttrName值返回不同的数据
            ExtractViewData.SelectCommonProps(args.Selector, out string selectorType, out string version);
            //实例tab对象，在匹配元素过程中，后边补tab的必要数据，如tabID
            BrowserTabPage tabPage = this.Browser.CreateBrowserTabPage(new ParamCreateBrowserTabPageModel
            {
                RunParam = args.RunParam,
                WebWindowInfo = (BrowserWindow)args.WindowInfo,
                TabId = ((BrowserWindow)args.WindowInfo).SpecifiedTabId
            });
            try
            {
                if (version.StartsWith("1."))
                {
                    string resultType = args.GetExtendParamValue("resultAttrName", string.Empty);
                    string resultAttrName = resultType.EqualIgnoreCase("selector") ? string.Empty : resultType;
                    string filter = args.GetExtendParamValue("filter", string.Empty);
                    //如果要返回的结果是selector,会在本层做逻辑处理,本参数置空；其他返回值类型需要到浏览器中获取，所以参数继续传递
                    string positionNum = args.GetExtendParamValue("positionNum", string.Empty);
                    var relativeType = args.GetExtendParamValue("relativeType", ElementRelativeType.Self);
                    var cssSelector = ExtractViewData.GetQueryModeSelector(args.Selector, out string queryMode);
                    var selector = UiCommon.ExtractSelectors(args.Selector);

                    retData = tabPage.GetElementFromSelectorAll(selector, version, queryMode, cssSelector, resultAttrName, relativeType, positionNum, filter, args.isActive, args.DiscernHidden);
                    if (resultType.EqualIgnoreCase("selector"))
                    {
                        var ids = retData.ToObject<List<string>>();
                        JArray inputSelectorPropsArr = JArray.Parse(selector);
                        var nodes = tabPage.CreateElementGuiNodes(ids);
                        retData = nodes.Select(node =>
                        {
                            var nodeSelector = node.PageElement.BuildSelector(false);
                            JArray selectorPropsArr = JArray.Parse(nodeSelector);
                            selectorPropsArr[0] = inputSelectorPropsArr[0];

                            return SelectorShell.RepackShell(selectorPropsArr, this.GuiType).ToJsonNoneFormating();
                        }).ToJson();
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(retData) || retData.Equals("[]"))
                            throw new RPAException(eErrorMessageType.Error_GetRegionError);
                        if (resultAttrName == "bounding")
                        {
                            var ele = tabPage.GetElementFromSelector(selector, cssSelector, false, version, queryMode, args.UserDefinedId, args.DiscernHidden);
                            var intoViewRect = tabPage.GetIntoViewPortRect();
                            var center = ele.BuildRelativeElementBounding().Center();
                            if (!intoViewRect.Contains(center))
                            {
                                ele.ScrollIntoViewIfNeeded_V2();
                                retData = tabPage.GetElementFromSelectorAll(selector, version, queryMode, cssSelector, resultAttrName, relativeType, positionNum, filter, args.isActive, args.DiscernHidden);
                            }
                        }
                    }
                }
                else
                {
                    retData = tabPage.GetElementFromSelectorAllOld(args.Selector);
                }
            }
            catch (Exception ex)
            {
                if (ex.IsRPAException(eErrorMessageType.Error_EmbeddedIEMode, out _))
                {
                    args.WindowInfo = tabPage.BrowserWindowInfo ?? args.WindowInfo;
                }
                throw ex;
            }
            return retData;
        }
        public IGuiNode GetElementById(GetElementByIdParams @params)
        {
            int tabID = @params.TabId > 0 ? @params.TabId : -1;
            BrowserTabPage tabpage = this.Browser.AcquireCurrentActiveTab();
            return tabpage.CreateElementGuiNode(@params.ElementId);
        }
        #endregion

        #region 获取激活页 拾取过程中 

        /// <summary>
        /// 获取当前激活的TabPage,数据可能来自与缓存，且只有每个Dom都有独立句柄的浏览器 才适用
        /// </summary>
        /// <param name="wnd"></param>
        /// <returns></returns>
        protected virtual BrowserTabPage GetCurActiveTab(IntPtr wnd)
        {
            if (wnd == IntPtr.Zero)
            {
                return this.Browser.AcquireCurrentActiveTab();
            }
            if (!this.BrowserTabpageCache.ContainsKey(wnd))
            {
                var curActiveTab = this.Browser.AcquireCurrentActiveTab();
                //为0的dom 不走缓存
                if (curActiveTab.ClientWndHandle == IntPtr.Zero)
                {
                    return curActiveTab;
                }
                //wnd 未必和ClientWndHandle相等，这是只做wnd 和 curActiveTab的映射
                this.BrowserTabpageCache.TryAdd(wnd, curActiveTab);
            }
            else
            {
                logger.LogDebug($"GetCurActiveTab Use Cache");
                this.BrowserTabpageCache[wnd].ClearTabDetail();
                //this.BrowserTabpageCache[wnd].SyncDocumentRectangle();
                logger.LogDebug($"GetCurActiveTab Use Cache end");

            }
            return this.BrowserTabpageCache[wnd];
        }

        #endregion

        #region 数据采集
        public List<SelectorProps> GetSameParentsProps(ParamCollectDataBySelectorModel paramModel)
        {
            List<SelectorProps> sameParentsProps = new List<SelectorProps>();
            if (paramModel.SelectorDataList.Count <= 1)
            {
                return sameParentsProps;
            }
            var firstSelector = paramModel.SelectorDataList.First();
            for (int i = 0; i < firstSelector.Selector.Count; i++)
            {
                bool sameProps = true;
                var referenceProp = firstSelector.Selector[i];
                for (int j = 1; j < paramModel.SelectorDataList.Count; j++)
                {
                    if (i > paramModel.SelectorDataList[j].Selector.Count - 1)
                    {
                        sameProps = false;
                        break;
                    }
                    if (!referenceProp.Equals(paramModel.SelectorDataList[j].Selector[i]))
                    {
                        sameProps = false;
                        break;
                    }
                }
                if (sameProps)
                {
                    sameParentsProps.Add(referenceProp);
                }
                else
                {
                    break;
                }
            }
            return sameParentsProps;
        }

        public List<List<string>> CollectDataBySelector(ParamCollectDataBySelectorModel paramModel)
        {
            List<List<string>> collectDataList = new List<List<string>>();
            BrowserTabPage tabPage = this.Browser.AcquireCurrentActiveTab();
            if (paramModel.IsTable)
            {
                var tableSelector = paramModel.SelectorDataList.FirstOrDefault()?.Selector;
                var selectorVersion = paramModel.SelectorDataList.FirstOrDefault()?.Version;
                var node = tabPage.GetElementFromSelector(tableSelector.ToJson(), null, false, selectorVersion, true);//匹配元素为空会抛出异常
                string data = node.GetTableData("");
                collectDataList = data.ToObject<List<List<string>>>();

            }
            else
            {
                //获取相同的父级
                List<SelectorProps> sameParentsProps = paramModel.SameParentsProps;
                collectDataList = tabPage.GetCollectDataFromSelector(sameParentsProps, paramModel.SelectorDataList);
            }
            return collectDataList;
        }

        public void ScrollIntoView(string srcoll)
        {
            var scrollObject = JsonConvert.DeserializeObject<JObject>(srcoll);
            var top = scrollObject["scrollTop"].ToString() == "" ? 0 : Convert.ToInt32(scrollObject["scrollTop"].ToString());
            var scrollVerticalDelay = Convert.ToInt32(scrollObject["ScrollVerticalDelay"].ToString());
            BrowserTabPage tabPage = this.Browser.AcquireCurrentActiveTab();
            if (top <= 0)
            {
                return;
            }
            var totalTopScroll = 0;
            Stopwatch sw = new Stopwatch();
            var onceVertical = 500;
            var scrollCount = top / onceVertical;
            for (int i = 0; i < scrollCount; i++)
            {
                if (i % 2 == 0)
                {
                    var curTopScroll = tabPage.GetBodyScroll(eScrollDirection.Top).ToString().As<int>();
                    if (curTopScroll >= top)
                    {
                        return;
                    }
                    if (totalTopScroll >= curTopScroll)
                    {
                        if (sw.ElapsedMilliseconds >= scrollVerticalDelay)
                        {
                            break;
                        }
                    }
                    else
                    {
                        totalTopScroll = curTopScroll;
                        sw.Restart();
                    }
                }
                tabPage.ScrollTop((i + 1) * onceVertical);

            }
            sw.Stop();
            tabPage.ScrollTop(top);
        }
        #endregion

        #region 数据采集-新版[version>=6.9.0]
        public List<SelectorProps> GetSameParentsProps_v2(ParamDataCollect_V2Model paramModel)
        {
            List<SelectorProps> sameParentsProps = new List<SelectorProps>();
            if (paramModel.DataCollectConfig.CollectionConfig.TableSelector.Count > 0)
            {
                return sameParentsProps;
            }
            var columnSelectorArray = paramModel.DataCollectConfig.CollectionConfig.ColumnSelectorArray;
            var firstSelector = columnSelectorArray.First();
            var iframeTags = ",iframe,frame,shadow-root,";
            var iframeLastIdx = firstSelector.FindLastIndex(sp => iframeTags.IndexOf($",{sp.Name},", StringComparison.OrdinalIgnoreCase) >= 0);
            if (iframeLastIdx >= 0)
            {
                sameParentsProps = firstSelector.Take(iframeLastIdx + 1).ToList();
                string baseTags = string.Join(",", sameParentsProps.Select(sp => sp.Name));
                for (int j = 1; j < columnSelectorArray.Count; j++)
                {
                    if (iframeLastIdx > columnSelectorArray[j].Count - 1)
                    {
                        throw new RPAException(eErrorMessageType.Error_DataCollectionV2_DiffIframe);//所有列必须同域
                    }
                    string tags = string.Join(",", columnSelectorArray[j].Take(iframeLastIdx + 1).Select(sp => sp.Name));
                    if (baseTags != tags)
                    {
                        //请确认所有要采集的列元素位于同一iframe内。
                        throw new RPAException(eErrorMessageType.Error_DataCollectionV2_DiffIframe);//所有列必须同域
                    }
                }
            }
            if (columnSelectorArray.Count == 1)
            {
                return sameParentsProps;
            }
            return sameParentsProps;
        }

        public List<List<string>> CollectData_v2(ParamDataCollect_V2Model paramModel)
        {
            List<List<string>> collectDataList = new List<List<string>>();
            BrowserTabPage tabPage = this.Browser.CreateBrowserTabPage(new ParamCreateBrowserTabPageModel()
            {
                WebWindowInfo = (BrowserWindow)paramModel.WindowInfo,
                TabId = ((BrowserWindow)paramModel.WindowInfo).SpecifiedTabId
            });
            try
            {
                collectDataList = tabPage.GetCollectDataFromConfig(paramModel);
            }
            catch (RPAException ex)
            {
                if (ex.IsRPAException(eErrorMessageType.Error_EmbeddedIEMode, out _))
                {
                    paramModel.WindowInfo = tabPage.BrowserWindowInfo ?? paramModel.WindowInfo;
                }
                throw ex;
            }

            //预览数据时，如果没有获取到数据，进行是否是采集页判断
            if (collectDataList.Count == 0 && paramModel.IsPreview)
            {
                if (!IsCollectionPage(paramModel.DataCollectConfig.PageInfo, tabPage.Url, tabPage.Title))
                {
                    throw new RPAException(this.GuiType, eErrorMessageType.Error_DataCollectionV2_NotCollectionPage);
                }
            }
            return collectDataList;
        }
        public (List<List<string>> collectionData, IGuiNode markedElement) GetCollectWaterfallDataFromConfigList(ParamDataCollect_V2Model paramModel)
        {
            List<List<string>> collectDataList = new List<List<string>>();
            BrowserTabPage tabPage;
            if (paramModel.MarkedElement != null)
            {
                tabPage = ((ElementGuiNode)paramModel.MarkedElement).PageElement.TabPage;
            }
            else
            {
                tabPage = this.Browser.CreateBrowserTabPage(new ParamCreateBrowserTabPageModel()
                {
                    WebWindowInfo = (BrowserWindow)paramModel.WindowInfo,
                    TabId = ((BrowserWindow)paramModel.WindowInfo).SpecifiedTabId
                });
            }
            try
            {
                return tabPage.GetCollectWaterfallDataFromConfig(paramModel);
            }
            catch (RPAException ex)
            {
                if (ex.IsRPAException(eErrorMessageType.Error_EmbeddedIEMode, out _))
                {
                    paramModel.WindowInfo = tabPage.BrowserWindowInfo ?? paramModel.WindowInfo;
                }
                throw ex;
            }
        }
        private bool IsCollectionPage(PageInfo collectionPageInfo, string currentUrl, string currentTitle)
        {
            if (collectionPageInfo == null)
            {
                return true;
            }
            if (!string.IsNullOrEmpty(currentUrl))
            {
                var count = currentUrl.IndexOf('?');
                if (count > 0)
                    currentUrl = currentUrl.Substring(0, count);
            }

            bool isSamePage = false;
            if (collectionPageInfo.WindowTitle == currentTitle)
            {
                isSamePage = true;
            }
            if (!isSamePage && collectionPageInfo.PageUrl.Contains(currentUrl))
            {
                isSamePage = true;
            }
            return isSamePage;
        }
        #endregion

        public string GetBoundings(string jsonParams)
        {
            BrowserTabPage tabPage = this.Browser.AcquireCurrentActiveTab();
            throw new NotImplementedException();
        }
        public string BuildSelectors(string jsonParams)
        {
            throw new NotImplementedException();
        }
        public string BuildSelectors(BulidSelectorsParams @params)
        {
            int tabID = @params.TabId > 0 ? @params.TabId : -1;
            BrowserTabPage tabpage = this.Browser.AcquireCurrentActiveTab();
            var eleIds = JsonConvert.DeserializeObject<string[]>(@params.ElementIds);
            List<string> selectorList = new List<string>();
            for (int i = 0; i < eleIds.Length; i++)
            {
                selectorList.Add(tabpage.CreateElementGuiNode(eleIds[i]).GetSelector(false));
            }

            return selectorList.ToJsonNoneFormating();

        }

        public string BuildSelectors(ParamBuildSelectorsModel paramModel)
        {
            throw new NotImplementedException();
        }

        public List<JObject> GetSimilarOrDataCollectElements(GetElementsAllParams @params)
        {
            throw new NotImplementedException();
        }
        public IGuiNode[] QuerySelectorAll(GetElementFromSelectorParams args)
        {
            logger.LogInformation($"{this.GuiType.ToString()} 进入QuerySelectorAll");
            string retData = string.Empty;//返回值数据，根据resultAttrName值返回不同的数据
            ExtractViewData.SelectCommonProps(args.Selector, out string selectorType, out string version);
            //实例tab对象，在匹配元素过程中，后边补tab的必要数据，如tabID
            BrowserTabPage tabPage = this.Browser.CreateBrowserTabPage(new ParamCreateBrowserTabPageModel()
            {
                RunParam = args.RunParam,
                WebWindowInfo = (BrowserWindow)args.WindowInfo,
                TabId = ((BrowserWindow)args.WindowInfo).SpecifiedTabId
            });
            string resultType = args.GetExtendParamValue("resultAttrName", string.Empty);
            string resultAttrName = resultType.EqualIgnoreCase("selector") ? string.Empty : resultType;
            string filter = args.GetExtendParamValue("filter", string.Empty);
            //如果要返回的结果是selector,会在本层做逻辑处理,本参数置空；其他返回值类型需要到浏览器中获取，所以参数继续传递
            string positionNum = args.GetExtendParamValue("positionNum", string.Empty);
            var relativeType = args.GetExtendParamValue("relativeType", ElementRelativeType.Self);
            var cssSelector = ExtractViewData.GetQueryModeSelector(args.Selector, out string queryMode);
            var selector = UiCommon.ExtractSelectors(args.Selector);

            try
            {
                retData = tabPage.GetElementFromSelectorAll(selector, version, queryMode, cssSelector, resultAttrName, relativeType, positionNum, filter, args.isActive, args.DiscernHidden);
                var ids = retData.ToObject<List<string>>();
                var nodes = tabPage.CreateElementGuiNodes(ids);
                if (args.isMove)
                {
                    var firstElement = nodes?.FirstOrDefault();
                    var intoViewRect = tabPage.GetIntoViewPortRect();
                    var center = firstElement.BuildRelativeElementBounding().Center();
                    if (!intoViewRect.Contains(center))
                    {
                        firstElement.ScrollIntoViewIfNeeded_V2();
                    }
                }
                return nodes.ToArray<IGuiNode>();
            }
            catch (Exception ex)
            {
                if (ex.IsRPAException(eErrorMessageType.Error_EmbeddedIEMode, out _))
                {
                    args.WindowInfo = tabPage.BrowserWindowInfo ?? args.WindowInfo;
                }
                throw ex;
            }

        }
        /// <summary>
        /// web类型元素窗口激活
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool VerifyWindowOfNode(GetElementFromSelectorParams args)
        {
            BrowserTabPage tabPage = this.Browser.CreateBrowserTabPage(new ParamCreateBrowserTabPageModel
            {
                RunParam = args.RunParam,
                WebWindowInfo = (BrowserWindow)args.WindowInfo
            });
            try
            {
                var selector = UiCommon.ExtractSelectors(args.Selector);
                tabPage.VerifyTab(selector);
            }
            catch (Exception e)
            {
                logger.LogWarning($"校验目标窗口异常:{e}");
                return false;
            }
            return true;
        }

        public IGuiOperator GetGuiOperator(MatchConfig matchConfig)
        {
            return this;
        }

        public string GetTargetElementInfo(GetTargetElementInfoModelParams @params)
        {
            throw new NotImplementedException();
        }
    }
}

