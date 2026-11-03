using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.WebBrowser.Params;
using II.RPA.WebBrowser.Common;
using II.RPA.WinAutomation.CommonUtility;
using System;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.WebBrowser.Plugins
{
    public class Executejs : BrowserComponentBase<ParamExecutejsModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override RPA.WinAutomation.CommonUtility.eComponentType ComponentType => eComponentType.Executejs;

        protected override JMessage Execute(ParamExecutejsModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
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
                if (string.IsNullOrWhiteSpace(paramModel.@params))
                    msg.Info = browserAction.Executejs(paramModel.title, paramModel.url, paramModel.scriptCode, null);
                else
                    msg.Info = browserAction.Executejs(paramModel.title, paramModel.url, paramModel.scriptCode, paramModel.@params);
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_ParamsError, ref msg);
            }
            return msg;

        }

        protected override void ValidateParameter(ParamExecutejsModel paramModel)
        {
            try
            {
                if (string.IsNullOrEmpty(paramModel.title))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                if (string.IsNullOrEmpty(paramModel.scriptCode))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"执行JS 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}


