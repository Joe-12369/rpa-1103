using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class FindRelativeElementsInfo : GuiComponentBase<ParamFindRelativeElementsInfoModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override RPA.WinAutomation.CommonUtility.eComponentType ComponentType => eComponentType.FindRelativeElementsInfo;
        protected override JMessage Execute(ParamFindRelativeElementsInfoModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            try
            {
                Dictionary<string, object> dic = new Dictionary<string, object>();//自定义属性字典
                dic["Relative"] = true;
                var element = GuiElement.FromSelector(paramModel.Selector, paramModel.SelectorType, extendParam: dic);

                if (element == null)
                {
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                }
                NotifyCoreServerElementFound(paramModel.RunUniqueID);
                JArray context = new JArray();
                var result = element.FindRelativeElementsInfo(paramModel.strSelector, ref context);
                if (context.Count != 0)
                {
                    msg.Info = context.ToString();
                }
                else
                {
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                }
            }
            catch (RPAException ex)
            {
                Exception(ex, ref msg);
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_OperationFail, ref msg);
            }
            return msg;
        }

        protected override void ValidateParameter(ParamFindRelativeElementsInfoModel paramModel) {
            try
            {
                if (string.IsNullOrEmpty(paramModel.Selector))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                if (string.IsNullOrEmpty(paramModel.strSelector))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, "数据抓取 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}

