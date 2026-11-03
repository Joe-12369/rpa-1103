using Newtonsoft.Json;

namespace II.RPA.MobileProvider.Params
{
    public class MobileElementValidationOptions
    {
        [JsonProperty("isVerifyPage")]
        public bool IsVerifyPage { get; set; }

        [JsonProperty("isVerifyElementImage")]
        public bool IsVerifyElementImage { get; set; }
        [JsonProperty("applicationPath")]
        public string ApplicationPath { get; set; }
    }
}
