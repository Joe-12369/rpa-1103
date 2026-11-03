using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Params;
using II.RPA.Plugin.GUI.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class GetSelectorByPoint : ElementAbilityBase<GetSelectorByPointModel>
    {
        private ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.None;

        protected override JMessage Execute(GetSelectorByPointModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            try
            {
                GlobleConfigurations.CaptureUniqueID = DateTime.Now.ToLongTimeStamp();
                var mousePoint = new System.Drawing.Point(paramModel.Left, paramModel.Top);
                GuiElement element = ElementProvider.Instance.CaptureElement(mousePoint);
                var nodeSelector = element.Selector;
                this.SaveElementShotImg(element, paramModel.FileFullName);
                if (!string.IsNullOrEmpty(nodeSelector))
                {
                    var common = JsonConvert.DeserializeObject<RPACommon>(nodeSelector);
                    common.CommonProps.filePath = paramModel.FilePath;
                    common.CommonProps.ResDirPath = $@"res\{GlobleConfigurations.CaptureUniqueID}";
                    common.CommonProps.isSmartControl = "false";
                    var selector = common.ToJsonNoneFormating();
                    msg.Info = selector;
                }
                else
                {
                    throw new RPAException(eErrorMessageType.Error_ElementCaptureError);
                }
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_ElementCaptureError, ref msg);
            }
            return msg;
        }

        protected override void ValidateParameter(GetSelectorByPointModel paramModel) {
            try
            {
                return;
            }
            catch
            {
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
        #region 保存元素缩略图
        public void SaveElementShotImg(GuiElement guiElement, string filePath)
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
