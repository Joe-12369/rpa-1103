using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.WebBrowser.Params
{
    public class ParamMultipleCloseModel : ParamBase
    {
        public string[] webDatas { get; set; }
        public string closeMode { get; set; }
        [JsonProperty("kill")]
        public int KillProcess { get; set; }
        [JsonProperty("isCloseAlert")]
        public bool IsCloseAlert { get; set; }
    }
}
