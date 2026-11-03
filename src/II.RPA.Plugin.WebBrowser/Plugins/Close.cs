using II.RPA.Plugin.Core;
using II.RPA.Plugin.WebBrowser.Params;
using II.RPA.WebBrowser.Common;
using II.RPA.WinAutomation.CommonUtility;
using System;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;


namespace II.RPA.Plugin.WebBrowser.Plugins
{
    public class Close : BrowserComponentBase<ParamCloseModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override RPA.WinAutomation.CommonUtility.eComponentType ComponentType => eComponentType.Close;

        protected override JMessage Execute(ParamCloseModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
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
                browserAction.Close(paramModel.IsCloseAlert);
            }
            catch (Exception ex)
            {
                if (ex.IsRPAException(eErrorMessageType.Error_PageNotFound, out _))
                {
                    logger.LogInformation($"未匹配到网页对象对应的tab信息，网页可能已被关闭");
                }
                else
                {
                    Exception(ex, eErrorMessageType.Error_CloseFail, ref msg);
                }
            }
            return msg;

        }

        protected override void ValidateParameter(ParamCloseModel paramModel)
        {
            try
            {
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"关闭网页 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}

