using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.RPASystem.Helper;
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
    public class CheckScreenState : SystemComponentBase<Params.ParamEmptyModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.LockState;

        protected override JMessage Execute(Params.ParamEmptyModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                msg.Info = ScreenStateHelper.GetScreenState();
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                Exception(ex, eErrorMessageType.Error_Lock_State, ref msg);
            }
            return msg;
        }

    }
}
