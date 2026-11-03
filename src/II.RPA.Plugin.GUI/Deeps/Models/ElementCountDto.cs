using Newtonsoft.Json;

namespace II.RPA.Plugin.GUI.Deeps.Models
{
    internal class ElementCountDto : PayloadDto
    {
        [JsonProperty("count")]
        public int Count { get; set; }
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}
