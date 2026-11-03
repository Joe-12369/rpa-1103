using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using II.RPA.Plugin.ZExplorer.Cef;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace II.RPA.Plugin.ZExplorer.Cef
{
    public sealed class TabPage
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("faviconUrl")]
        public string FaviconUrl { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("url")]
        public string PageUrl { get; set; }

        [JsonProperty("webSocketDebuggerUrl")]
        public string WebSocketDebuggerUrl { get; set; }
    }
}
