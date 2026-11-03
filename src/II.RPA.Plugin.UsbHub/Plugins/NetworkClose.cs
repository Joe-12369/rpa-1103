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
    public class NetworkClose : PluginAbilityBase<ParamNetworkCloseModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.NetworkClose;

        protected override JMessage Execute(ParamNetworkCloseModel paramModel, PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            string output = "";     //输出字符串
            try
            {
                string removeDeviceCmd = string.Format("rfsnu -rd {0}:{1}", paramModel.IpAddress, paramModel.Port);
                string closeChannel = string.Format("rfsnu -d {0}:{1} {2}", paramModel.IpAddress, paramModel.Port, paramModel.Channel);

                msg.Info = "False";
                output = UsbHubHelper.Instance.ExecuteCmd(closeChannel);
                output = UsbHubHelper.Instance.ExecuteCmd(removeDeviceCmd);
                if (output.ToLower().Contains("remove device success"))
                {
                    msg.Info = "True";
                }
                else
                {
                    msg.Code =(int)eErrorMessageType.Error_LocalCloseError;
                    msg.Message = output;
                    logger.LogError(output);
                }
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_LocalCloseError, ref msg);
            }
            return msg;
        }

        protected override void ValidateParameter(ParamNetworkCloseModel paramModel)
        {
            try
            {
                logger.LogDebug("Start NetworkClose");
                if (string.IsNullOrEmpty(paramModel.IpAddress))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError, "IP地址参数不可为空".I18n("controlType_NetworkClose_8AB8AC9C"));
                }
                if (string.IsNullOrEmpty(paramModel.Channel))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError, "通道号参数不可为空".I18n("controlType_NetworkClose_FE6B657D"));
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

