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
    public class DownloadUrl : BrowserComponentBase<ParamDownloadUrlModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override RPA.WinAutomation.CommonUtility.eComponentType ComponentType => eComponentType.DownloadUrl;

        protected override JMessage Execute(ParamDownloadUrlModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
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
                msg.Info = browserAction.DownloadUrl(paramModel.downLoadUrl, paramModel.saveFile, paramModel.timeout);
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_CreateFail, ref msg);
            }
            return msg;

        }

        protected override void ValidateParameter(ParamDownloadUrlModel paramModel)
        {
            try
            {
                if (string.IsNullOrEmpty(paramModel.downLoadUrl))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"打开新的浏览器 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}
