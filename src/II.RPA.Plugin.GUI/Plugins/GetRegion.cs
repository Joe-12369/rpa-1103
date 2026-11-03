using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.GUI.Commons;
using System.Collections;


namespace II.RPA.Plugin.GUI.Plugins
{
    public class GetRegion : GuiComponentBase<ParamGetRegionModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.GetRegion;
        protected override eErrorMessageType OperationTimeoutMsgType => eErrorMessageType.Error_GetRegion_OperationTomeout_Error;

        protected override void ValidateParameter(ParamGetRegionModel paramModel) {
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
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"SetAttribute 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        protected override JMessage Execute(ParamGetRegionModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            string selectorType;
            GuiElement element = null;
            try
            {
                Dictionary<string, object> dic = new Dictionary<string, object>();//自定义属性字典
                dic["Move"] = false;
                dic["processId"] = paramModel.processId;
                paramModel.cvExecutionServiceURL = GlobleConfigurations.ZBrainServicePath + CVRouteConstFields.CV_EXECUTION_SERVICE;
                var param = new GetElementFromSelectorParams
                {
                    Selector = paramModel.Selector,
                    SelectorType = paramModel.SelectorType,
                    FindOnce = false,
                    CVExecutionServiceURL = paramModel.cvExecutionServiceURL,
                    ExtendParam = dic,
                    MatchType = paramModel.MatchType,
                    RunParam = paramModel.RunParam,
                    isMove = false,
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
                var result = element.GetRegion(paramModel.positionType);
                Rect rc = new Rect();
                rc.x = result.X.ToString();
                rc.y = result.Y.ToString();
                rc.width = result.Width.ToString();
                rc.height = result.Height.ToString();
                msg.Info = JsonHelper.JsonByNewtonJson(rc);
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_GetRegionError, ref msg);
            }
            return msg;

        }
    }
}

