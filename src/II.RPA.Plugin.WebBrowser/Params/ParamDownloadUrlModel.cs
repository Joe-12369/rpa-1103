using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.WebBrowser.Params
{
    public class ParamDownloadUrlModel : ParamBase
    {
        public string downLoadUrl { get; set; }
        public string saveFile { get; set; }
        public int timeout { get; set; } = 10000;
    }
}
