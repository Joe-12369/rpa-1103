using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.GUI.Commons.WindowMatch.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace II.RPA.Plugin.ZExplorer.Config
{
    public class ZExplorerConfigurations : ConfigurationBase<ZExplorerConfigurations>
    {
        public ZExplorerConfigurations()
        {

        }
        [JsonProperty("app")]
        public List<AppConfig> App { get; set; }

        [JsonProperty("arguments")]
        public string[] Arguments { get; set; }
        public AppConfig GetSetting(string name)
        {
            try
            {
                var app = Info.App.Find(a => a.name.Equals(name));
                return app;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        /// <summary>
        /// 窗口匹配配置映射
        /// </summary>
        [JsonProperty("matchConfigMap")]
        public IDictionary<string, MatchConfig> MatchConfigMap { get; set; }
    }
    public class AppConfig
    {
        public string name { get; set; }
        public string className { get; set; } = "Chrome_RenderWidgetHostHWND";
        public bool enable { get; set; }
        public int port { get; set; }
    }

}
