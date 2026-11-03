using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamHighlightScriptElementModel : ParamBase
    {
        [JsonProperty("webScript")]
        public string WebScript { get; set; }
        [JsonProperty("webData")]
        public string WebData { get; set; }

        /// <summary>
        /// default
        /// cssSelector
        /// xpath
        /// </summary>
        [JsonProperty("queryMode")]
        public string QueryMode { get; set; }
       
    }
}
