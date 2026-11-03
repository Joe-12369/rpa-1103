using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.Core.UI;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.Core.Entities;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using II.RPA.Plugin.GUI.Utils;
using II.RPA.WebBrowser.Common.Basic;

namespace II.RPA.Plugin.GUI.Plugins.Chatrpa
{
    public class CyclicSimilarElementConfig : GuiComponentBase<ParamCyclicSimilarElementConfigModel>
    {
        private ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.CyclicSimilarElementConfig;
        private readonly UIProvider m_UIProvider;
        private string sourceType = "";
        string filePath = "";
        string similarSelector = "";
        public CyclicSimilarElementConfig()
        {
            m_UIProvider = new UIProvider();
        }

        protected override JMessage Execute(ParamCyclicSimilarElementConfigModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            try
            {
                FacMessageHelper.SendShowOrHiddenMainWndActionToFactory(false);
                GlobleConfigurations.CaptureUniqueID = DateTime.Now.ToLongTimeStamp();
                IntPtr windowHandle = paramModel.WndHandle;
                sourceType = paramModel.sourceType;
                bool userLast = false;
                ZbrainAPIService.NotifyGetWindowImg_ChatRpa(windowHandle, "", ref userLast);
                ShowScanWnd(paramModel.WndHandle);
                bool useNormalGuiType = paramModel.sourceType.EqualIgnoreCase("normal") || paramModel.sourceType.EqualIgnoreCase("webCombineCv");
                string opGuiType = useNormalGuiType ? paramModel.guiType : eGuiType.CV.ToString();
                IGuiOperator_6_9_0 guiOperator = (IGuiOperator_6_9_0)GuiOperatorProvider.Instance.GetGuiOperator(opGuiType, paramModel.WindowInfo);
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
                    ActionName = "cycle_eles"
                });
                //进行两个元素的拾取进行循环相似元素结构构建处理
                AutoCaptureCyclicElement(opGuiType, cvRetElesData);
                msg.Info = similarSelector;

            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_ChatCyclicSimilarElementConfig, ref msg);
            }
            return msg;
        }

        protected override void ValidateParameter(ParamCyclicSimilarElementConfigModel paramModel) {
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
                var m_PictureRandomName = $"{new Random().Next(1, 99999)}-{new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()}.png".Trim().Replace(" ", string.Empty);
                filePath = Path.Combine(GlobleConfigurations.PackageDirectory, "res", m_PictureRandomName);
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
        private void AutoCaptureCyclicElement(string guiType, List<JObject> elesData)
        {
            if (elesData.Count < 1)
            {
                throw new Exception("z-brain未返回有效元素!".I18n("controlType_CyclicSimilarElementConfig_393F749D"));
            }
            var targetEleData = elesData[0];
            if (sourceType.EqualIgnoreCase("normal") ||
               sourceType.EqualIgnoreCase("webCombineCv"))
            {
                similarSelector = GetSelectorByElementId(guiType, targetEleData);
            }
            else
            {
                similarSelector = GetSelectorByCvToNomal(guiType, targetEleData);
            }
            if (!string.IsNullOrEmpty(similarSelector))
            {
                var common = JsonConvert.DeserializeObject<RPACommon>(similarSelector);
                common.CommonProps.filePath = filePath;
                common.CommonProps.ResDirPath = $@"res\{GlobleConfigurations.CaptureUniqueID}";
                similarSelector = common.ToJsonNoneFormating();
            }
        }



        #region 获取selector
        private string GetSelectorByElementId(string guiType, JObject eleData)
        {
            var guiElement = GuiElement.FromElementId(guiType,
                    new GetElementByIdParams
                    {
                        TabId = -1,
                        ElementId = eleData["uniqueId"].ToString()
                    });
            if (guiElement != null)
            {
                SaveElementShotImg(guiElement, filePath);
                return guiElement?.GetSimilarSelectorByAuto();
            }
            return string.Empty;
        }
        private string GetSelectorByCvToNomal(string guiType, JObject eleData)
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
                    var nodeSelector = guiElement?.GetSimilarSelectorByAuto();
                    SaveElementShotImg(guiElement, filePath);
                    return nodeSelector;
                }
            }
            return null;

        }


        private void SaveElementShotImg(GuiElement guiElement, string filePath)
        {
            using (var currentDesktopScreenshot = UiCommon.UIAAutomation.GetDesktop().Capture())
            {
                using (guiElement.CaptureInformativeScreenshot(filePath, currentDesktopScreenshot))
                { }
            }
        }
        #endregion
    }
}
