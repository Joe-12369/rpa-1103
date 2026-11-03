using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.WebBrowser.Params
{
    public class ParamDownloadModel : ParamBase
    {
        public string selector { get; set; }
        public string saveFile { get; set; }
        public string fileName { get; set; }
        public int timeout { get; set; } = 10000;
        /// <summary>
        /// 浏览器类型
        /// </summary>
        [JsonProperty("browserType")]
        public string BrowserType { get; set; } = string.Empty;
    }
}
