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
    public class SendMail : PluginAbilityBase<ParamSendMailModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.SendMail;

        protected override JMessage Execute(ParamSendMailModel paramModel, PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            msg.Info = "False";
            try
            {
                OutLookEmailHelper.SendMail(paramModel);
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
                Exception(ex, eErrorMessageType.Error_SendMail, ref msg);
            }
            return msg;
        }

        protected override void ValidateParameter(ParamSendMailModel paramModel)
        {
            try
            {
                if (string.IsNullOrEmpty(paramModel.MailTo))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError, "收件人参数不可为空".I18n("controlType_SendMail_23169C4B"));
                }
                if (string.IsNullOrEmpty(paramModel.MailHTMLBody))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError, "邮件内容参数不可为空".I18n("controlType_SendMail_76B1299"));
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

