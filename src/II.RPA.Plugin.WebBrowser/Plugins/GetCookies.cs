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
    public class GetCookies : BrowserComponentBase<ParamGetCookiesModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override RPA.WinAutomation.CommonUtility.eComponentType ComponentType => eComponentType.GetCookies;

        protected override JMessage Execute(ParamGetCookiesModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
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
                if (string.IsNullOrEmpty(paramModel.cookieModel))
                    paramModel.cookieModel = "auto";
                var url = paramModel.cookieModel == "auto" ? value["url"].ToString() : paramModel.url;
                if (string.IsNullOrEmpty(url))
                    url = null;
                var cookies = browserAction.GetCookies(url);
                if (string.IsNullOrEmpty(paramModel.name))
                    msg.Info = cookies;
                else
                {
                    var values = JArray.Parse(cookies);
                    foreach (var cookie in values)
                    {
                        if (cookie.Value<string>("name").Trim() == paramModel.name)
                        {
                            msg.Info = cookie.Value<string>("value").ToString();
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_GetCookiesFail, ref msg);
            }
            return msg;

        }

        protected override void ValidateParameter(ParamGetCookiesModel paramModel)
        {
            try
            {
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"获取Cookie 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}


