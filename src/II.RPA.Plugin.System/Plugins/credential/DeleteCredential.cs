using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.RPASystem.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace II.RPA.Plugin.RPASystem.Plugins
{
    public class DeleteCredential : SystemComponentBase<ParamReadCredential>
    {
        private static ILogger logger = Logger.OfContext();

        private string[] application;
        protected override eComponentType ComponentType => eComponentType.DeleteCredential;

        protected override void ValidateParameter(ParamReadCredential paramModel)
        {
            if (paramModel.application == null || string.IsNullOrWhiteSpace(paramModel.application.ToString()))
            {
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        protected override JMessage Execute(ParamReadCredential paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                IEnumerable<string> targetArr;
                if (paramModel.application is string)
                {
                    var strArr = paramModel.application.ToString().Trim(new char[] { '[', ']' }).Replace("'", "").Replace("\"", "");
                    targetArr = from c in strArr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                select c.Trim();
                }
                else
                {
                    targetArr = (paramModel.application as JArray).ToObject<IEnumerable<string>>();
                }
                var type = paramModel.type == 0 ? CredentialType.Generic : CredentialType.DomainPassword;
                foreach (var c in targetArr)
                {
                    if (CredentialManager.ReadCredential(c, type) != null)
                    {
                        CredentialManager.CredDelete(c, (uint)type, 0);
                    }
                    else
                    {
                        msg.Code = -1;
                        msg.UnifyCode = "-1";
                        msg.Message = Localize($"删除凭据{c}失败,凭据不存在", "DeleteCredential_CC0D2981");
                        break;
                    }

                }
            }
            catch (Exception)
            {
                msg.Code = -1;
                msg.UnifyCode = "-1";
                msg.Message = "删除凭据失败".I18n("DeleteCredential_BBDA0F3E");

            }
            return msg;
        }

    }
}

