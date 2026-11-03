using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic.Params
{
    public class ParamDownloadModel : ParamModel
    {
        public string saveFile { get; set; }
        public string url { get; set; }
        public int id { get; set; }
    }
}
