using Newtonsoft.Json;

namespace II.RPA.Plugin.Core.Services
{
    public class RuntimeMessageModel
    {
        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("requestId")]
        public string RequestId { get; set; }

        [JsonProperty("payload")]
        public string Payload { get; set; }
    }
}
