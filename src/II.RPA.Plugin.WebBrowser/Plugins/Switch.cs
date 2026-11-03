using II.RPA.Plugin.Core;
using II.RPA.Plugin.WebBrowser.Params;
using II.RPA.WebBrowser.Common;
using II.RPA.WinAutomation.CommonUtility;
using System;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.WebBrowser.Plugins
{
    public class Switch : BrowserComponentBase<ParamSwitchModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override RPA.WinAutomation.CommonUtility.eComponentType ComponentType => eComponentType.SwitchTab;
        protected override JMessage Execute(ParamSwitchModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
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
                if (browserAction == null)
                {
                    throw new RPAException(eErrorMessageType.Error_PageNotFound);
                }
                browserAction.Switch(paramModel.isActive);

            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_SwitchTabFail, ref msg);
            }
            return msg;

        }
    }
}
