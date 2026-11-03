using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using FlaUI.Core.AutomationElements;
using II.RPA.Logging;
using II.RPA.Logging.Formatting;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.Core.UI;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.FacDialog;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using II.RPA.Plugin.GUI.Params;
using II.RPA.Plugin.GUI.Utils;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Plugins.Chatrpa
{
    public class DataCollectConfig : GuiComponentBase<ParamDataCollectConfigModel>
    {
        private ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.DataCollectConfig;
        private readonly UIProvider m_UIProvider;
        private string sourceType = "";
        private GuiElement columnElement;
        private string retDataCollectStr = "";
        private bool isFirstCapture = true;
        private PageInfo currentPageInfo = null;
        private bool isTable = false;
        private string turnPageElementSelector = "";
        RetDataCollectConfigurationModel_v2 retCaptureModel;
        public DataCollectConfig()
        {
            m_UIProvider = new UIProvider();
        }
        protected override JMessage Execute(ParamDataCollectConfigModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            try
            {
                FacMessageHelper.SendShowOrHiddenMainWndActionToFactory(false);
                IntPtr windowHandle = paramModel.WndHandle;
                sourceType = paramModel.sourceType;
                bool userLast = false;
                ZbrainAPIService.NotifyGetWindowImg_ChatRpa(windowHandle, "", ref userLast);
                ShowScanWnd(paramModel.WndHandle);
                bool useNormalGuiType = paramModel.sourceType.EqualIgnoreCase("normal") || paramModel.sourceType.EqualIgnoreCase("webCombineCv");
                string opGuiType = useNormalGuiType ? paramModel.guiType : eGuiType.CV.ToString();
                var guiOperator = (IGuiOperator_6_9_0)GuiOperatorProvider.Instance.GetGuiOperator(opGuiType, paramModel.WindowInfo);
                string elesData = "";
                IGuiOperator_6_9_0 getAllData_GuiOperator = (IGuiOperator_6_9_0)guiOperator;
                if (paramModel.sourceType.EqualIgnoreCase("webCombineCv"))
                {
                    var data = ((IGuiWebOperator)guiOperator).GetViewElementsData(new Commons.GetElementsAllParams
                    {
                        WindowHandle = paramModel.WndHandle,
                        ClsNames = paramModel.clsNames,
                        TagNames = paramModel.TagNames,
                        AttrKeywords = paramModel.AttrKeywords,
                        PrevCVSelector = paramModel.prevCVSelector,
                        CVUseLastData = userLast,
                        SpecialOperation = paramModel.SpecialOperation,
                        TabId = -1,
                    });
                    elesData = data.ToJsonNoneFormating();
                    logger.LogDebug($"GetViewElementsData：{elesData}");
                    getAllData_GuiOperator = (IGuiOperator_6_9_0)GuiOperatorProvider.Instance.GetGuiOperator(eGuiType.CV);
                }
                var cvRetElesData = getAllData_GuiOperator.GetSimilarOrDataCollectElements(new GetElementsAllParams
                {
                    WindowHandle = paramModel.WndHandle,
                    ClsNames = paramModel.clsNames,
                    TagNames = paramModel.TagNames,
                    AttrKeywords = paramModel.AttrKeywords,
                    PrevCVSelector = paramModel.prevCVSelector,
                    CVUseLastData = userLast,
                    SpecialOperation = paramModel.SpecialOperation,
                    TabId = -1, //暂时使用激活页，口子预留,
                    ViewElementsData = elesData,
                    SourceType = paramModel.sourceType,
                    TaskInfo = paramModel.TaskInfo,
                    ActionName = "data_collect"
                });
                //进行列数据采集配置
                AutoCaptureDataCollect(paramModel.guiType, cvRetElesData, paramModel);
                msg.Info = retDataCollectStr;
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_ChatDataCollectConfig, ref msg);
            }
            finally
            {
                isFirstCapture = true;
                currentPageInfo = null;
                isTable = false;
                turnPageElementSelector = "";
                retCaptureModel = null;
            }
            return msg;
        }

        protected override void ValidateParameter(ParamDataCollectConfigModel paramModel) {
            try
            {
                if (paramModel.windowType.EqualIgnoreCase("web"))
                {
                    var browserWndHandle = IntPtr.Zero;
                    if (string.IsNullOrEmpty(paramModel.guiType) || GlobleConfigurations.UseSZBrowser == true)
                    {
                        var windowInfo = BrowserWindowProvider.GetFirstBrowserByZOrder();
                        paramModel.guiType = windowInfo.WindowType.ToString();
                        browserWndHandle = windowInfo.Handle;
                        paramModel.WindowInfo = windowInfo;
                    }
                    else
                    {
                        eGuiType guiType = (eGuiType)Enum.Parse(typeof(eGuiType), paramModel.guiType.ToString(), true);
                        var windowInfo = BrowserWindowProvider.GetFirstBrowserByZOrder(null, guiType);
                        paramModel.guiType = windowInfo.WindowType.ToString();
                        browserWndHandle = windowInfo.Handle;
                        paramModel.WindowInfo = windowInfo;
                    }
                    paramModel.WndHandle = browserWndHandle;
                    logger.LogDebug($"GetZindexFirstBrowser：{paramModel.windowType}，{paramModel.guiType},{paramModel.WndHandle}");
                }
                else
                {
                    IntPtr hWnd = IntPtr.Zero;
                    //针对虚拟桌面启动后，获取激活窗口句柄为0的情况，做最多3000秒等待
                    RPAUtils.Retry(() =>
                    {
                        hWnd = ForegroundWinUtils.GetForegroundWindow();
                        return hWnd != IntPtr.Zero;
                    }, 3000, 200);
                    paramModel.guiType = "windows";
                    paramModel.WndHandle = hWnd;

                }
                return;
            }
            catch (Exception ex)
            {
                logger.LogInformation($"参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        private void ShowScanWnd(IntPtr wndHandle)
        {
            var windowRect = RectHelper.GetWindowRect(wndHandle);
            m_UIProvider?.CloseScanAnimationWnd();
            logger.LogDebug($"开始构建弹出扫描动画界面");
            m_UIProvider.ShowScanAnimationWnd(windowRect.X, windowRect.Y, windowRect.Width, windowRect.Height,
                (sender, e) =>
                {
                    logger.LogDebug($"关闭扫描动画界面");
                    m_UIProvider?.CloseScanAnimationWnd();
                });

        }
        private void AutoCaptureDataCollect(string guiType, List<JObject> elesData, ParamDataCollectConfigModel paramData)
        {
            if (elesData.Count < 1)
            {
                throw new Exception("未挑选到有效的数据采集元素".I18n("controlType_DataCollectConfig_E54F5BD6"));
            }
            int columIndex = 0;
            foreach (var edata in elesData)
            {
                if (sourceType.EqualIgnoreCase("normal") ||
                   sourceType.EqualIgnoreCase("webCombineCv"))
                {
                    columnElement = GetGuiElementByElementId(guiType, edata);
                }

                else
                {
                    columnElement = GetGuiElementByCvToNomal(guiType, edata);
                }
                DoSelect(columnElement, paramData, columIndex);
                columIndex++;
            }
            if (retCaptureModel != null)
            {
                JObject retDataResult = new JObject();
                retDataResult["config"] = JObject.FromObject(retCaptureModel, new JsonSerializer { NullValueHandling = NullValueHandling.Ignore });
                retDataResult["uiSelector_page"] = turnPageElementSelector;
                retDataCollectStr = retDataResult.ToString();
            }
        }

        #region 获取GuiElement
        private GuiElement GetGuiElementByElementId(string guiType, JObject eleData)
        {
            var guiElement = GuiElement.FromElementId(guiType,
                    new GetElementByIdParams
                    {
                        TabId = -1,
                        ElementId = eleData["uniqueId"].ToString()
                    });
            if (guiElement != null)
            {
                //SaveElementShotImg(guiElement, paramModel.AFilePath);
                return guiElement;
            }
            return null;
        }

        private GuiElement GetGuiElementByCvToNomal(string guiType, JObject eleData)
        {
            var elementDataObj = eleData;
            var cvRect = elementDataObj["area3"].ToObject<RpaRect>();
            var point = cvRect.Center;
            if (Configurations.Info.CvToNomalMoveMouse)
            {
                II.RPA.Plugin.GUI.Commons.MouseMotions.MouseMotionHelper.PersonMoveMouse(true, point);
            }
            var hWndList = UiCommon.GetRootWindowList(point);
            AutomationElement rootElement = null;
            AutomationElement pointElement = null;
            if (hWndList.Count > 0)
            {
                foreach (IntPtr hWnd in hWndList)
                {
                    if (hWnd != IntPtr.Zero)
                    {
                        rootElement = UiCommon.UIAAutomation.FromHandle(hWnd);   //获取根窗体
                    }
                    if (rootElement == null)
                    {
                        break;
                    }
                    if (rootElement.Properties.BoundingRectangle.IsSupported && rootElement.BoundingRectangle.Contains(point))
                    {
                        break;
                    }
                }
                pointElement = UiCommon.UIAAutomation.FromHandle(hWndList.Last());
            }
            var guiOperator = (IGuiOperator_6_9_0)GuiOperatorProvider.Instance.GetGuiOperator(guiType);
            IGuiNode inspectNode = null;
            bool inspectResult = false;
            if (guiOperator.IsCanUse(point, rootElement, pointElement))
            {
                inspectResult = guiOperator.InspectNode(point, rootElement, pointElement, "", out inspectNode);
            }
            else
            {
                logger.LogInformation($"获取selector失败,GUI类型未匹配,未找到普通元素");
            }
            if (inspectResult && inspectNode != null)
            {
                var guiElement = new GuiElement(inspectNode);
                var newArea = guiElement.BoundingRectangle;
                logger.LogDebug($"CvToNomal new ele area:{newArea.ToString()} ");
                var intersectArea = Rectangle.Intersect(newArea, cvRect.ToRectangle());
                if (!intersectArea.IsEmpty && intersectArea.Width * intersectArea.Height >= newArea.Width * newArea.Height * Configurations.Info.IntersectRatio)//有60%以上 普通元素重叠区
                {
                    //var nodeSelector = guiElement.Selector;
                    //SaveElementShotImg(guiElement, paramModel.AFilePath);
                    return guiElement;
                }
            }
            return null;

        }
        #endregion

        #region 采集数据

        private void DoSelect(GuiElement guiElement, ParamDataCollectConfigModel paramData, int columnIndex)
        {
            //DoSelect 后是否需要fac重新开始拾取
            if (retCaptureModel == null)
            {
                retCaptureModel = new RetDataCollectConfigurationModel_v2();
            }
            try
            {
                var node = guiElement.uiNode as IGuiNode_6_1_0;
                var isGuiWebNode = node is IGuiWebNode_690;

                if (!isFirstCapture)
                {
                    if (currentPageInfo?.WindowType == "web" && !isGuiWebNode)
                    {
                        //MessageHelper.ShowDialog("选择采集类型与采集目标元素类型不符，无法采集此类元素数据。");
                        throw new RPAException(eErrorMessageType.Error_DataCollection_ElementTypeError);
                    }
                    if (currentPageInfo?.WindowType != "web" && isGuiWebNode)
                    {
                        //MessageHelper.ShowDialog("选择采集类型与采集目标元素类型不符，无法采集此类元素数据。");
                        throw new RPAException(eErrorMessageType.Error_DataCollection_ElementTypeError);
                    }
                }

                if (isTable)
                {
                    var rpaex = new RPAException(eErrorMessageType.Error_DataCollection_CapturedTableElement);
                    throw rpaex;
                }
                var element = guiElement;
                //首次数据采集拾取时，判断是否是表格
                bool isUseTableElement = false;
                if (isFirstCapture)
                {
                    var eleIsTable = element.ElementIsTable();
                    isUseTableElement = paramData.iSEnableTable == 1 && eleIsTable;
                    isTable = isUseTableElement;
                }
                logger.LogDebug($"get selector UIElement_OnSelected");
                string orgSelector = GetCaptureElementSelector(element, isUseTableElement, isGuiWebNode);
                var pageInfo = this.GetPageInfo(orgSelector, out var selectorObject);
                var isSamePage = this.IsSamePage(currentPageInfo, pageInfo);
                if (!isSamePage)
                {
                    var rpaex = new RPAException(eErrorMessageType.Error_DataCollectionV2_DifferentPages);
                    throw rpaex;
                }
                if (isUseTableElement)
                {
                    retCaptureModel.CollectionConfig.TableSelector = selectorObject;
                }
                else
                {
                    var retSelectorObj = ProcessSelector(element, selectorObject);
                    retCaptureModel.CollectionConfig.ColumnSelectorArray.Add(retSelectorObj);
                    var defColumnConfig = new ColumnConfig();
                    if (isGuiWebNode)
                    {
                        defColumnConfig.ColumnAttributes.AddRange((node as IGuiWebNode_690).GetUsableAttributes());
                        switch (retSelectorObj?.Last()?.Name?.ToLower())
                        {
                            case "img":
                                defColumnConfig.DataAttrName = "src";
                                break;
                        }
                        #region 动态选取列数据类型
                        string columnType = "";
                        //列数据类型入参为空或数量对不上，则做默认值文本处理
                        if (paramData.ColumnType == null || paramData.ColumnType.Count <= columnIndex)
                        {
                            columnType = "text";
                        }
                        else
                        {
                            columnType = paramData.ColumnType[columnIndex];
                        }
                        switch (columnType)
                        {
                            case "text":
                                defColumnConfig.DataAttrName = "innerText";
                                break;
                            case "linkurl":
                                defColumnConfig.DataAttrName = "href";
                                break;
                            case "imgurl":
                                defColumnConfig.DataAttrName = "src";
                                break;
                            default:
                                defColumnConfig.DataAttrName = "innerText";
                                break;
                        }
                        #endregion
                    }
                    retCaptureModel.CollectionConfig.ColumnConfigArray.Add(defColumnConfig);
                }
                if (isFirstCapture)
                {
                    retCaptureModel.PageInfo = pageInfo;
                    this.GetTurningPageElementSelector(element, isFirstCapture);
                    currentPageInfo = pageInfo;
                    isFirstCapture = false;
                }
                retCaptureModel.CollectionConfig.SelectorVersion = GlobleConfigurations.CurrentSelectorVersion;
                logger.LogDebug($"RetCaptureModel ：{retCaptureModel.ToJson()}");
                logger.LogDebug($"Send Capture Data To Factory End");
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "完成拾取动作时发生了异常，本次拾取结束");
            }
            finally
            {
            }

        }

        private string GetCaptureElementSelector(GuiElement element, bool isUseTableElement, bool isGuiWebNode)
        {
            if (!isGuiWebNode)
            {
                return isUseTableElement ? element.TableSelector : element.Selector;
            }
            if (isUseTableElement)
            {
                element.TryGetTableElement(out var tableNode);
                var tableElement = new GuiElement((IGuiNode)tableNode);
                return tableElement.GetColletionColumnSelector();
            }
            else
            {
                return element.GetColletionColumnSelector();
            }
        }
        private PageInfo GetPageInfo(string selectorJson, out List<SelectorProps> selectorObject)
        {
            try
            {
                PageInfo pageInfo = new PageInfo();
                JToken jToken = JToken.Parse(selectorJson);
                if (jToken is JObject)
                {
                    var common = selectorJson.ToObject<RPACommon>();
                    selectorObject = common.Selector.ToObject<List<SelectorProps>>();
                    pageInfo.WindowType = common.CommonProps.GUIType.ToLower();
                    pageInfo.ProcessName = common.CommonProps.ProcessName;
                }
                else
                {
                    selectorObject = selectorJson.ToObject<List<SelectorProps>>();
                }
                var tabProps = selectorObject.Find(o => o.Name == ConstFields.DATACOLECTING_SELECTORPROPS_TAB);
                if (tabProps != null)
                {
                    selectorObject.Remove(tabProps);
                    var title = tabProps.Props.Find(p => p.Name == ConstFields.DATACOLECTING_PAGE_TITLE)?.Value ?? string.Empty;
                    var url = tabProps.Props.Find(p => p.Name == ConstFields.DATACOLECTING_PAGE_URL)?.Value ?? string.Empty;
                    pageInfo.WindowTitle = title;
                    pageInfo.PageUrl = url;
                    pageInfo.WindowType = "web";
                }
                else
                {
                    var title = selectorObject.FirstOrDefault()?.Props.Find(p => p.Name == ConstFields.DATACOLECTING_WINDOW_TITLE)?.Value ?? string.Empty;
                    pageInfo.WindowTitle = title;
                }
                return pageInfo;
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw ex;
            }
        }
        private bool IsSamePage(PageInfo pageInfo1, PageInfo pageInfo2)
        {
            if (pageInfo1 == null || pageInfo2 == null)
            {
                return true;
            }
            if (pageInfo1.WindowType == "web")
            {
                string currentUrl = pageInfo2.PageUrl;
                if (!string.IsNullOrEmpty(currentUrl))
                {
                    var count = currentUrl.IndexOf('?');
                    if (count > 0)
                        currentUrl = currentUrl.Substring(0, count);
                }

                bool isSamePage = false;
                if (pageInfo1.WindowTitle == pageInfo2.WindowTitle)
                {
                    isSamePage = true;
                }

                if (!isSamePage && pageInfo1.PageUrl.Contains(currentUrl))
                {
                    isSamePage = true;
                }
                return isSamePage;
            }
            else
            {
                return pageInfo1.WindowType == pageInfo1.WindowType && pageInfo1.WindowTitle == pageInfo2.WindowTitle;

            }
        }

        private List<SelectorProps> ProcessSelector(GuiElement element, List<SelectorProps> selectorObject)
        {
            try
            {
                List<SelectorProps> processedSelector = null;
                if (!(element.uiNode is IGuiWebNode_690))
                {
                    processedSelector = selectorObject;
                }
                else
                {
                    processedSelector = element.GetSimilarElementSelector(selectorObject);
                }
                return processedSelector;
            }
            catch (RPAException rpaex)
            {
                if (rpaex.ErrorMessageType == eErrorMessageType.Error_ElementNode_Error)
                {
                    return selectorObject;
                }
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, rpaex, "处理拾取得到的selector异常");
                throw rpaex;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "处理拾取得到的selector异常");
                throw ex;
            }
        }
        private void GetTurningPageElementSelector(GuiElement element, bool FirstCapture)
        {
            Task.Run(() =>
           {
               try
               {
                   //只有数据采集拾取，且为首次拾取才执行获取下一页逻辑
                   if (!FirstCapture)
                   {
                       return string.Empty;
                   }
                   var guiNode = element.GetTurningPageElement();
                   if (guiNode == null)
                   {
                       return string.Empty;
                   }
                   string selector = (guiNode as IGuiNode_6_6_0).GetSelector(true);
                   if (!string.IsNullOrEmpty(selector))
                   {
                       var common = JsonConvert.DeserializeObject<RPACommon>(selector);
                       common.CommonProps.ResDirPath = $@"res\xxxxx";//没有路径前端可能会误删
                       string screenShotPath = string.Empty;//缩略图地址
                       string fileName = Guid.NewGuid().ToString() + ".png";
                       screenShotPath = Path.Combine("res", fileName);
                       string screenShotFilePath = System.IO.Path.Combine(GlobleConfigurations.PackageDirectory, screenShotPath);
                       using (Stream sm = this.GetType().Assembly.GetManifestResourceStream($"II.RPA.Plugin.GUI.DataCollection.Assets.Images.TurnPageDefultImg_Embedded.png"))
                       {
                           using (var img = new Bitmap(sm))
                           {
                               if (!Directory.Exists(Directory.GetParent(screenShotFilePath).FullName))
                               {
                                   Directory.CreateDirectory(Directory.GetParent(screenShotFilePath).FullName);
                               }
                               img.Save(screenShotFilePath);
                           }
                       }
                       common.CommonProps.filePath = screenShotPath;
                       selector = common.ToJson();
                       //FacMessageHelper.SendPageTurnButtonElement(selector);
                       turnPageElementSelector = selector;
                   }
                   return selector;
               }
               catch (Exception ex)
               {
                   Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "自动获取翻页按钮逻辑发生异常，用户可以人为再次拾取，忽略此异常");
                   return string.Empty;
               }
           }).Wait();
        }

        #endregion
    }
}


