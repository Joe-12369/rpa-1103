using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons.WindowMatch.Entities;
using II.RPA.Plugin.WebBrowser.Params;
using II.RPA.WebBrowser.Common;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.WebBrowser.Plugins
{
    public class GetSupportedBrowserList : BrowserComponentBase<ParamBase>
    {
        protected override eComponentType ComponentType => eComponentType.GetSupportedBrowserList;
        protected override JMessage Execute(ParamBase paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                msg.Info = GetInternalBrowserArr().ToString();
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_GetScrollFail, ref msg);
            }
            return msg;
        }

        private JArray GetInternalBrowserArr()
        {
            var dataList = new[]
                    {
                        new { browserName = "谷歌浏览器".I18n("GetSupportedBrowserList_84B401AB"), browserType = "chrome" },
                        new { browserName = "IE浏览器".I18n("GetSupportedBrowserList_53C72CE2"), browserType = "ie" },
                        new { browserName = "火狐浏览器".I18n("GetSupportedBrowserList_A566EDAB"), browserType = "firefox" },
                        new { browserName = "实在浏览器".I18n("GetSupportedBrowserList_F41CFC2A"), browserType = "zexplorer" },
                        new { browserName = "Edge浏览器".I18n("GetSupportedBrowserList_FBB2C8B9"), browserType = "edge" },
                        new { browserName = "360安全浏览器".I18n("GetSupportedBrowserList_D2AB0C6C"), browserType = "Web360se" },
                        new { browserName = "360极速浏览器".I18n("GetSupportedBrowserList_1076A765"), browserType = "web360x" },
                        new { browserName = "搜狗浏览器".I18n("GetSupportedBrowserList_F68BE1B1"), browserType = "Sogou" },
                        new { browserName = "紫鸟浏览器".I18n("GetSupportedBrowserList_FF75FF76"), browserType = "superbrowser" }
                    };
            //避免混淆带来Key错误
            JArray arr = new JArray();
            foreach (var data in dataList)
            {
                arr.Add(new JObject { { "browserType", data.browserType }, { "browserName", data.browserName } });
            }

            var configMap = SetMatchConfigMap();
            foreach (var matchConfig in configMap.Values)
            {
                arr.Add(new JObject { { "browserType", matchConfig.ProductName }, { "browserName", matchConfig.LableName } });
            }
            return arr;
        }

        private Dictionary<string, MatchConfig> SetMatchConfigMap()
        {
            var map = new Dictionary<string, MatchConfig>();
            try
            {
                var localApplication = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var configFilePath_old = Path.Combine(localApplication, "IIRPA\\ChromiunBrowserConfig.json");//700sp3~711
                var configFilePath = Path.Combine(localApplication, "IIRPA\\ChromiumBrowserConfig.json");
                if (File.Exists(configFilePath))
                {
                    var configObj = JObject.Parse(File.ReadAllText(configFilePath));
                    map = configObj["BrowserConfigMap"].ToObject<Dictionary<string, MatchConfig>>();
                }
                else if (File.Exists(configFilePath_old))
                {
                    File.Copy(configFilePath_old, configFilePath);
                    var configObj = JObject.Parse(File.ReadAllText(configFilePath));
                    map = configObj["BrowserConfigMap"].ToObject<Dictionary<string, MatchConfig>>();
                }
            }
            catch (Exception ex)
            {
                //Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"读取Chromiun浏览器配置信息异常，需要检查位置文件格式");
            }
            return map;
        }
    }
}

