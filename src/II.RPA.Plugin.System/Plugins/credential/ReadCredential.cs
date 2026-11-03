using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.RPASystem.Params;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace II.RPA.Plugin.RPASystem.Plugins
{
    public class ReadCredential : SystemComponentBase<ParamReadCredential>
    {
        private static ILogger logger = Logger.OfContext();
        protected override WinAutomation.CommonUtility.eComponentType ComponentType => WinAutomation.CommonUtility.eComponentType.ReadCredential;

        protected override JMessage Execute(ParamReadCredential paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                var credType = paramModel.type == 0 ? CredentialType.Generic : CredentialType.DomainPassword;
                Func<IEnumerable<string>, object> func_ReadCredential = (IEnumerable<string> targetArr) =>
                {
                    if (targetArr.Count() == 1)
                    {
                        var credInfo = CredentialManager.ReadCredential(targetArr.First(), credType);
                        return new JObject() { { "application", credInfo.ApplicationName }, { "username", credInfo.UserName ?? "" }, { "password", credInfo.Password ?? "" } };
                    }
                    else
                    {
                        var credList = new List<JObject>();
                        foreach (var target in targetArr)
                        {
                            var _cred = CredentialManager.ReadCredential(target, credType);
                            credList.Add(new JObject() { { "application", _cred.ApplicationName }, { "username", _cred.UserName ?? "" }, { "password", _cred.Password ?? "" } });
                        }
                        return credList;
                    }
                };


                if (paramModel.application is string)
                {
                    if (string.IsNullOrWhiteSpace(paramModel.application?.ToString()))
                    {
                        var credList = new List<JObject>();
                        var credInfo = CredentialManager.EnumerateCrendentials();
                        foreach (var _cred in credInfo)
                        {
                            if (_cred.CredentialType == credType)
                            {
                                credList.Add(new JObject() { { "application", _cred.ApplicationName }, { "username", _cred.UserName ?? "" }, { "password", _cred.Password ?? "" } });
                            }
                        }
                        msg.Info = JsonConvert.SerializeObject(credList);
                    }
                    else
                    {
                        var strArr = paramModel.application.ToString().Trim(new char[] { '[', ']' }).Replace("'", "").Replace("\"", "");
                        var targetArr = from c in strArr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                        select c.Trim();
                        msg.Info = JsonConvert.SerializeObject(func_ReadCredential(targetArr));

                    }

                }
                else
                {
                    var targetArr = (paramModel.application as JArray).ToObject<string[]>();
                    msg.Info = JsonConvert.SerializeObject(func_ReadCredential(targetArr));
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                msg.Code = -1;
                msg.UnifyCode = "-1";
                msg.Message = "获取系统凭据失败！".I18n("ReadCredential_84477978");

            }
            return msg;
        }
    }

}
