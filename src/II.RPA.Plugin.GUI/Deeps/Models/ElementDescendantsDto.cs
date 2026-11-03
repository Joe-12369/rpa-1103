using Newtonsoft.Json;
using System.Collections.Generic;

namespace II.RPA.Plugin.GUI.Deeps.Models
{
    internal class ElementDescendantsDto : PayloadDto
    {
        [JsonProperty("elementId")]
        public string ElementId { get; set; }

        [JsonProperty("descendants")]
        public IEnumerable<ElementModel> Descendants { get; set; }
        }
}
