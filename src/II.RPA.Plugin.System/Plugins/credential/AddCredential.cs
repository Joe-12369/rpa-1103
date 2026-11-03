using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.RPASystem.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace II.RPA.Plugin.RPASystem.Plugins
{
    public class AddCredential : SystemComponentBase<ParamCredential>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.AddCredential;

        protected override void ValidateParameter(ParamCredential paramModel)
        {
            if (string.IsNullOrWhiteSpace(paramModel.application) || string.IsNullOrWhiteSpace(paramModel.username))
            {
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        protected override JMessage Execute(ParamCredential paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                var credType = paramModel.type == 0 ? CredentialType.Generic : CredentialType.DomainPassword;
                var persistence = paramModel.persistence == 0 ? CredentialPersistence.Enterprise : paramModel.persistence == 1 ? CredentialPersistence.LocalMachine : CredentialPersistence.Session;
                var written = CredentialManager.WriteCredential(paramModel.application, paramModel.username, DecodeBase64(paramModel.password), credType, persistence);
                if (!written)
                {
                    int lastError = Marshal.GetLastWin32Error();
                    logger.LogError(string.Format("CredWrite failed with the error code {0}.", lastError));
                    msg.Code = -1;
                    msg.UnifyCode = "-1";
                    msg.Message = "添加系统凭据失败！".I18n("AddCredential_118A5156");
                }
            }
            catch (Exception ex)
            {
                msg.Code = -1;
                msg.UnifyCode = "-1";
                msg.Message = "添加系统凭据失败！".I18n("AddCredential_118A5156");

            }
            return msg;
        }

        private string DecodeBase64(string code, string code_type = "utf-8")
        {
            string decode = "";
            byte[] bytes = Convert.FromBase64String(code);
            try
            {
                decode = Encoding.GetEncoding(code_type).GetString(bytes);
            }
            catch
            {
                decode = code;
            }
            return decode;
        }
    }
}

