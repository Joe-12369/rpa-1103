using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.OutLook.Models;
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
    public class CheckMail : PluginAbilityBase<ParamCheckMailModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.CheckMail;

        protected override JMessage Execute(ParamCheckMailModel paramModel, PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                var mailInfos = OutLookEmailHelper.CheckMail(paramModel);
                msg.Info = JsonHelper.JsonByNewtonJson<List<MailInfo>>(mailInfos);
            }
            catch (RPAException RpaEx)
            {
                logger.LogError(RpaEx);
                Exception(RpaEx, RpaEx.ErrorMessageType, ref msg);
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                Exception(ex, eErrorMessageType.Error_CheckMail, ref msg);
            }
            return msg;
        }

        protected override void ValidateParameter(ParamCheckMailModel paramModel)
        {
        }
    }
}

