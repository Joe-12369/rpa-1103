using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.RPASystem.Config;
using II.RPA.Plugin.RPASystem.Helper;
using II.RPA.Plugin.RPASystem.Params;
using II.RPA.Plugin.RPASystem.SR;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.RPASystem.Plugins
{
    public class RemoteUnLock : SystemComponentBase<ParamRemoteLockModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.RemoteUnlocking;

        protected override JMessage Execute(ParamRemoteLockModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
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
                        channelproxy.DoWorkAsync(paramModel.User, paramModel.Pw, paramModel.Ip, paramModel.Port);
                    }
                }
                Thread.Sleep(paramModel.WaitTime);
                if (ScreenStateHelper.GetScreenState() == "Locked")
                {
                    throw new RPAException(eErrorMessageType.Error_RemoteUnLock);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                Exception(ex, eErrorMessageType.Error_RDPLock, ref msg);
            }

            return msg;
        }

        protected override void ValidateParameter(ParamRemoteLockModel paramModel)
        {
            if (string.IsNullOrEmpty(paramModel.User) || string.IsNullOrEmpty(paramModel.Pw) || string.IsNullOrEmpty(paramModel.Ip))
            {
                throw new RPAException(eErrorMessageType.Error_RemoteUnLock_AccountOrPwd);
            }
        }
    }
}
