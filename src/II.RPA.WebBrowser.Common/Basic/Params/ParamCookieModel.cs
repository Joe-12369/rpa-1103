using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic.Params
{
    public class ParamCookieModel : ParamModel
    {
        public string url { get; set; }
        public string name { get; set; }
        public string domain { get; set; }
        public string value { get; set; }
    }
}
