using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.OcrOffline.Params
{
    public class ParamGetOcrTableDataBySelector : ParamBase
    {
        [JsonProperty("server")]
        public string Server { get; set; }

        [JsonProperty("isFromSelector")]
        public bool IsFromSelector { get; set; }

        [JsonProperty("selector")]
        public object Selector2 { get; set; }
       
        [JsonIgnore]
        public string Selector
        {
            get
            {
                return Selector2.ToJson();
            }
        }

        [JsonProperty("token")]
        public string Token { get; set; }
    }
}
