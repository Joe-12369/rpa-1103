using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.OutLook.Params;
using II.RPA.Plugin.OutLook.Params.Enum;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.OutLook.Plugins
{
    public class OperateMail : PluginAbilityBase<ParamOperateMailModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.OperateMail;

        protected override JMessage Execute(ParamOperateMailModel paramModel, PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            msg.Info = "False";
            try
            {
                if (paramModel.MailOperateType == E_MailOperateType.MarkAsRead
                    || paramModel.MailOperateType == E_MailOperateType.MarkAsUnRead)
                {
                    OutLookEmailHelper.MarkMail(paramModel);
                }
                if (paramModel.MailOperateType == E_MailOperateType.DownLoadAttachments)
                {
                    OutLookEmailHelper.DownLoadMailAttachment(paramModel);
                }
                if (paramModel.MailOperateType == E_MailOperateType.DeleteMail)
                {
                    OutLookEmailHelper.DeteteMail(paramModel);
                }
                if (paramModel.MailOperateType == E_MailOperateType.MoveMail)
                {
                    OutLookEmailHelper.MoveMail(paramModel);
                }
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
                Exception(ex, eErrorMessageType.Error_OperateMail, ref msg);
            }
            return msg;
        }

        protected override void ValidateParameter(ParamOperateMailModel paramModel)
        {
            try
            {
                if (string.IsNullOrEmpty(paramModel.AwaitingOperateMail))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError, "邮件对象不可为空".I18n("controlType_OperateMail_F01EB1FB"));
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

