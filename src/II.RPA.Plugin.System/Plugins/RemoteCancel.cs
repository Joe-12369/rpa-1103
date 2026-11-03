using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.RPASystem.Config;
using II.RPA.Plugin.RPASystem.Params;
using II.RPA.Plugin.RPASystem.SR;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.RPASystem.Plugins
{
    public class RemoteCancel : SystemComponentBase<ParamRemoteCancelModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.RemoteCancel;

        protected override JMessage Execute(ParamRemoteCancelModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                using (var factory = new ChannelFactory<IRemoteRunService>(new BasicHttpBinding(),
                                        new EndpointAddress(SysConfigurations.Info.RemoteServiceAddress)))
                {
                    IRemoteRunService channelproxy = factory.CreateChannel();
                    using (channelproxy as IDisposable)
                    {
                        channelproxy.CanelConnectionAsync(paramModel.User, paramModel.Ip);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                Exception(ex, eErrorMessageType.Error_RemoteCancel, ref msg);
            }
            return msg;
        }

        protected override void ValidateParameter(ParamRemoteCancelModel paramModel)
        {
            if (string.IsNullOrEmpty(paramModel.User) || string.IsNullOrEmpty(paramModel.Ip))
            {
                throw new RPAException(eErrorMessageType.Error_RemoteCancel_AccountOrIp);
            }
        }
    }
}
