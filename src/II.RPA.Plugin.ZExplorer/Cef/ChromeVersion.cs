using System;
using Newtonsoft.Json;

namespace II.RPA.Plugin.ZExplorer.Cef
{
    public sealed class ChromeVersion
    {
        [JsonProperty("Browser")]
        public string Browser { get; set; }

        [JsonProperty("Protocol-Version")]
        public string ProtocolVersion { get; set; }

        [JsonProperty("User-Agent")]
        public string UserAgent { get; set; }

        [JsonProperty("WebKit-Version")]
        public string WebKitVersion { get; set; }
    }
}
