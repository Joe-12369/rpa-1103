using II.RPA.Plugin.Core.Entities;
using Newtonsoft.Json;

namespace II.RPA.Plugin.Core.PluginBase
{
    public abstract class PluginParameterBase
    {
        [JsonProperty("componentLabel")]
        public string ComponentLabel { get; set; } = string.Empty;

        [JsonProperty("runParam")]
        public RunParamModel RunParam { get; set; }
    }
}
