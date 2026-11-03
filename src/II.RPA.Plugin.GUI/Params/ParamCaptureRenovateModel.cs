using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamCaptureRenovateModel : ParamBase
    {
        public string filepath { get; set; }
        public bool isCVCapture { get; set; }
        public string cvServer { get; set; }
        public int InspectStatus { get; set; }
        public string selector { get; set; }
        public bool isRenovate { get; set; } = true;
    }
}
