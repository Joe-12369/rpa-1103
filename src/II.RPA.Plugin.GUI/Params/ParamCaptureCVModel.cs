using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamCaptureCVModel : ParamBase
    {
        [JsonProperty("filePath")]
        public string FilePath { get; set; }
    }
}
