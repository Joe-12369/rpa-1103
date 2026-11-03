using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class GetLineAndBarChartData : GuiComponentBase<ParamGetLineAndBarChartDataModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.GetLineAndBarChartData;

        protected override JMessage Execute(ParamGetLineAndBarChartDataModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            try
            {
                if (!GuiElement.selectorType_Web.Contains(paramModel.WindowInfo.WindowType.ToString(), StringComparer.OrdinalIgnoreCase))
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
                string parentUserDefinedID = "";
                if (!string.IsNullOrEmpty(paramModel.positionSelector))
                {
                    var parentParam = new GetElementFromSelectorParams
                    {
                        Selector = paramModel.positionSelector,
                        SelectorType = paramModel.SelectorType,
                        FindOnce = false,
                        CVExecutionServiceURL = "",
                        ExtendParam = hashtable,
                        MatchType = paramModel.MatchType,
                        RunParam = paramModel.RunParam,
                        WindowInfo = paramModel.WindowInfo
                    };
                    var positionElement = GuiElement.FromSelector(parentParam);
                    if (positionElement == null)
                    {
                        throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                    }
                    parentUserDefinedID = positionElement.UserDefinedID;
                }
                NotifyCoreServerElementFound(paramModel.RunUniqueID);
                var chartData = element.GetLineAndBarChartData(new Commons.Params.ParamGetChartDataModel
                {
                    moveDirection = (int)paramModel.moveDirection,
                    moveSpeed = (int)paramModel.moveSpeed,
                    moveModel = (int)paramModel.moveModel,
                    offset = paramModel.offset,
                    parentElementId = parentUserDefinedID
                });
                msg.Info = chartData;
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_GetAttributeError, ref msg);
            }
            return msg;
        }

        protected override void ValidateParameter(ParamGetLineAndBarChartDataModel paramModel) {
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
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, "GetLineAndBarChartData 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}

