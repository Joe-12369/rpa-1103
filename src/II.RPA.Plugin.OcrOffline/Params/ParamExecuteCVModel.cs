using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.OcrOffline.Params
{
    public class ParamExecuteCVModel : ParamBase
    {
        [JsonProperty("selector")]
        public string Selector { get; set; }
        [JsonProperty("textToFind")]
        public string TextToFind { get; set; }
        [JsonProperty("server")]
        public string Server { get; set; }
    }
}
