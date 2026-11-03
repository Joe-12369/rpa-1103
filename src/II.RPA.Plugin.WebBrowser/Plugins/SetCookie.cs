using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.WebBrowser.Params;
using II.RPA.WebBrowser.Common;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.WebBrowser.Plugins
{
    public class SetCookie : BrowserComponentBase<ParamSetCookieModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override RPA.WinAutomation.CommonUtility.eComponentType ComponentType => eComponentType.SetCookie;

        protected override JMessage Execute(ParamSetCookieModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {

                var @params = new BrowserParams()
                {
                    windowId = "",
                    tabId = "",
                    browserType = paramModel.browserType,
                    webData = paramModel.webData,
                };
                var browserAction = WebBrowserManager.GetBrowserAction(@params);
                var value = JsonConvert.DeserializeObject<JToken>(paramModel.webData);
                browserAction.SetCookie(value["url"].ToString(), paramModel.value);
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_SetCookieFail, ref msg);
            }
            return msg;

        }

        protected override void ValidateParameter(ParamSetCookieModel paramModel)
        {
            try
            {
                if (string.IsNullOrEmpty(paramModel.value))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"设置Cookie 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}
