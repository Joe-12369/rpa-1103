using II.RPA.Plugin.Core;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.Plugin.Abstractions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons
{
    public class PluginActionHelper
    {
        private const string ACTION_NAME_WEBBROWSER_ACTIVE = "WebBrowser.Active";
        private const string ACTION_NAME_WEBBROWSER_ACQUIRE = "WebBrowser.Acquire";
        #region 激活网页

        public static async Task<JMessage> WebBrowserActiveAsync(
            IPluginInvoker pluginInvoker,
            string browserType,
            string webData)
        {
            JObject pJObj = new JObject() {
                                { "browserType", browserType },
                                { "webData",webData}
                            };
            //使用WebBrowser.Acquire 支持无网页对象的情况，WebBrowser.active 必须有网页对象
            var result = await pluginInvoker.InvokeAsync(
                PluginRoute.Parse(ACTION_NAME_WEBBROWSER_ACTIVE),
                pJObj.ToString()).ConfigureAwait(false);
            return result.ToObject<JMessage>();
        }
        #endregion
    }
}
