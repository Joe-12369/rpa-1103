using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Mobile.Params
{
    public class ParamCvCaptureModel
    {
        [JsonProperty("filePath")]
        public string FilePath { get; set; }
        public string CVExecutionServiceURL { get; set; }
    }
}
