using Newtonsoft.Json;
using System.Collections.Generic;

namespace II.RPA.Plugin.GUI.Deeps.Models
{
    public class UiaTreeResultDto
    {
        [JsonProperty("tree")]
        public ElementModel[] Tree { get; set; }
        [JsonProperty("active")]
        public List<string> Active { get; set; } = new List<string>();
        [JsonIgnore]
        public bool Deprecated => Tree == null;
    }
}
