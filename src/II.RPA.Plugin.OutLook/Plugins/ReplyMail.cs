using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
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
    public class ReplyMail : PluginAbilityBase<ParamReplyMailModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.ReplyMail;

        protected override JMessage Execute(ParamReplyMailModel paramModel, PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            msg.Info = "False";
            try
            {
                OutLookEmailHelper.ReplayMail(paramModel);
                msg.Info = "True";
            }
            catch (RPAException RpaEx)
            {
                logger.LogError(RpaEx);
                Exception(RpaEx, RpaEx.ErrorMessageType, ref msg);
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                Exception(ex, eErrorMessageType.Error_ReplyMail, ref msg);
            }
            return msg;
        }

        protected override void ValidateParameter(ParamReplyMailModel paramModel)
        {
            try
            {
                if (string.IsNullOrEmpty(paramModel.AwaitingOperateMail))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError, "邮件对象不可为空".I18n("controlType_ReplyMail_F01EB1FB"));
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

