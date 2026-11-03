using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.WebBrowser.Params
{
    public class ParamCaptureScreenshotModel : ParamBase
    {
        public string picPath { get; set; }
        public int saveToClp { get; set; }
        public int cutRange { get; set; }
        public int cover { get; set; }
        public int height { get; set; }
    }
}
