using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Params;
using II.RPA.Plugin.GUI.Utils;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpVectors.Dom;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class GetElementSelector : GuiComponentBase<ParamGetElementSelectorModel>
    {
        private ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.GetElementSelector;

        protected override JMessage Execute(ParamGetElementSelectorModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            try
            {
                GlobleConfigurations.CaptureUniqueID = DateTime.Now.ToLongTimeStamp();
                FacMessageHelper.SendShowOrHiddenMainWndActionToFactory(false);
                UiCommon.ForceShow((IntPtr)paramModel.windowHandle);
                string nodeSelector;
                if (paramModel.sourceType.EqualIgnoreCase("normal") ||
                    paramModel.sourceType.EqualIgnoreCase("webCombineCv"))
                {
                    nodeSelector = this.GetSelectorByElementId(paramModel);
                }

                else
                {
                    nodeSelector = this.GetSelectorByCvToNomal(paramModel);
                }
                if (!string.IsNullOrEmpty(nodeSelector))
                {
                    var common = JsonConvert.DeserializeObject<RPACommon>(nodeSelector);
                    common.CommonProps.filePath = paramModel.filePath;
                    common.CommonProps.ResDirPath = $@"res\{GlobleConfigurations.CaptureUniqueID}";
                    common.CommonProps.isSmartControl = "false";
                    msg.Info = common.ToJsonNoneFormating();
                }
            }
            catch (Exception e)
            {
                Exception(e, eErrorMessageType.Error_GetElementSelectorError, ref msg);
            }
            finally
            {
                FacMessageHelper.SendShowOrHiddenMainWndActionToFactory(true);
            }
            return msg;
        }

        protected override void ValidateParameter(ParamGetElementSelectorModel paramModel) {
            if (string.IsNullOrEmpty(paramModel.guiType) ||
                (GlobleConfigurations.UseSZBrowser == true &&
                (paramModel.sourceType.EqualIgnoreCase("normal") || paramModel.sourceType.EqualIgnoreCase("webCombineCv"))))
            {
                var windowInfo = BrowserWindowProvider.GetFirstBrowserByZOrder();
                paramModel.guiType = windowInfo.WindowType.ToString();
                paramModel.WindowInfo = windowInfo;
            }
            if (string.IsNullOrEmpty(paramModel.filePath))
            {
                var m_PictureRandomName = $"{new Random().Next(1, 99999)}-{new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()}.png".Trim().Replace(" ", string.Empty);
                paramModel.filePath = Path.Combine("res", m_PictureRandomName);
            }
            return;
        }

        #region 获取selector
        private string GetSelectorByElementId(ParamGetElementSelectorModel paramModel)
        {
            var guiElement = GuiElement.FromElementId(paramModel.guiType,
                    new GetElementByIdParams
                    {
                        TabId = -1,
                        ElementId = paramModel.ElementId,
                    });
            if (guiElement != null)
            {
                SaveElementShotImg(guiElement, paramModel.AFilePath);
                //为ai智能修复保存数据
                SaveElementAiRepairData(guiElement);
                return guiElement?.Selector;

            }
            return string.Empty;
        }
        private string GetSelectorByCvToNomal(ParamGetElementSelectorModel paramModel)
        {
            var elementDataObj = JObject.Parse(paramModel.ElementData);
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
            var guiOperator = (IGuiOperator_6_9_0)GuiOperatorProvider.Instance.GetGuiOperator(paramModel.guiType, paramModel.WindowInfo);
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
                    var nodeSelector = guiElement.Selector;
                    SaveElementShotImg(guiElement, paramModel.AFilePath);

                    //为ai智能修复保存数据
                    SaveElementAiRepairData(guiElement);
                    return nodeSelector;
                }
            }
            return getCvElementSelector(paramModel);

        }

        private string getCvElementSelector(ParamGetElementSelectorModel paramModel)
        {
            var elementDataObj = JObject.Parse(paramModel.ElementData);
            var uniqueId = elementDataObj["uniqueId"].ToString();

            var guiElement = GuiElement.FromElementId(eGuiType.CV.ToString(),
                   new GetElementByIdParams
                   {
                       ElementId = uniqueId,
                   });
            if (guiElement != null)
            {
                ((IGuiCVNode)guiElement.uiNode).CVSubElementToBitmap(paramModel.AFilePath, null);
                return guiElement?.Selector;
            }
            return string.Empty;
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


        #region 保存元素智能修复数据
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
        #endregion
    }
}
