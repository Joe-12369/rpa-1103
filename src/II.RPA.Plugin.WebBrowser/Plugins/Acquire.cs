using II.RPA.Plugin.Core;
using II.RPA.Plugin.WebBrowser.Params;
using II.RPA.WebBrowser.Common;
using II.RPA.WinAutomation.CommonUtility;
using System;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.WebBrowser.Plugins
{
    public class Acquire : BrowserComponentBase<ParamAcquireModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override RPA.WinAutomation.CommonUtility.eComponentType ComponentType => eComponentType.Acquire;
        protected override JMessage Execute(ParamAcquireModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                var @params = new BrowserParams()
                {
                    windowId = "",
                    tabId = "",
                    browserType = paramModel.browserType,
                    webData = paramModel.webData
                };
                //BrowserAction.browserAction = BrowserAction.GetActionBase(@params);
                var browserAction = WebBrowserManager.GetBrowserAction(@params);
                if (browserAction == null)
                {
                    throw new RPAException(eErrorMessageType.Error_PageNotFound);
                }
                msg.Info = browserAction.Acquire(paramModel.value, paramModel.mode, paramModel.index, paramModel.isActive, paramModel.isMatchAll, paramModel.browserType);
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_AcquireFail, ref msg);
            }
            return msg;

        }

        protected override void ValidateParameter(ParamAcquireModel paramModel)
        {
            try
            {
                //if (string.IsNullOrEmpty(paramModel.value))
                //{
                //    throw new RPAException(eErrorMessageType.Error_ParamsError);
                //}
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"获取Tab页 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}

