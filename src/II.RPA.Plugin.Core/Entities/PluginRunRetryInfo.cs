using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Core.Entities
{
    public class PluginRunRetryInfo
    {
        [JsonProperty("totaltimes")]
        public int TotalTimes { get; set; }
        [JsonProperty("remaintimes")]
        public int RemainTimes { get; set; }
        [JsonProperty("interval")]
        public int Interval { get; set; }
        [JsonProperty("timeout")]
        public int TimeOut { get; set; }
    }
}
