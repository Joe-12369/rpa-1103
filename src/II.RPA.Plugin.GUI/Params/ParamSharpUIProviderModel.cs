using II.RPA.Plugin.GUI.UIProviders.Tips;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamSharpUIProviderModel : ParamBase
    {
        [JsonProperty("type")]
        public string ActionType
        {
            get;
            set;
        }
        [JsonProperty("title")]
        public string Title
        {
            get;
            set;
        }
        [JsonProperty("content")]
        public string Content
        {
            get;
            set;
        }
        [JsonProperty("level")]
        public TipLevel Level
        {
            get;
            set;
        }
    }
}
