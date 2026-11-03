using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.GUI.Commons.WindowMatch.Entities;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace II.RPA.Plugin.Browsers.Config
{
    public sealed class Configurations : ConfigurationBase<Configurations>
    {
        private Configurations() { }

        [JsonProperty("extensionsWhitelist")]
        public string[] ExtensionsWhitelist { get; set; } = new string[0];

        [JsonProperty("matchConfigMap")]
        public IDictionary<string, MatchConfig> MatchConfigMap { get; set; }
            = new Dictionary<string, MatchConfig>();
    }
}
