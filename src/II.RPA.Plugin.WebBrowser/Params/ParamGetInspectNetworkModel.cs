using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.WebBrowser.Params
{
    public class ParamGetInspectNetworkModel : ParamBase
    {
        public string type { get; set; }
        public string url { get; set; }
        public int isRegex { get; set; }
        public string filePath { get; set; }
    }
}
