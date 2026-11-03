using Newtonsoft.Json;
using System.Collections.Generic;

namespace II.RPA.Plugin.GUI.Deeps.Models
{
    internal class ElementDetailDto : PayloadDto
    {
        [JsonProperty("elementId")]
        public string ElementId { get; set; }

        [JsonProperty("elementAlias")]
        public string ElementAlias { get; set; }

        [JsonProperty("meta")]
        public List<ElementPropertyDto> Meta { get; set; }
    }
}
