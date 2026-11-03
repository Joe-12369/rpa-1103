using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.WebBrowser.Params;
using II.RPA.WebBrowser.Common;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;

namespace II.RPA.Plugin.WebBrowser.Plugins
{
    public class EndInspectNetwork : BrowserComponentBase<ParamBase>
    {
     
        protected override eComponentType ComponentType => eComponentType.EndInspectNetwork;
        private static ILogger logger = Logger.OfContext();

        protected override JMessage Execute(ParamBase paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
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
                if (WebBrowserManager.supportGuiType.IndexOf($",{browserAction.GuiType.ToString().ToLower()},") < 0)
                {
                    Exception(eErrorMessageType.Error_NotImplemented, ref msg);
                    return msg;
                }
                browserAction.EndInspectNetwork();
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_StartInspectNewwork, ref msg);
            }
            return msg;

        }

        protected override void ValidateParameter(ParamBase paramModel)
        {
            try
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogInformation($"结束监听网络请求 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}













