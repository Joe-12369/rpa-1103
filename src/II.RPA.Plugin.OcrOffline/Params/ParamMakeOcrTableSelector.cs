using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.OcrOffline.Params
{
    public class ParamMakeOcrTableSelector : ParamBase
    {
        [JsonProperty("server")]
        public string Server { get; set; }

        [JsonProperty("imgPath")]
        public string ImgPath { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }
    }
}
