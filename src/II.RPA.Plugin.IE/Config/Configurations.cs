using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.GUI.Commons.WindowMatch.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.IE.Config
{
    public class Configurations : ConfigurationBase<Configurations>
    {
        private Configurations()
        {
        }
        /// <summary>
        /// 窗口匹配配置映射
        /// </summary>
        [JsonProperty("matchConfigMap")]
        public IDictionary<string, MatchConfig> MatchConfigMap { get; set; }
    }
}
