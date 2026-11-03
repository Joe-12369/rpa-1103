using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.GUI.Commons;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class GetCVText : GuiComponentBase<ParamGetCVTextModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.GetCVText;

        protected override void ValidateParameter(ParamGetCVTextModel paramModel) {
            try
            {
                if (string.IsNullOrEmpty(paramModel.Selector))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                if (string.IsNullOrEmpty(paramModel.cvserver))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, "GetCVText 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        protected override JMessage Execute(ParamGetCVTextModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            try
            {
                paramModel.cvExecutionServiceURL = GlobleConfigurations.ZBrainServicePath + CVRouteConstFields.CV_EXECUTION_SERVICE;
                var param = new GetElementFromSelectorParams
                {
                    Selector = paramModel.Selector,
                    SelectorType = paramModel.SelectorType,
                    FindOnce = false,
                    CVExecutionServiceURL = paramModel.cvExecutionServiceURL,
                    ExtendParam = null,
                    MatchType = paramModel.MatchType,
                    RunParam = paramModel.RunParam,
                    WindowInfo = paramModel.WindowInfo
                };
                var element = GuiElement.FromSelector(param);
                if (element == null)
                {
                    Exception(null, eErrorMessageType.Error_GetCVText_Error, ref msg);
                    return msg;
                }
                NotifyCoreServerElementFound(paramModel.RunUniqueID);
                paramModel.cvserver = GlobleConfigurations.ZBrainServicePath + CVRouteConstFields.GET_TEXT_CONTENT_SERVICE;
                string result = element.GetTextByCV(paramModel.cvserver);
                msg.Info = result;
            }
            catch (Exception ex)
            {
                if (ex is RPAException)
                {
                    var rpaEx = (RPAException)ex;
                    if (rpaEx.ErrorMessageType == eErrorMessageType.Error_NotFoundElement)
                    {
                        rpaEx.ErrorMessageType = eErrorMessageType.Error_GetCVText_Error;
                    }
                }
                Exception(ex, eErrorMessageType.Error_GetCVText_Error, ref msg);
            }

            return msg;

        }

    }
}

