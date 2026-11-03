using Newtonsoft.Json;

namespace II.RPA.Plugin.GUI.Deeps.Models
{
    internal class BuildSelectorDto : PayloadDto
    {
        [JsonProperty("elementId")]
        public string ElementId { get; set; }
        [JsonProperty("selector")]
        public string Selector { get; set; }
    }
}
