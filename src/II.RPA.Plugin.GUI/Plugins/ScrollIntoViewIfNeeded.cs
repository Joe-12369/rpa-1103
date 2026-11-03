using II.RPA.Plugin.Core;
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
    public class ScrollIntoViewIfNeeded : GuiComponentBase<ParamScrollIntoViewIfNeededModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.ScrollIntoViewIfNeeded;
        protected override eErrorMessageType OperationTimeoutMsgType => eErrorMessageType.Error_ScrollIntoViewIfNeeded_OperationTomeout_Error;

        protected override void ValidateParameter(ParamScrollIntoViewIfNeededModel paramModel) {
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
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"GetElementCheck 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        protected override JMessage Execute(ParamScrollIntoViewIfNeededModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            try
            {
                if ("chrome,edge,firefox,web360se,ie,sogou,web360x,zexplorer,superbrowser,chromium".Contains(paramModel.SelectorType.ToLower()))
                {
                    Dictionary<string, object> dic = new Dictionary<string, object>();//自定义属性字典
                    dic["ForceMove"] = true;
                    dic["processId"] = paramModel.processId;
                    //var element = GuiElement.FromSelector(selector, selectorType, dic);
                    var param = new GetElementFromSelectorParams
                    {
                        Selector = paramModel.Selector,
                        SelectorType = paramModel.SelectorType,
                        ExtendParam = dic,
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
                    element.IntoView();
                }
                else
                {
                    throw new RPAException(eErrorMessageType.Error_NotFound);
                }
            }
            catch (Exception e)
            {
                Exception(e, eErrorMessageType.Error_NotFoundElement, ref msg);
            }
            return msg;

        }
    }
}

