using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.OcrOffline.Params
{
    public class ParamGetTextByCV_V2Model : ParamBase
    {
        [JsonProperty("selector")]
        public string Selector { get; set; }
        [JsonProperty("server")]
        public string Server { get; set; }
        [JsonProperty("imgPath")]
        public string ImgPath { get; set; }
    }
}
