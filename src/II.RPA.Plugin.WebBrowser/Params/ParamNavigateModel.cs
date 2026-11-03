using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.WebBrowser.Params
{
    public class ParamNavigateModel : ParamBase
    {
        public string url { get; set; }
        public int timeout { get; set; }
    }
}
