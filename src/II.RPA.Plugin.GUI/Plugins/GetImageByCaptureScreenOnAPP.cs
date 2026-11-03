using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Plugins
{

    public class GetImageByCaptureScreenOnApp : GuiComponentBase<ParamGetImageByCaptureScreenOnApp>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.GetImageByCaptureScreenOnApp;

        protected override void ValidateParameter(ParamGetImageByCaptureScreenOnApp paramModel) {
            try
            {
                logger.LogDebug($"GetImageByCaptureScreenOnApp 获取Json数据:{paramModel.ToJson()}");
                if (string.IsNullOrEmpty(paramModel.Selector))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"GetImageByCaptureScreenOnApp 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        protected override JMessage Execute(ParamGetImageByCaptureScreenOnApp paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            logger.LogDebug($"GetImageByCaptureScreenOnApp 执行");
            JMessage msg = new JMessage();
            try
            {
                GuiElement element = GuiElement.FromSelector(paramModel.Selector, paramModel.SelectorType, false, null);
                if (element == null)
                {
                    Exception(null, eErrorMessageType.Error_GetImageByCaptureScreenOnApp_Error, ref msg);
                    return msg;
                }
                NotifyCoreServerElementFound(paramModel.RunUniqueID);
                string imageBase64 = element.GetCVOriginImage(null);
                var resultInfo = new JObject
                    {{"image", imageBase64??string.Empty}}.ToString();
                msg.Info = resultInfo;
            }
            catch (Exception ex)
            {
                if (ex is RPAException)
                {
                    var rpaEx = (RPAException)ex;
                    if (rpaEx.ErrorMessageType == eErrorMessageType.Error_NotFoundElement)
                    {
                        rpaEx.ErrorMessageType = eErrorMessageType.Error_GetImageByCaptureScreenOnApp_Error;
                    }
                }
                Exception(ex, eErrorMessageType.Error_GetImageByCaptureScreenOnApp_Error, ref msg);
            }

            return msg;

        }

    }
}

