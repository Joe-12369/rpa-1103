using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons.Params
{
    public class ParamAiEnhancedModel
    {
        [JsonProperty("elementId")]
        public string ElementId { get; set; }
        [JsonProperty("retrieveSelector")]
        public string RetrieveSelector { get; set; }
        [JsonProperty("retrieveTime")]
        public string RetrieveTime { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }
        [JsonProperty("repairSource")]
        public string RepairSource { get; set; }
    }
}
