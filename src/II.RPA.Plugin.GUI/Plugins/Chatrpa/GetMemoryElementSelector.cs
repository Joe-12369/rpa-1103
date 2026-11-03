using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Params;
using II.RPA.Plugin.GUI.Utils;
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
    public class GetMemoryElementSelector : GuiComponentBase<ParamGetMemoryElementSelectorModel>
    {
        private ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.GetElementSelector;

        protected override JMessage Execute(ParamGetMemoryElementSelectorModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            try
            {
                GlobleConfigurations.CaptureUniqueID = DateTime.Now.ToLongTimeStamp();
                UiCommon.ForceShow(paramModel.WindowInfo.WndHandle);
                string nodeSelector;

                if (!ZbrainAPIService.QueryMemoryElementArea(paramModel.WindowInfo, paramModel.MemoryInfo, out var area, out var remoteWndArea))
                {
                    logger.LogDebug($"未匹配到记忆元素");
                    msg.Info = string.Empty;
                    return msg;
                }
                NativeMethods.GetWindowRect(paramModel.WindowInfo.WndHandle, out var _loclWndRect);
                var w = _loclWndRect.Right - _loclWndRect.Left;
                var h = _loclWndRect.Bottom - _loclWndRect.Top;
                var localWndArea = new Rectangle(_loclWndRect.Left, _loclWndRect.Top, w, h);
                area.X += remoteWndArea.X;
                area.Y += remoteWndArea.Y;
                var correctionArea = RectHelper.RemoteRectCorrection(localWndArea, remoteWndArea, area);

                nodeSelector = this.GetSelectorByCvToNomal(paramModel, correctionArea);
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

        protected override void ValidateParameter(ParamGetMemoryElementSelectorModel paramModel) {
            if (paramModel.WindowInfo == null)
            {
                logger.LogInformation($"缺少WindowInfo参数");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
            if (string.IsNullOrEmpty(paramModel.filePath))
            {
                var m_PictureRandomName = $"{new Random().Next(1, 99999)}-{new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()}.png".Trim().Replace(" ", string.Empty);
                paramModel.filePath = Path.Combine("res", m_PictureRandomName);
            }
            return;
        }

        #region 获取selector
        private string GetSelectorByCvToNomal(ParamGetMemoryElementSelectorModel paramModel, Rectangle area)
        {
            var point = area.Center();
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
            var guiOperator = (IGuiOperator_6_9_0)GuiOperatorProvider.Instance.GetGuiOperator(paramModel.guiType);
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
                logger.LogDebug($"new ele area:{newArea.ToString()} ");

                var nodeSelector = guiElement.Selector;
                SaveElementShotImg(guiElement, paramModel.AFilePath);
                return nodeSelector;
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
