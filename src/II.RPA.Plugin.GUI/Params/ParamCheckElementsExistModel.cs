using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamCheckElementsExistModel : ParamBase
    {
        [JsonProperty("processName")]
        public string ProcessName { get; set; }
        [JsonProperty("selectors")]
        public List<SelectorInfo> Selectors { get; set; }

    }
    public class SelectorInfo
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }
        [JsonProperty("selector")]
        public string Selector { get; set; }
    }
}
