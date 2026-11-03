using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.WebBrowser.Params
{
    public class ParamStartConfigModel : ParamBase
    {
        public string jsPath { get; set; }
        public string config { get; set; }
        public string url { get; set; }
        public string browserType { get; set; }
    }
}
