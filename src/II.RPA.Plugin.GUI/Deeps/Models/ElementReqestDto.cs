using Newtonsoft.Json;

namespace II.RPA.Plugin.GUI.Deeps.Models
{
    public class ElementReqestDto
    {
        [JsonProperty("elementId")]
        [JsonRequired]
        public string ElementId { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }
    }
}
