using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.RPASystem.Helper;
using II.RPA.Plugin.RPASystem.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.RPASystem.Plugins
{
    public class LocalRemoteOff : SystemComponentBase<ParamLocalRemoteOffModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.ReomteOff;

        protected override JMessage Execute(ParamLocalRemoteOffModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                User32Helper.Close(RdpHelper.CLASS_NAME, RdpHelper.GetTitle(paramModel.User, paramModel.Ip));
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                Exception(ex, eErrorMessageType.Error_LocalRemoteOff, ref msg);
            }

            return msg;
        }

        protected override void ValidateParameter(ParamLocalRemoteOffModel paramModel)
        {
            if (string.IsNullOrEmpty(paramModel.User) || string.IsNullOrEmpty(paramModel.Ip))
            {
                throw new RPAException(eErrorMessageType.Error_LocalRemoteOff_AccountOrIp);
            }
        }
    }
}
