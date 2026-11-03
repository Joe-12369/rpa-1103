using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class SetElementSelect_V2 : GuiComponentBase<ParamSetElementSelectModel_V2>
    {
        protected override eComponentType ComponentType => eComponentType.SetElementSelect;
        protected override eErrorMessageType OperationTimeoutMsgType => eErrorMessageType.Error_OperationSelect_OperationTomeout_Error;
        private static ILogger logger = Logger.OfContext();

        protected override void ValidateParameter(ParamSetElementSelectModel_V2 paramModel) {
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
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"SetElementSelect 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        protected override JMessage Execute(ParamSetElementSelectModel_V2 paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();

            GuiElement element;
            try
            {
                logger.LogInformation($"setvalue:{paramModel.setValue}");
                if (paramModel.selectType == 1)
                {
                    var items = JsonConvert.DeserializeObject<List<string>>(paramModel.setValue);
                    if (items != null && items.Count > 1)
                    {
                        string regexPattern = $"[\"({string.Join("|", items)})\"]";
                        logger.LogInformation($"regexPattern:{regexPattern}");
                        paramModel.setValue = regexPattern;
                        paramModel.matchMode = 2;
                    }
                }

                Dictionary<string, object> hashtable = new Dictionary<string, object>{
                        {"processId",paramModel.processId }
                    };
                var param = new GetElementFromSelectorParams
                {
                    Selector = paramModel.Selector,
                    SelectorType = paramModel.SelectorType,
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
                NotifyCoreServerElementFound(paramModel.RunUniqueID);
                element.SetElementSelectWithMatchModel(paramModel.selectType, paramModel.matchMode, paramModel.setValue);
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_SetSelectError, ref msg);
            }
            return msg;

        }
    }
}

