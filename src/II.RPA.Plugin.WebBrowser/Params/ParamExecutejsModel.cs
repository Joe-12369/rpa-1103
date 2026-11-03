using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.WebBrowser.Params
{
    public class ParamExecutejsModel : ParamBase
    {
        public string webData { get; set; }
        public string title { get; set; }
        public string url { get; set; }
        public string scriptCode { get; set; }
        public string @params { get; set; }
        public string browserType { get; set; }
    }
}
