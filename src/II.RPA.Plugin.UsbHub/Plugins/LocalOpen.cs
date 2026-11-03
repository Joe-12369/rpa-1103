using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.UsbHub.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.UsbHub.Plugins
{
    public class LocalOpen : PluginAbilityBase<ParamLocalOpenModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.LocalOpen;

        protected override JMessage Execute(ParamLocalOpenModel paramModel, PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                var coms = UsbHubHelper.Instance.OpenUsb(paramModel.PortName, paramModel.Channel);
                string result = JsonHelper.JsonByNewtonJson(coms);
                msg.Info = result;
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_LocalOpenError, ref msg);
            }
            return msg;
        }

        protected override void ValidateParameter(ParamLocalOpenModel paramModel)
        {
            try
            {
                logger.LogDebug("Start LocalOpen");
                if (string.IsNullOrEmpty(paramModel.PortName))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError, "参数不可为空".I18n("controlType_LocalOpen_4D3305DB"));
                }
                if (string.IsNullOrEmpty(paramModel.Channel))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError, "通道号参数不可为空".I18n("controlType_LocalOpen_FE6B657D"));
                }
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}

