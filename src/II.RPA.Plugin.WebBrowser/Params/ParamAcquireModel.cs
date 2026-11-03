using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.WebBrowser.Params
{
    public class ParamAcquireModel : ParamBase
    {
        public string value { get; set; }
        public string mode { get; set; }
        public int index { get; set; }
        public bool isActive { get; set; }
        public bool isMatchAll { get; set; }
    }
}
