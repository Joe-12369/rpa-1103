using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using Fluent.Localization.Languages;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.Core.UI;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.Plugin.GUI.Params;
using II.RPA.Plugin.GUI.ScreenShot;
using II.RPA.Plugin.GUI.Utils;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class GetTargetElementInfo : GuiComponentBase<GetTargetElementInfoModel>
    {
        private ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.GetElementSelector;
        private readonly UIProvider m_UIProvider;
        public string webCode = "";

        private string sourceType = "";
        private GuiElement columnElement;
        private string retDataCollectStr = "";
        private bool isFirstCapture = true;
        private PageInfo currentPageInfo = null;
        private bool isTable = false;
        private string turnPageElementSelector = "";
        RetDataCollectConfigurationModel_v2 retCaptureModel;
        private WindowObject currentWinObject;
        public GetTargetElementInfo()
        {
            m_UIProvider = new UIProvider();
        }
        protected override JMessage Execute(GetTargetElementInfoModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            //此处代码逻辑细节需要待处理，先实现主体逻辑
            JMessage msg = new JMessage();
            CVResult agentResult = null;
            try
            {
                logger.LogInformation($"GetTargetElementInfo入参：{JsonConvert.SerializeObject(paramModel)}");
                bool userLast = false;
                //调用更新文案接口
                FacMessageHelper.SendUpdateSmallCardStepTextActionToFactory("正在分析任务…");
                if (paramModel.eventType == EventType.Visual)
                {
                    //隐藏执行小窗
                    FacMessageHelper.SendShowOrHiddenMainWndActionToFactory(false);
                    //调用zbrian截图
                    ZbrainAPIService.NotifyGetWindowImg_AiVision(IntPtr.Zero, paramModel.prevCVSelector, ref userLast);
                    msg.Info = new
                    {
                        action_type = "ai_vision"
                    }.ToJson();
                }
                else
                {
                    GlobleConfigurations.CaptureUniqueID = DateTime.Now.ToLongTimeStamp();
                    IntPtr windowHandle = paramModel.WndHandle;
                    //隐藏执行小窗
                    FacMessageHelper.SendShowOrHiddenMainWndActionToFactory(false);
                    if (!paramModel.noMapStep)
                    {
                        logger.LogDebug($"地图模式,激活目标窗口");
                        UiCommon.ForceShow(windowHandle, false);
                    }
                    //调用zbrian截图
                    ZbrainAPIService.NotifyGetWindowImg_Agent(windowHandle, paramModel.prevCVSelector, ref userLast);
                    if (paramModel.finishJudge)
                    {
                        //通知前端终态检查
                        logger.LogDebug($"终态检查前先限制执行小窗");
                        FacMessageHelper.SendShowOrHiddenMainWndActionToFactory(true);
                        logger.LogDebug($"通知终态检查");
                        FacMessageHelper.SendAgentCheckFinalStateResultToFactory();
                    }
                    else
                    {
                        //调用zbrian获取目标元素位置信息接口(去掉扫描效果-2026-04-27)
                        //ShowScanWnd(paramModel.WndHandle, userLast, false);
                    }
                    string opGuiType = eGuiType.CV.ToString();
                    var guiOperator = (IGuiOperator_6_9_0)GuiManager.Info.GuiOperators.First(
                              go => go.GuiType.ToString().EqualIgnoreCase(opGuiType)
                          );
                    IGuiOperator_6_9_0 getAllData_GuiOperator = (IGuiOperator_6_9_0)guiOperator;

                    var retCvElesData = getAllData_GuiOperator.GetTargetElementInfo(new GetTargetElementInfoModelParams
                    {
                        Step_Info = paramModel.taskInfo,
                        WindowHandle = windowHandle,
                        WebCode = webCode
                    });
                    agentResult = JsonConvert.DeserializeObject<CVResult>(retCvElesData);
                    if (agentResult.action_type == "click"
                        || agentResult.action_type == "drag"
                        || agentResult.action_type == "scroll"
                        || agentResult.action_type == "left_double"
                        || agentResult.action_type == "right_single"
                        || agentResult.action_type == "mouse_move")
                    {
                        int elementX = agentResult.absElementsPoint[0][0];
                        int elementY = agentResult.absElementsPoint[0][1];
                        List<List<int>> points_abs = new List<List<int>>();
                        points_abs.Add(new List<int> { elementX, elementY });
                        if (agentResult.elementsPoint.Count > 1 && agentResult.action_type == "drag")
                        {
                            points_abs.Add(new List<int> { agentResult.absElementsPoint[1][0], agentResult.absElementsPoint[1][1] });
                        }
                        logger.LogDebug($"点击或拖拽类型：目标元素相对坐标[{points_abs.ToJson()}]");
                        msg.Info = new
                        {
                            selector = "",
                            action_type = agentResult.action_type,
                            abs_elements = points_abs,
                            elements = agentResult.elementsPoint,
                            content = agentResult.content,
                            message = agentResult.message,
                            action_description = agentResult.action_description,
                            agentResult.model_output
                        }.ToJson();
                    }
                    else if (agentResult.action_type == "input" || agentResult.action_type == "closePopup")
                    {
                        var nodeSelector = this.GetElementSelectorByPoint(
                            new Point(agentResult.elementsPoint[0][0], agentResult.elementsPoint[0][1]), paramModel.AFilePath);
                        if (!string.IsNullOrEmpty(nodeSelector))
                        {
                            var common = JsonConvert.DeserializeObject<RPACommon>(nodeSelector);
                            common.CommonProps.filePath = paramModel.filePath;
                            common.CommonProps.ResDirPath = $@"res\{GlobleConfigurations.CaptureUniqueID}";
                            common.CommonProps.isSmartControl = "true";
                            msg.Info = new
                            {
                                selector = common.ToJsonNoneFormating(),
                                action_type = agentResult.action_type,
                                content = agentResult.content,
                                message = agentResult.message,
                                action_description = agentResult.action_description,
                                agentResult.model_output
                            }.ToJson();
                        }
                        else
                        {
                            logger.LogError("拾取目标元素异常");
                            throw new RPAException(eErrorMessageType.Error_GetElementSelectorError);
                        }
                    }
                    else if (agentResult.action_type == "dataCollect")
                    {
                        var windowInfo = BrowserWindowProvider.GetFirstBrowserByZOrder();
                        paramModel.guiType = windowInfo.WindowType.ToString();
                        paramModel.WndHandle = windowInfo.Handle;
                        paramModel.WindowInfo = windowInfo;
                        paramModel.SelectorGuiType = paramModel.guiType;
                        currentWinObject = windowInfo;
                        logger.LogDebug($"GetZindexFirstBrowser：{paramModel.guiType}，{paramModel.WndHandle}");

                        logger.LogDebug($"数据采集目标浏览器类型：{paramModel.guiType}");
                        var guiWebOperator = GuiOperatorProvider.Instance.GetGuiOperator(paramModel.guiType);
                        //实在浏览器会出现等待网页加载外完成时还会出现获取网页结构数据出现异常进行重试处理，先这样，后续让文一排查为何实在浏览器出现等待加载完成无效问题
                        int retryCount = 0;
                        while (retryCount < 3)
                        {
                            try
                            {
                                //如果是数据采集，先给整个网页标签赋值rpa-key
                                var data = ((IGuiWebOperator)guiWebOperator).GetViewElementsData(new Commons.GetElementsAllParams
                                {
                                    WindowHandle = paramModel.WndHandle,
                                    WindowInfo = currentWinObject,
                                    TabId = -1, //暂时使用激活页，口子预留
                                });
                                webCode = data.ToJsonNoneFormating();
                                break;
                            }

                            catch (Exception ex)
                            {
                                logger.LogWarning($"获取网页数据结构第{retryCount + 1}次异常：{ex}");
                                if (retryCount == 2)
                                {
                                    //throw ex;
                                    webCode = "";
                                    break;
                                }
                                retryCount++;
                                Thread.Sleep(1000);
                            }
                        }
                        retCvElesData = getAllData_GuiOperator.GetTargetElementInfo(new GetTargetElementInfoModelParams
                        {
                            Step_Info = paramModel.taskInfo,
                            WindowHandle = windowHandle,
                            WebCode = webCode
                        });
                        agentResult = JsonConvert.DeserializeObject<CVResult>(retCvElesData);
                        //非元素类组件处理
                        AutoCaptureDataCollect(paramModel.guiType, agentResult.ele_ids, new ParamDataCollectConfigModel
                        {
                            iSEnableTable = paramModel.dataCollect.ISEnableTable,
                            ColumnType = paramModel.dataCollect.ColumnType
                        }, agentResult.elementsPoint);
                        msg.Info = new
                        {
                            selector = retDataCollectStr,
                            action_type = agentResult.action_type,
                            content = agentResult.content,
                            message = agentResult.message,
                            action_description = agentResult.action_description,
                            agentResult.model_output
                        }.ToJson();
                    }
                    else
                    {
                        msg.Info = new
                        {
                            selector = "",
                            action_type = agentResult.action_type,
                            content = agentResult.content,
                            message = agentResult.message,
                            action_description = agentResult.action_description,
                            agentResult.model_output
                        }.ToJson();
                    }
                }
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_GetElementSelectorError, ref msg);
                if (agentResult != null)
                {
                    msg.Info = new
                    {
                        selector = "",
                        action_type = agentResult.action_type,
                        content = agentResult.content,
                        message = !string.IsNullOrEmpty(agentResult.message) ? agentResult.message : msg.Message,
                        action_description = agentResult.action_description,
                        agentResult.model_output
                    }.ToJson();
                }
                //如果在和zbrain交互前出现异常，将异常消息内容传给zbrian
                else
                {
                    msg.Info = new
                    {
                        selector = "",
                        action_type = "",
                        content = "",
                        message = msg?.Message,
                        action_description = "",
                        agentResult.model_output
                    }.ToJson();
                }
            }
            finally
            {
                webCode = "";
                isFirstCapture = true;
                currentPageInfo = null;
                isTable = false;
                turnPageElementSelector = "";
                retCaptureModel = null;
                FacMessageHelper.SendShowOrHiddenMainWndActionToFactory(true);
            }
            return msg;
        }

        protected override void ValidateParameter(GetTargetElementInfoModel paramModel) {
            if (paramModel.eventType == EventType.Visual)
            {
                return;
            }
            //此部分只管非浏览器的，浏览器部分在数据采集场景夏在处理
            IntPtr hWnd = IntPtr.Zero;
            //针对虚拟桌面启动后，获取激活窗口句柄为0的情况，做最多3000秒等待
            RPAUtils.Retry(() =>
            {
                hWnd = ForegroundWinUtils.GetForegroundWindow();
                return hWnd != IntPtr.Zero;
            }, 3000, 200);
            paramModel.WndHandle = hWnd;

            return;
        }


        private string GetElementSelectorByPoint(Point point, string filePath)
        {
            try
            {
                GuiElement guiElement = ElementProvider.Instance.CaptureElement(point);
                var nodeSelector = guiElement.Selector;
                SaveElementShotImg(guiElement, filePath);
                //为ai智能修复保存数据
                SaveElementAiRepairData(guiElement);
                return nodeSelector;
            }
            catch (Exception e)
            {
                logger.LogError(e);
                return null;
            }
        }
        private void SaveElementShotImg(GuiElement guiElement, string filePath)
        {
            using (var currentDesktopScreenshot = UiCommon.UIAAutomation.GetDesktop().Capture())
            {
                using (guiElement.CaptureInformativeScreenshot(filePath, currentDesktopScreenshot))
                { }
            }
        }
        private void SaveElementAiRepairData(GuiElement targetElement)
        {
            try
            {
                logger.LogDebug("SaveElementAiRepairData Start");
                AutomationElement element = null;
                Rectangle wndRect = Rectangle.Empty;
                Rectangle elementRect = targetElement.BoundingRectangle;
                if (elementRect.IsEmpty)
                {
                    return;
                }
                var targetPoint = elementRect.Center();
                List<IntPtr> parentLinked = UiCommon.GetRootWindowList(targetPoint);
                foreach (IntPtr hWnd in parentLinked)
                {
                    if (hWnd != IntPtr.Zero)
                    {
                        element = UiCommon.UIAAutomation.FromHandle(hWnd);   //获取根窗体
                    }
                    if (element == null)
                    {
                        logger.LogDebug("SaveElementAiRepairData 根据句柄获取根窗口异常");
                        return;//空窗口元素 终止方法
                    }
                    if (element.Properties.BoundingRectangle.IsSupported && element.BoundingRectangle.Contains(targetPoint))
                    {
                        wndRect = element.BoundingRectangle;
                        elementRect.X -= wndRect.X;
                        elementRect.Y -= wndRect.Y;
                        break;
                    }
                }
                JObject jObj = new JObject();
                JObject elementDataObj = new JObject();
                elementDataObj.Add(new JProperty("x", elementRect.X));
                elementDataObj.Add(new JProperty("y", elementRect.Y));
                elementDataObj.Add(new JProperty("width", elementRect.Width));
                elementDataObj.Add(new JProperty("height", elementRect.Height));
                jObj.Add(new JProperty(ConstFields.RES_SELECTOR_STATICDATA_ELEMENTDATA, elementDataObj));

                UiCommon.SaveStaticSelectorData(GlobleConfigurations.CaptureUniqueID, ConstFields.RES_SELECTOR_STATICFILE_NAME, jObj.ToString());

                Dictionary<string, object> wndInfo = new Dictionary<string, object>();
                wndInfo.Add("handle", (int)element.Properties.NativeWindowHandle.Value);
                wndInfo.Add("area", new[] { wndRect.X, wndRect.Y, wndRect.Width, wndRect.Height });
                ZbrainAPIService.SendUIDToZBrain(wndInfo);
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            logger.LogDebug("SaveElementAiRepairData End");
        }
        private void ShowScanWnd(IntPtr wndHandle, bool userLast, bool isShowFactoryWnd = true)
        {
            if (!userLast)
            {
                //全走整屏扫描
                wndHandle = InteropMethods.GetDesktopWindow();
                var windowRect = RectHelper.GetWindowRect(wndHandle);
                m_UIProvider?.CloseScanAnimationWnd();
                logger.LogDebug($"开始构建弹出扫描动画界面");
                m_UIProvider.ShowScanAnimationWnd(windowRect.X, windowRect.Y, windowRect.Width, windowRect.Height,
                    (sender, e) =>
                    {
                        logger.LogDebug($"关闭扫描动画界面");
                        m_UIProvider?.CloseScanAnimationWnd();
                        if (isShowFactoryWnd)
                        {
                            FacMessageHelper.SendShowOrHiddenMainWndActionToFactory(true);
                        }
                    });
            }
        }


        private void AutoCaptureDataCollect(string guiType, List<string> unqueIds, ParamDataCollectConfigModel paramData, List<List<int>> elementsPoint)
        {
            int columIndex = 0;
            //有unqueIds则优先使用，为空或无有效元素则通过坐标
            if (unqueIds != null && unqueIds.Count > 0)
            {
                logger.LogDebug($"根据unqueId生成数据采集配置");
                foreach (var unqueId in unqueIds)
                {
                    columnElement = GetGuiElementByElementId(guiType, unqueId);
                    if (columnElement == null)
                    {
                        logger.LogError($"生成Id：【{unqueId}】元素数据采集配置失败");
                        throw new RPAException(eErrorMessageType.Error_ChatDataCollectConfig);
                    }
                    DoSelect(columnElement, paramData, columIndex);
                    columIndex++;
                }
                //throw new Exception("未挑选到有效的数据采集元素");
            }
            else
            {
                logger.LogDebug($"根据坐标生成数据采集配置");
                if (elementsPoint == null || elementsPoint.Count < 1)
                {
                    throw new Exception("未挑选到有效的数据采集元素".I18n("controlType_GetTargetElementInfo_E54F5BD6"));
                }
                foreach (var elementPoint in elementsPoint)
                {
                    columnElement = ElementProvider.Instance.CaptureElement(new Point(elementPoint[0], elementPoint[1]));
                    if (columnElement == null)
                    {
                        logger.LogError($"生成坐标：【({elementPoint[0]},{elementPoint[1]})】下元素数据采集配置失败");
                        throw new RPAException(eErrorMessageType.Error_ChatDataCollectConfig);
                    }
                    DoSelect(columnElement, paramData, columIndex);
                    columIndex++;
                }
            }
            if (retCaptureModel != null)
            {
                JObject retDataResult = new JObject();
                retDataResult["config"] = JObject.FromObject(retCaptureModel, new JsonSerializer { NullValueHandling = NullValueHandling.Ignore });
                retDataResult["uiSelector_page"] = turnPageElementSelector;
                retDataCollectStr = retDataResult.ToString();
            }
        }


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

        private GuiElement GetGuiElementByElementId(string guiType, string uniqueId)
        {
            var guiElement = GuiElement.FromElementId(guiType,
                    new GetElementByIdParams
                    {
                        TabId = -1,
                        ElementId = uniqueId,
                        WindowInfo = currentWinObject
                    });
            if (guiElement != null)
            {
                //SaveElementShotImg(guiElement, paramModel.AFilePath);
                return guiElement;
            }
            return null;
        }
    }
    public class CVResult
    {
        [JsonProperty("elements")]
        public List<List<int>> elementsPoint { get; set; }
        public List<string> ele_ids { get; set; }
        public string action_type { get; set; }
        public string content { get; set; }
        public string message { get; set; }
        public string action_description { get; set; }
        public List<List<int>> absElementsPoint { get; set; }
        public List<string> model_output { get; set; }
    }
}



