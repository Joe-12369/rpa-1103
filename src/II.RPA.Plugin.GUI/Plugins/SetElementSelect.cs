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
    public class SetElementSelect : GuiComponentBase<ParamSetElementSelectModel>
    {
        protected override eComponentType ComponentType => eComponentType.SetElementSelect;
        protected override eErrorMessageType OperationTimeoutMsgType => eErrorMessageType.Error_OperationSelect_OperationTomeout_Error;
        private static ILogger logger = Logger.OfContext();

        protected override void ValidateParameter(ParamSetElementSelectModel paramModel) {
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

        protected override JMessage Execute(ParamSetElementSelectModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();

            GuiElement element = null;
            try
            {
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
                //element.SetElementSelect(paramModel.selectType, paramModel.setValue);
                if (!paramModel.isMultiple)
                    element.SetElementSelect(paramModel.selectType, paramModel.setValue);
                else
                    element.SetElementMultipleSelect(paramModel.selectType, paramModel.setValue);
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_SetSelectError, ref msg);
            }
            return msg;

        }
    }
}

