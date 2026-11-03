using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Params;
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
                GlobleConfigurations.CaptureUniqueID = RandomHelper.RandomString(10);
                UiCommon.ForceShow((IntPtr)paramModel.windowHandle);
                string nodeSelector;
                if (paramModel.sourceType.EqualIgnoreCase("normal"))
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
            return msg;
        }

        protected override void ValidateParameter(ParamGetElementSelectorModel paramModel) {
            if (string.IsNullOrEmpty(paramModel.guiType))
            {
                paramModel.guiType = BrowserWindowProvider.GetFirstBrowserByZOrder()?.WindowType.ToString() ?? string.Empty;
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
                return guiElement?.Selector;
            }
            return string.Empty;
        }
        private string GetSelectorByCvToNomal(ParamGetElementSelectorModel paramModel)
        {
            var elementDataObj = JObject.Parse(paramModel.ElementData);
            var cvRect = elementDataObj["area"].ToObject<RpaRect>();
            var point = cvRect.Center;
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
            var guiOperator = GuiManager.Info.GuiOperators.First(go => go.GuiType.ToString().EqualIgnoreCase(paramModel.guiType));
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
                var intersectArea = Rectangle.Intersect(newArea, cvRect.ToRectangle());
                if (!intersectArea.IsEmpty && intersectArea.Width * intersectArea.Height >= newArea.Width * newArea.Height * Configurations.Info.IntersectRatio)//有60%以上 普通元素重叠区
                {
                    var nodeSelector = guiElement.Selector;
                    SaveElementShotImg(guiElement, paramModel.AFilePath);
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
    }
}
