using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using II.RPA.Plugin.GUI.Params;
using II.RPA.Plugin.GUI.Plugins;
using II.RPA.Plugin.GUI.Utils;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpVectors.Dom;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Interop;

namespace II.RPA.Plugin.GUI.AiRepair
{
    internal class AiRepairUtils
    {
        private static ILogger logger;
        private static string aiRepairDataSavePath;

        public static string AiRepairDataSavePath
        {
            get
            {
                if (string.IsNullOrEmpty(aiRepairDataSavePath))
                {
                    SetAiRepairDataSavePath();
                }
                return aiRepairDataSavePath;
            }
            set => aiRepairDataSavePath = value;
        }
        static AiRepairUtils()
        {
            logger = Logger.OfContext();
        }
        private static void SetAiRepairDataSavePath()
        {
            if (string.IsNullOrEmpty(GlobleConfigurations.PackageDirectory))
            {
                logger.LogInformation("无法设置智能修复数据保存路径，因为包目录信息不可用");
                return;
            }
            string fileName = Path.Combine(GlobleConfigurations.PackageDirectory, ConstFields.PROJECT_INFO_FILE_NAME);
            if (System.IO.File.Exists(fileName))
            {
                string projectInfoJson = System.IO.File.ReadAllText(fileName, Encoding.UTF8);
                JObject projectInfoObj = JObject.Parse(projectInfoJson);
                if (projectInfoObj.TryGetValue("id", out var projectId))
                {
                    aiRepairDataSavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ConstFields.IIRPA_AIREPAIR_DATA_FLODERNAME, projectId.ToString());
                }
            }
            else
            {
#if DEBUG
                aiRepairDataSavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ConstFields.IIRPA_AIREPAIR_DATA_FLODERNAME, "default");
#endif

            }

        }
        #region 新版本 使用CV元素实现智能修复逻辑
        public static string LastRepaireNodePoint { get; private set; }
        public static string LastRepaireStatusCode { get; private set; }
        public static string LastRepaireErrorMsg { get; private set; }
        internal static GuiElement ExecAiRepair_V2(CommonProps commonProps, string runMode = "runtime", string originalElementId = "", bool useFlowGuardLocate = false)
        {
            try
            {
                LastRepaireNodePoint = "";
                LastRepaireStatusCode = "";
                LastRepaireErrorMsg = "";
                logger.LogInformation("Start ExecAiRepair_V2");
                if (string.IsNullOrEmpty(commonProps.ResDirPath))
                {
                    return null;
                }
                AutomationElement sourceAppWndElement = null;
                Rectangle sourceAppWndElement_rect = Rectangle.Empty;
                CaptureImgSourceType sourceType;
                string uid = commonProps.GetRunUniqueID();
                ClearAiRepairData(uid);
                //获取激活窗口元素的截图，并将截图和UID、流程目录等发送给zbrain,获取元素坐标
                IntPtr foregroundWnd = NativeMethods.GetForegroundWindow();
                string foregroundProcessName = UiCommon.GetProcessName(foregroundWnd);
                //判断前置激活窗口是否与selector中所指进程相同（并不能100%避免源窗口非目标窗口，只是减少概率，降低算法耗时概率）
                if (foregroundProcessName.Equals(commonProps.ProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogInformation($"ExecAiRepair - 激活窗口属于预期进程{foregroundProcessName},使用窗口截图");
                    sourceType = CaptureImgSourceType.app;
                    sourceAppWndElement = UiCommon.UIAAutomation.FromHandle(foregroundWnd);
                }
                else
                {
                    logger.LogInformation($"ExecAiRepair - 激活窗口进程{foregroundProcessName}不属于预期进程{commonProps.ProcessName}，使用桌面截图");
                    sourceType = CaptureImgSourceType.desktop;
                    sourceAppWndElement = UiCommon.UIAAutomation.GetDesktop();
                }
                if (sourceAppWndElement == null)
                {
                    logger.LogInformation("ExecAiRepair - 未获取到应用信息");
                    return null;
                }
                if (!sourceAppWndElement.Properties.BoundingRectangle.IsSupported)
                {
                    logger.LogInformation("ExecAiRepair - 未获取到应用的区域信息");
                    return null;
                }
                sourceAppWndElement_rect = sourceAppWndElement.Properties.BoundingRectangle;
                //调用 zbrain 获取修复元素坐标
                var elementSelfRepaireControl = commonProps.isSmartControl.As<bool>();
                var repairPoint = GetAiRepairPoint(sourceAppWndElement, sourceAppWndElement_rect, uid, commonProps.ResDirPath, sourceType, commonProps.Version, commonProps.AiDesc, runMode, useFlowGuardLocate, originalElementId, elementSelfRepaireControl);
                if (repairPoint == Point.Empty)
                {
                    return null;
                }
                //实时拾取元素
                Point aiElePoint = new Point(sourceAppWndElement_rect.X + repairPoint.X, sourceAppWndElement_rect.Y + repairPoint.Y);
                logger.LogDebug($"aiElePoint:{aiElePoint.X},{aiElePoint.Y}");
                var guiType = (eGuiType)Enum.Parse(typeof(eGuiType), commonProps.GUIType, true);
                var hWndList = UiCommon.GetRootWindowList(aiElePoint);
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
                        if (rootElement.Properties.BoundingRectangle.IsSupported && rootElement.BoundingRectangle.Contains(aiElePoint))
                        {
                            break;
                        }
                    }
                    pointElement = UiCommon.UIAAutomation.FromHandle(hWndList.Last());
                }
                //if (guiOperator.IsCanUse(aiElePoint, rootElement, pointElement))
                if (GuiOperatorProvider.Instance.TryGetSupportedOperator(aiElePoint, rootElement, pointElement, guiType, out var guiOperator))
                {
                    guiOperator.Release();
                    var inspectResult = guiOperator.InspectNode(aiElePoint, rootElement, pointElement, "", out var inspectNode);
                    if (inspectResult)
                    {
                        logger.LogDebug($"inspectNodeRect:{inspectNode.GetRegion(3)}");
                        var guiElement = new GuiElement(inspectNode);
                        guiElement.MatchSourceType = ElementMatchType.AiRepair;
                        bool isBeforeVersion16 = RPAUtils.CompareVersionNumber(commonProps.Version, "1.6") < 0;
                        bool isFlowGuardControl = GlobleConfigurations.EnableAiEnhancedScreenshot;
                        bool useFlowGuardControl = isFlowGuardControl && (isBeforeVersion16 || !elementSelfRepaireControl) ? true : false;
                        //新版本的元素智能修复后不存修复数据
                        if (isBeforeVersion16)
                        {
                            SaveAiRepairData(uid, guiElement);
                        }
                        // AI增强定位（version >= 1.6, runtime）：元素巡数据回填前端
                        // 网页元素额外做TARS优化，非网页元素直接上传巡元素原始结构
                        if ((!isBeforeVersion16 || isFlowGuardControl) && runMode == "runtime")
                        {
                            if (GuiElement.IsWebSelectorType(commonProps.GUIType))
                            {
                                OptimizeWebElementSelector(guiElement, commonProps, originalElementId, useFlowGuardControl);
                            }
                            else
                            {
                                SendRetrieveResult(guiElement.Selector, commonProps, originalElementId, useFlowGuardControl);
                            }
                        }
                        return guiElement;
                    }
                }
                else
                {
                    logger.LogInformation($"ExecAiRepair  - 修复失败,GUI类型未匹配,未找到元素");
                }
                logger.LogInformation($"ExecAiRepair  - 修复失败,结束,未找到元素");
                return null;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "智能修复出现异常情况，修复失败，不影响其他任务。");
                return null;
            }
        }

        private static Point GetAiRepairPoint(AutomationElement sourceAppWndElement, Rectangle sourceAppWndElement_rect, string uniqueID, string resDirPath, CaptureImgSourceType sourceType, string eleVersion = "1.6", string aiDesc = "", string runMode = "runtime", bool useFlowGuardLocate = false, string originalElementId = "", bool isEnableSelfRepaire = false)
        {
            string result = string.Empty;
            Dictionary<string, object> wndInfo = new Dictionary<string, object>
                {
                    { "handle", (int)sourceAppWndElement.Properties.NativeWindowHandle.Value },
                    { "area", new[] { sourceAppWndElement_rect.X, sourceAppWndElement_rect.Y, sourceAppWndElement_rect.Width, sourceAppWndElement_rect.Height } }
                };

            using (var appFaceImg = sourceAppWndElement.Capture())
            {
                result = ZbrainAPIService.SendAiRepairToZBrain_V2(uniqueID, resDirPath, appFaceImg, sourceType, wndInfo, eleVersion, aiDesc, runMode, useFlowGuardLocate, isEnableSelfRepaire, originalElementId);
                if (string.IsNullOrEmpty(result))
                {
                    logger.LogInformation("ExecAiRepair - 修复失败");
                    return Point.Empty;
                }
            }
            JObject retObj = JObject.Parse(result);
            LastRepaireStatusCode = retObj.GetValue<string>("code");
            LastRepaireErrorMsg = retObj.GetValue<string>("message");
            if (!retObj.GetValue<string>("status").Equals("success"))
            {
                logger.LogInformation($"ExecAiRepair - 修复失败,返回值:{result}");
                return Point.Empty;
            }
            int x, y;
            if (!retObj.TryGetValueByPath("result.point.x", out x)
                || x <= -9999
                || !retObj.TryGetValueByPath("result.point.y", out y)
                || y <= -9999)
            {
                logger.LogInformation($"ExecAiRepair - 修复失败,未找到元素位置:{result}");
                retObj.TryGetValueByPath("result.point.y", out y);
                LastRepaireNodePoint = $"({x},{y})";
                return Point.Empty;
            }
            LastRepaireNodePoint = $"({x},{y})";
            if (retObj.TryGetValueByPath("wnd_info.area", out int[] wndArea))
            {
                var remoteRefRect = RectHelper.FromAraeArr(wndArea);
                var point = RectHelper.RemotePositionCorrection(sourceAppWndElement_rect, remoteRefRect, x, y);
                logger.LogDebug($"sourceAppWndElement_rect：{sourceAppWndElement_rect}");
                logger.LogDebug($"remoteRefRect：{remoteRefRect}");
                logger.LogDebug($"RemotePosition：{x},{y}");
                logger.LogDebug($"point：{point.X},{point.Y}");
                return point;
            }
            return new Point(x, y);
        }
        private static void SaveAiRepairData(string uid, GuiElement guiElement)
        {
            if (string.IsNullOrEmpty(AiRepairDataSavePath))
            {
                return;
            }
            string selector = guiElement.Selector;

            RPACommon rpaCommon = selector.ToObject<RPACommon>();
            JObject dataObj = new JObject()
            {
                { "airepairTime",DateTime.Now.ToLongTimeStamp()},
                { "selector",rpaCommon.Selector}
            };
            if (!Directory.Exists(AiRepairDataSavePath))
            {
                Directory.CreateDirectory(AiRepairDataSavePath);
            }
            string fileFullName = Path.Combine(AiRepairDataSavePath, $"{uid}.json");
            using (StreamWriter streamWriter = new StreamWriter(fileFullName, append: false, encoding: Encoding.UTF8))
            {
                streamWriter.Write(dataObj.ToString());
            }
        }
        public static JArray GetAiRepairSelector(string uid, out long airepairTime)
        {
            airepairTime = 0;
            if (string.IsNullOrEmpty(AiRepairDataSavePath) || string.IsNullOrEmpty(uid))
            {
                return null;
            }
            string fileFullName = Path.Combine(AiRepairDataSavePath, $"{uid}.json");
            if (!File.Exists(fileFullName))
            {
                return null;
            }
            logger.LogDebug($"存在修复缓存数据：fileFullName");
            string text = File.ReadAllText(fileFullName, Encoding.UTF8);
            JObject jObj = JObject.Parse(text);
            long.TryParse(jObj["airepairTime"].ToString(), out airepairTime);
            return (JArray)jObj["selector"];
        }
        public static void ClearAiRepairData(string uid)
        {
            try
            {
                if (string.IsNullOrEmpty(AiRepairDataSavePath) || string.IsNullOrEmpty(uid))
                {
                    return;
                }
                string fileFullName = Path.Combine(AiRepairDataSavePath, $"{uid}.json");
                if (File.Exists(fileFullName))
                {
                    logger.LogDebug($"智能修复缓存数据将被清除：{fileFullName}");
                    File.Delete(fileFullName);
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug($"删除智能修复产生的数据时，发生了异常，{ex}");
            }
        }
        #endregion

        #region ExecAiRepair_V1 老本的智能修复逻辑，6.7.2(6.8.0)后禁用,不要修改其内任何逻辑

        [Obsolete("老本的智能修复逻辑，6.7.2(6.8.0)后禁用,如非必要，不要修改其内任何逻辑，仅留作参考")]
        internal static GuiElement ExecAiRepair_V1(CommonProps commonProps)
        {
            try
            {
                logger.LogInformation("Start ExecAiRepair");
                if (string.IsNullOrEmpty(commonProps.ResDirPath))
                {
                    return null;
                }
                AutomationElement sourceAppWndElement = null;
                Rectangle sourceAppWndElement_rect = Rectangle.Empty;
                CaptureImgSourceType sourceType;
                string uid = commonProps.ResDirPath.Substring(commonProps.ResDirPath.LastIndexOf('\\') + 1);
                //获取激活窗口元素的截图，并将截图和UID、流程目录等发送给zbrain,获取元素坐标
                IntPtr foregroundWnd = NativeMethods.GetForegroundWindow();
                string foregroundProcessName = UiCommon.GetProcessName(foregroundWnd);
                //判断前置激活窗口是否与selector中所指进程相同（并不能100%避免源窗口非目标窗口，只是减少概率，降低算法耗时概率）
                if (foregroundProcessName.Equals(commonProps.ProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogInformation("ExecAiRepair - 激活窗口属于预期进程");
                    sourceType = CaptureImgSourceType.app;
                    sourceAppWndElement = UiCommon.UIAAutomation.FromHandle(foregroundWnd);
                }
                else
                {
                    logger.LogInformation("ExecAiRepair - 激活窗口不属于预期进");
                    sourceType = CaptureImgSourceType.desktop;
                    sourceAppWndElement = UiCommon.UIAAutomation.GetDesktop();
                }
                if (sourceAppWndElement == null)
                {
                    logger.LogInformation("ExecAiRepair - 未获取到应用信息");
                    return null;
                }
                if (!sourceAppWndElement.Properties.BoundingRectangle.IsSupported)
                {
                    logger.LogInformation("ExecAiRepair - 未获取到应用的区域信息");
                    return null;
                }
                sourceAppWndElement_rect = sourceAppWndElement.Properties.BoundingRectangle;
                //调用 zbrain 获取修复元素坐标
                string result = string.Empty;
                using (var appFaceImg = sourceAppWndElement.Capture())
                {
                    result = ZbrainAPIService.SendAiRepairToZBrain(uid, commonProps.ResDirPath, appFaceImg, sourceType);
                    if (string.IsNullOrEmpty(result))
                    {
                        logger.LogInformation("ExecAiRepair - 修复失败");
                        return null;
                    }
                }
                JObject retObj = JObject.Parse(result);
                if (retObj.TryGetValueByPath("status", out string status))
                {
                    if (!status.Equals("success"))
                    {
                        logger.LogInformation($"ExecAiRepair - 修复失败,返回值:{result}");
                        return null;
                    }
                }
                if (!retObj.TryGetValueByPath("result.point.x", out int x)
                    || x <= -9999
                    || !retObj.TryGetValueByPath("result.point.y", out int y)
                    || y <= -9999)
                {
                    logger.LogInformation($"ExecAiRepair - 修复失败,未找到元素位置:{result}");
                    return null;
                }
                //实时拾取元素
                Point aiElePoint = new Point(sourceAppWndElement_rect.X + x, sourceAppWndElement_rect.Y + y);
                var guiType = (eGuiType)Enum.Parse(typeof(eGuiType), commonProps.GUIType);
                var hWndList = UiCommon.GetRootWindowList(aiElePoint);
                AutomationElement rootElement = null;
                AutomationElement pointElement = null;
                if (hWndList.Count > 0)
                {
                    //rootElement = UiCommon.UIAAutomation.FromHandle(hWndList.First());
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
                        if (rootElement.Properties.BoundingRectangle.IsSupported && rootElement.BoundingRectangle.Contains(aiElePoint))
                        {
                            break;
                        }
                    }
                    pointElement = UiCommon.UIAAutomation.FromHandle(hWndList.Last());
                }
                if (GuiOperatorProvider.Instance.TryGetSupportedOperator(aiElePoint, rootElement, pointElement, guiType, out var guiOperator))
                {
                    var inspectResult = guiOperator.InspectNode(aiElePoint, rootElement, pointElement, "", out var inspectNode);
                    if (inspectResult)
                    {
                        return new GuiElement(inspectNode);
                    }
                }
                else
                {
                    logger.LogInformation($"ExecAiRepair  - 修复失败,GUI类型未匹配,未找到元素");
                }
                logger.LogInformation($"ExecAiRepair  - 修复失败,结束,未找到元素");
                return null;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "智能修复出现异常情况，修复失败，不影响其他任务。");
                return null;
            }
        }

        #endregion

        #region MyRegion
        public static JObject ReadStaticData(string resDirPath)
        {
            string text = UiCommon.ReadStaticSelectorData(resDirPath, "intellectData.json");
            JObject jObj = JObject.Parse(text);
            return jObj;
        }
        public static T ReadStaticData<T>(string resDirPath, string propName)
        {
            JObject jObj = ReadStaticData(resDirPath);
            return jObj.GetValue<T>(propName);
        }
        #endregion

        #region
        /// <summary>
        /// 上传巡元素结构到前端（不含TARS优化），非网页元素的AI增强定位结果直接走此方法。
        /// </summary>
        private static void SendRetrieveResult(string selector, CommonProps commonProps, string originalElementId = "", bool isUseFlowGuardRepair = false)
        {
            try
            {
                // 将 selector 结构中的 elementDetail.elementId 替换为原始结构的 ElementId
                string normalizedSelector = selector;
                if (!string.IsNullOrEmpty(originalElementId))
                {
                    try
                    {
                        var rpaCommon = selector.ToObject<RPACommon>();
                        if (rpaCommon?.ElementDetail != null)
                        {
                            rpaCommon.ElementDetail.ElementId = originalElementId;
                            rpaCommon.CommonProps.ResDirPath = commonProps.ResDirPath;
                            rpaCommon.CommonProps.filePath = commonProps.filePath;
                            rpaCommon.CommonProps.isSmartControl = commonProps.isSmartControl;
                            rpaCommon.CommonProps.AiDesc = commonProps.AiDesc;
                            normalizedSelector = rpaCommon.ToJsonNoneFormating();
                        }
                    }
                    catch { /* 反序列化失败则使用原始 selector */ }
                }
                var retrieveResult = new ParamAiEnhancedModel
                {
                    ElementId = originalElementId,
                    RetrieveSelector = normalizedSelector,
                    RetrieveTime = DateTime.Now.ToLongTimeStamp(),
                    Version = commonProps.Version,
                    RepairSource = isUseFlowGuardRepair ? "FLOW_GUARD" : "AI_REPAIR"
                };
                #region 根据客户端类型回传元素增强定位后的数据
                if (GlobleConfigurations.ClientType == ClientType.Factory)
                {
                    logger.LogDebug("回填AI增强数据至设计器");
                    FacMessageHelper.SendRetrieveElementResultToFactory(retrieveResult);
                }
                else if (GlobleConfigurations.ClientType == ClientType.Bot)
                {
                    logger.LogDebug("回填AI增强数据至机器人");
                    var ret = MessageService.SendMessage(TerminalType.Bot, "Element.aiEnhancedLocator", retrieveResult.ToJsonNoneFormating());
                    if (ret != null)
                    {
                        logger.LogDebug($"SendMessageToBot done");
                    }
                    else
                    {
                        logger.LogError($"SendMessageToBot fail");
                    }
                }
                #endregion
                logger.LogInformation("AI增强定位结果上传完成, uid={Uid}", commonProps.GetRunUniqueID());
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "AI增强定位结果上传失败");
            }
        }

        /// <summary>
        /// 对网页元素进行TARS节点优化（含XPath生成）+ 上传给前端，仅 >= 1.6 版本 runtime 场景触发。
        /// TARS优化成功则上传优化后的结构，失败则兜底上传原始结构。
        /// </summary>
        private static void OptimizeWebElementSelector(GuiElement guiElement, CommonProps commonProps, string originalElementId = "", bool isUseFlowGuardRepair = false)
        {
            try
            {
                string startSelector = string.Empty;
                ParamSmartSelectorModel paramModel = new ParamSmartSelectorModel()
                {
                    Selector = guiElement.Selector,
                    Timeout = 30000,
                    ExecuteMode = "passive"
                };
                JMessage msg = new JMessage();
                string jsonParam = paramModel.ToJson();
                var smartSuc = RPAUtils.Retry(() =>
                {
                    string result = GuiManager.PluginInvoker
                        .InvokeAsync(new II.RPA.Plugin.Abstractions.PluginRoute("Element", "SmartSelector"), jsonParam)
                        .GetAwaiter()
                        .GetResult();
                    if (string.IsNullOrEmpty(result))
                    {
                        throw new RPAException(eErrorMessageType.Error_DataCollection_NextBtnNotFound);
                    }
                    msg = result.ToObject<JMessage>();
                    return msg.Code == 0;
                }, 2000, 100);
                if (smartSuc)
                {
                    startSelector = msg.Info;
                    // 上传优化后的结构
                    SendRetrieveResult(startSelector, commonProps, originalElementId, isUseFlowGuardRepair);
                    return;
                }
                else
                {
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "TARS优化失败，回退上传原始结构");
            }
            // 兜底：上传未优化的原始结构
            SendRetrieveResult(guiElement.Selector, commonProps, originalElementId, isUseFlowGuardRepair);
        }

        #endregion
    }
}

