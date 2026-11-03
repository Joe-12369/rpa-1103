using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic.Params
{
    public class ParamInspectNetworkModel : ParamModel
    {
        public string type { get; set; }
        public string url { get; set; }
        public bool isRegex { get; set; }
    }
}
