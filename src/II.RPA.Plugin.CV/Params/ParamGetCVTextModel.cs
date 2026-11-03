using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.CV.Params
{
    public class ParamGetCVTextModel
    {
        public string selector { get; set; }
        public string cvserver { get; set; }
        public int timeout { get; set; } = 10000;
        public string cvExecutionServiceURL { get; set; }
    }
}
