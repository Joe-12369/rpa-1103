using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.WebBrowser.Params
{
    public class ParamCloseModel : ParamBase
    {
        [JsonProperty("isCloseAlert")]
        public bool IsCloseAlert { get; set; }
    }
}
