using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.GUI.Commons;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class GetButtonEnable : GuiComponentBase<ParamGetButtonEnableModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.ElementEnable;
        protected override eErrorMessageType OperationTimeoutMsgType => eErrorMessageType.Error_ElementEnable_OperationTomeout_Error;


        protected override void ValidateParameter(ParamGetButtonEnableModel paramModel) {
            try
            {
                if (string.IsNullOrEmpty(paramModel.Selector))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, "SetAttribute 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        protected override JMessage Execute(ParamGetButtonEnableModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            GuiElement element = null;
            try
            {
                Dictionary<string, object> hashtable = new Dictionary<string, object>{
                        {"processId",paramModel.processId }
                    };
                //element = GuiElement.FromSelector(selector, selectorType, extendParam: hashtable);
                var param = new GetElementFromSelectorParams
                {
                    Selector = paramModel.Selector,
                    SelectorType = paramModel.SelectorType,
                    FindOnce = false,
                    CVExecutionServiceURL = "",
                    ExtendParam = hashtable,
                    MatchType = paramModel.MatchType,
                    RunParam = paramModel.RunParam,
                    WindowInfo = paramModel.WindowInfo
                };
                element = GuiElement.FromSelector(param);
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_NotFoundElement, ref msg);
                return msg;
            }
            try
            {
                if (element == null)
                {
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                }

                if (!element.IsShow() ||
                       (element.BoundingRectangle.Width <= 0 && element.BoundingRectangle.Height <= 0))
                {
                    if (!paramModel.IsDiscernHidden)
                    {
                        throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                    }
                    else
                    {
                        msg.Code = 0;
                        msg.UnifyCode = "0";
                        msg.Info = "0";
                        return msg;
                    }
                }

                NotifyCoreServerElementFound(paramModel.RunUniqueID);
                var result = element.GetEnableClick().ToLower();
                msg.Info = result == "true" ? "1" : "0";
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_ElementIsEnable, ref msg);
            }
            return msg;
        }

    }
}


