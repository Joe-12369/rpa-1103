using Newtonsoft.Json;

namespace II.RPA.Plugin.GUI.Deeps.Models
{
    public class ElementPropertyDto
    {
        [JsonProperty("key")]
        public string Key { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
