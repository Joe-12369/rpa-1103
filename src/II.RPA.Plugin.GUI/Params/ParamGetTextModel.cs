using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamGetTextModel : ParamBase
    {
        public int timeout { get; set; } = 10000;
        public string cvExecutionServiceURL { get; set; } = "";
    }
}
