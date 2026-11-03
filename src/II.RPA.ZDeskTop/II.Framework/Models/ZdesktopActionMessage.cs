using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace II.Framework.Models
{
    public class ZdesktopActionMessage
    {
        [JsonProperty("action")]
        public string Action { get; set; }
        [JsonIgnore]
        public string Payload { get; set; }
        [JsonProperty("requestId")]
        public string RequestId { get; set; }

        [JsonProperty("payload")]
        public JObject PayloadObj { get; set; }
    }
}
