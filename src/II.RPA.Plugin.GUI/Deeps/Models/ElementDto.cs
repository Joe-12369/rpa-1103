using Newtonsoft.Json;
using System.Collections.Generic;

namespace II.RPA.Plugin.GUI.Deeps.Models
{
    public class ElementDto
    {
        public ElementDto(string id, string name, string tag)
        {
            Id = id;
            Name = name;
            Tag = tag;
            Children = new List<ElementDto>();
        }
        [JsonProperty("elementId")]
        public string Id { get; private set; }

        [JsonProperty("name")]
        public string Name { get; private set; }

        [JsonProperty("tag")]
        public string Tag { get; private set; }

        [JsonProperty("children")]
        public List<ElementDto> Children { get; private set; }

        public string ToJson()
        {
            if (string.IsNullOrEmpty(Id))
                throw new System.InvalidOperationException($"'{nameof(Id)}' requires a valid value.");
            return JsonConvert.SerializeObject(this);
        }
    }
}
