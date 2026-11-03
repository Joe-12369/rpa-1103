using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.WebBrowser.Params
{
    public class ParamElementExecutejsModel : ParamBase
    {
        public string selector { get; set; }
        public string scriptCode { get; set; }
        public string @params { get; set; }
        /// <summary>
        /// 浏览器类型
        /// </summary>
        [JsonProperty("browserType")]
        public string BrowserType { get; set; } = string.Empty;
    }
}
