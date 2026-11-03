using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Mobile.Params
{
    public class ParamElementCaptureModel
    {
        public string filepath { get; set; }
        public int timeout { get; set; }
        public string isSimilar { get; set; } = "0";
    }
}
