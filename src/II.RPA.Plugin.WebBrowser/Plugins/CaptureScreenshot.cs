using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Core.Threading;
using II.RPA.Plugin.WebBrowser.Params;
using II.RPA.WebBrowser.Common;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;

namespace II.RPA.Plugin.WebBrowser.Plugins
{
    public class CaptureScreenshot : StaBrowserComponentBase<ParamCaptureScreenshotModel>
    {
        private static ILogger logger = Logger.OfContext();

        public CaptureScreenshot(IStaDispatcher staDispatcher)
            : base(staDispatcher)
        {
        }

        protected override eComponentType ComponentType => eComponentType.CaptureScreenshot;

        protected override JMessage Execute(ParamCaptureScreenshotModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
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
                if (paramModel.height == 0)
                {
                    paramModel.height = 25000;
                }
                paramModel.height = Math.Min(paramModel.height, 60000);
                browserAction.CaptureScreenshot(paramModel.saveToClp == 1, paramModel.cutRange, paramModel.picPath, paramModel.cover == 1, paramModel.height);
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_CaptureScreen_Error, ref msg);
            }
            return msg;

        }

        protected override void ValidateParameter(ParamCaptureScreenshotModel paramModel)
        {
            try
            {
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"浏览器长截图 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}
