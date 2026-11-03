using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.GUI.Commons;
using System.Collections;


namespace II.RPA.Plugin.GUI.Plugins
{
    public class GetCVPriceAmount : GuiComponentBase<ParamGetCVTextModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.GetCVPriceAmount;

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
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, "GetCVPriceAmount 参数格式不符合要求：{paramModel.ToJson()}");
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
                    Exception(null, eErrorMessageType.Error_GetCVPriceAmount_Error, ref msg);
                    return msg;
                }
                NotifyCoreServerElementFound(paramModel.RunUniqueID);
                string result = element.GetCVOriginImage(paramModel.cvserver);
                msg.Info = result;
            }
            catch (Exception ex)
            {
                if (ex is RPAException)
                {
                    var rpaEx = (RPAException)ex;
                    if (rpaEx.ErrorMessageType == eErrorMessageType.Error_NotFoundElement)
                    {
                        rpaEx.ErrorMessageType = eErrorMessageType.Error_GetCVPriceAmount_Error;
                    }
                }
                Exception(ex, eErrorMessageType.Error_GetCVPriceAmount_Error, ref msg);
            }

            return msg;

        }

    }

    /// <summary>
    /// 修正写法
    /// </summary>
    #region 修正写法
    /*
    public class GetCVPriceAmount : ComponentBase<ParamGetCVPriceAmount>
    {
        protected override eComponentType ComponentType => eComponentType.GetCVPriceAmount;

        protected override void ValidateParameter(ParamGetCVPriceAmount paramModel) {
            try
            {
                if (string.IsNullOrEmpty(paramModel.selector))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                return;
            }
            catch (Exception ex)
            {
                logger.LogInformation($"GetCVPriceAmount 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        protected override JMessage Execute(ParamGetCVPriceAmount paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            logger.LogInformation("重写金额 组件 执行");
            JMessage msg = new JMessage();
            string selectorType;
            string selector = paramModel.selector;
            try
            {
                selector = this.GetSelectorInfo(selector, paramModel.BrowserType, out selectorType, out string version);
                GuiElement element = GuiElement.FromSelector(selector, selectorType, false, null);

                if (element == null)
                {
                    Exception(null, eErrorMessageType.Error_GetCVPriceAmount_Error, ref msg);
                    return msg;
                }
                string result = element.GetCVOriginImage(null);
                msg.Info = result;
            }
            catch (Exception ex)
            {
                if (ex is RPAException)
                {
                    var rpaEx = (RPAException)ex;
                    if (rpaEx.ErrorMessageType == eErrorMessageType.Error_NotFoundElement)
                    {
                        rpaEx.ErrorMessageType = eErrorMessageType.Error_GetCVPriceAmount_Error;
                    }
                }
                Exception(ex, eErrorMessageType.Error_GetCVPriceAmount_Error, ref msg);
            }

            return msg;

        }

    }
    */
    #endregion


}

