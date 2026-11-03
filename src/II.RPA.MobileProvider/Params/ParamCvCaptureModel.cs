using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Params
{
    public class ParamCvCaptureModel
    {
        public string sessionId { get; set; }
        [JsonProperty("filePath")]
        public string FilePath { get; set; }
        public string CVExecutionServiceURL { get; set; }
    }
}
