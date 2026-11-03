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
using System.Threading;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.UsbHub.Plugins
{
    public class NetworkOpen : PluginAbilityBase<ParamNetworkOpenModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.NetworkOpen;

        protected override JMessage Execute(ParamNetworkOpenModel paramModel, PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            string output = "";     //输出字符串
            try
            {
                string removeDeviceCmd = string.Format("rfsnu -rd {0}:{1}", paramModel.IpAddress, paramModel.Port);
                string addDeviceCmd = string.Format("rfsnu -ad {0}:{1}", paramModel.IpAddress, paramModel.Port);
                string openChannel = string.Format("rfsnu -c {0}:{1} {2}", paramModel.IpAddress, paramModel.Port, paramModel.Channel);
                msg.Info = "False";
                output = UsbHubHelper.Instance.ExecuteCmd(addDeviceCmd);
                logger.LogTrace(output);
                Thread.Sleep(1000);
                output = UsbHubHelper.Instance.ExecuteCmd(openChannel);
                logger.LogTrace(output);
                if (output.ToLower().Contains("connected")||output.ToLower().Contains("success"))
                {
                     msg.Info = "True";
                }
               else
                {
                    if(string.IsNullOrEmpty(output))
                    {
                        output = "执行命令返回为空，请检查是否安装了USB网络服务器管理工具".I18n("NetworkOpen_B788F92C");
                    }
                     msg.Code = (int)eErrorMessageType.Error_LocalOpenError; ;
                     msg.Message = output;
                     logger.LogError(output);
                }
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_LocalOpenError, ref msg);
            }
            return msg;
        }

        protected override void ValidateParameter(ParamNetworkOpenModel paramModel)
        {
            try
            {
                logger.LogDebug("Start NetworkOpen");
                if (string.IsNullOrEmpty(paramModel.IpAddress))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError, "IP地址参数不可为空".I18n("controlType_NetworkOpen_8AB8AC9C"));
                }
                if (string.IsNullOrEmpty(paramModel.Channel))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError, "通道号参数不可为空".I18n("controlType_NetworkOpen_FE6B657D"));
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

