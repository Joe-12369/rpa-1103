using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class GetPieChartData : GuiComponentBase<ParamGetPieChartDataModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.GetPieChartData;

        protected override JMessage Execute(ParamGetPieChartDataModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
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
                NotifyCoreServerElementFound(paramModel.RunUniqueID);
                var chartData = element.GetPieChartData(new Commons.Params.ParamGetChartDataModel
                {
                    radiusRatio = paramModel.radiusRatio,
                    horOffset = paramModel.horOffset,
                    verOffset = paramModel.verOffset,
                    moveSpeed = (int)paramModel.moveSpeed
                });
                msg.Info = chartData;
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_GetAttributeError, ref msg);
            }
            return msg;
        }

        protected override void ValidateParameter(ParamGetPieChartDataModel paramModel) {
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
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, "GetPieChartData 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}

