using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System.Collections;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class MouseHover : GuiComponentBase<ParamMouseHoverModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.MouseHover;
        protected override eErrorMessageType OperationTimeoutMsgType => eErrorMessageType.Error_MouseHover_OperationTomeout_Error;

        protected override void ValidateParameter(ParamMouseHoverModel paramModel) {
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

        protected override JMessage Execute(ParamMouseHoverModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            GuiElement element = null;
            try
            {
                Dictionary<string, object> dic = new Dictionary<string, object>();//自定义属性字典
                dic["Move"] = true;
                dic["processId"] = paramModel.processId;
                paramModel.cvExecutionServiceURL = GlobleConfigurations.ZBrainServicePath + CVRouteConstFields.CV_EXECUTION_SERVICE;
                //element = GuiElement.FromSelector(selector, selectorType, false, paramModel.cvExecutionServiceURL, dic);
                var param = new GetElementFromSelectorParams
                {
                    Selector = paramModel.Selector,
                    SelectorType = paramModel.SelectorType,
                    FindOnce = false,
                    CVExecutionServiceURL = paramModel.cvExecutionServiceURL,
                    ExtendParam = dic,
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
                UiElementHoverParams hp = new UiElementHoverParams();
                hp.offsetX = paramModel.offsetX;
                hp.offsetY = paramModel.offsetY;
                hp.cursorPos = paramModel.cursorPos;
                hp.isUsePerson = paramModel.isUsePerson == 1;
                element.MouseHover(hp);
            }
            catch (Exception e)
            {
                Exception(e, eErrorMessageType.Error_MoveElementError, ref msg);
                return msg;
            }
            return msg;


        }
    }
}

