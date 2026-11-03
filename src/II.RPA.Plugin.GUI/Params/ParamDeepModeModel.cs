using Newtonsoft.Json;

namespace II.RPA.Plugin.GUI.Params
{

    public class ParamDeepCaptureModel : ParamBase
    {
        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("payload", NullValueHandling = NullValueHandling.Ignore)]
        public ParamDeepCapturePayloadModel Payload { get; set; }
    }

    public class ParamDeepCapturePayloadModel : ParamBase
    {
        [JsonProperty("handles")]
        public string[] Handles { get; set; }

        [JsonProperty("productNames")]
        public string[] ProductNames { get; set; }
    }
}
