using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.OutLook.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.OutLook.Plugins
{
    public class CheckAccount : PluginAbilityBase<ParamCheckAccountModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.CheckAccount;

        protected override JMessage Execute(ParamCheckAccountModel paramModel, PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                var accountArray = OutLookEmailHelper.CheckAccount();
                msg.Info = JsonHelper.JsonByNewtonJson<List<string>>(accountArray);
            }
            catch (RPAException RpaEx)
            {
                logger.LogError(RpaEx);
                Exception(RpaEx, RpaEx.ErrorMessageType, ref msg);
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                Exception(ex, eErrorMessageType.Error_NotFoundMailAccount, ref msg);
            }
            return msg;
        }

        protected override void ValidateParameter(ParamCheckAccountModel paramModel)
        {
        }
    }
}
