using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.OcrOffline.Params
{
    public class ParamGetOcrTableDataByImg : ParamBase
    {
        [JsonProperty("imagePath")]
        public string ImagePath { get; set; }

        [JsonProperty("server")]
        public string Server { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }
    }
}
