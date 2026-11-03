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
    public class GetInspectNetwork : BrowserComponentBase<ParamGetInspectNetworkModel>
    { 
        protected override eComponentType ComponentType => eComponentType.GetInspectNetwork;
        private static ILogger logger = Logger.OfContext();

        protected override JMessage Execute(ParamGetInspectNetworkModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
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
                msg.Info = browserAction.GetInspectNetwork(paramModel.type, paramModel.url, paramModel.isRegex == 1, paramModel.filePath);
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_StartInspectNewwork, ref msg);
            }
            return msg;

        }

        protected override void ValidateParameter(ParamGetInspectNetworkModel paramModel)
        {
            try
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogInformation($"获取网络请求 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}













