using II.RPA.Plugin.Core;
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
    public class GetAttribute : GuiComponentBase<ParamGetAttributeModel>
    {
        protected override eComponentType ComponentType => eComponentType.GetAttribute;
        protected override eErrorMessageType OperationTimeoutMsgType => eErrorMessageType.Error_GetAttribute_OperationTomeout_Error;
        private static ILogger logger = Logger.OfContext();

        protected override void ValidateParameter(ParamGetAttributeModel paramModel) {
            try
            {
                if (string.IsNullOrEmpty(paramModel.Selector))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                if (string.IsNullOrEmpty(paramModel.name))
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

        protected override JMessage Execute(ParamGetAttributeModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            try
            {
                if (!GuiElement.selectorType_Web.Contains(paramModel.WindowInfo.WindowType.ToString(), StringComparer.OrdinalIgnoreCase)
                    && !GuiElement.selectorType_Win.Contains(paramModel.SelectorType.ToLower()))//windows 支持
                {
                    throw new RPAException(eErrorMessageType.Error_ElementNotOperation);
                }
                Dictionary<string, object> hashtable = new Dictionary<string, object>{
                        {"processId",paramModel.processId }
                    };
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
                var element = GuiElement.FromSelector(param);
                if (element == null)
                {
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                }
                NotifyCoreServerElementFound(paramModel.RunUniqueID);
                var attrVal = element.GetAttribute(paramModel.name, paramModel.apiVersion);
                //paramModel.apiVersion ==v2 之前的版本(无) ，空值属性 均返回"",下边是兼容性代码
                if (string.IsNullOrEmpty(attrVal) && string.IsNullOrEmpty(paramModel.apiVersion)) {
                    attrVal = "";
                }
                msg.Info = attrVal;
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_GetAttributeError, ref msg);
            }
            return msg;

        }
    }
}

