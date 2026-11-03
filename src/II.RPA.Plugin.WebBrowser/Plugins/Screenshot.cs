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
    public class Screenshot : BrowserComponentBase<ParamScreenshotModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override RPA.WinAutomation.CommonUtility.eComponentType ComponentType => eComponentType.Screenshot;

        protected override JMessage Execute(ParamScreenshotModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
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
                browserAction.Screenshot(paramModel.savePath);
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_ScreenshotFail, ref msg);
            }
            return msg;

        }

        protected override void ValidateParameter(ParamScreenshotModel paramModel)
        {
            try
            {
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"浏览器截图 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}














