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
    public class Wait : BrowserComponentBase<ParamWaitModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override RPA.WinAutomation.CommonUtility.eComponentType ComponentType => eComponentType.Wait;

        protected override JMessage Execute(ParamWaitModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
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
                bool tResult = browserAction.Wait();
                if (!tResult)
                {
                    throw new RPAException(eErrorMessageType.Error_WaitTimeout);
                }
            }
            catch (RPAException RpaEx)
            {
                Exception(RpaEx, RpaEx.ErrorMessageType, ref msg);
                return msg;
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_WaitFail, ref msg);
            }
            return msg;
        }

        protected override void ValidateParameter(ParamWaitModel paramModel)
        {
            try
            {
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"等待加载 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}
