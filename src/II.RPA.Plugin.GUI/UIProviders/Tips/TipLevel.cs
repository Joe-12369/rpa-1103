using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.UIProviders.Tips
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TipLevel
    {
        [JsonProperty("info")]
        INFO = 0,
        [JsonProperty("warning")]
        WARNING = 1,
        [JsonProperty("error")]
        ERROR = 2,
        [JsonProperty("done")]
        DONE = 3,
        [JsonProperty("proceed")]
        PROCEED = 4,
    }
}
