using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Core
{
    public class PipeRequest
    {
        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("params")]
        public JObject Params { get; set; }
        [JsonProperty("codeId")]
        public string CodeId { get; set; }

        [JsonProperty("options")]
        public JObject Options { get; set; }



    }
}
