using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.WebBrowser.Params;
using II.RPA.WebBrowser.Common;
using II.RPA.WinAutomation.CommonUtility;
using System;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WebBrowser.Common.Basic;

namespace II.RPA.Plugin.WebBrowser.Plugins
{
    public class GetTabDetail : BrowserComponentBase<ParamGetTabDetailModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override RPA.WinAutomation.CommonUtility.eComponentType ComponentType => eComponentType.GetTabDetail;

        protected override JMessage Execute(ParamGetTabDetailModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
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
                msg.Info = browserAction.GetTabDetail();
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_GetTabDetailFail, ref msg);
            }
            return msg;

        }

        protected override void ValidateParameter(ParamGetTabDetailModel paramModel)
        {
            try
            {
                if (string.IsNullOrEmpty(paramModel.browserType) && string.IsNullOrEmpty(paramModel.webData) )
                {
                    paramModel.browserType = BrowserWindowProvider.GetFirstBrowserByZOrder()?.WindowType.ToString() ?? string.Empty;
                }
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"获取Tab信息 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}




