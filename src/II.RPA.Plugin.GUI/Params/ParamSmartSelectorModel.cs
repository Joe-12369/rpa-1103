using II.RPA.Plugin.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamSmartSelectorModel : ParamBase
    {
        [JsonProperty("timeout")]
        public int Timeout { get; set; }
        public string ExecuteMode { get; set; } = "auto";
    }
}
