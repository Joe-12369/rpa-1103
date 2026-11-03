using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
   public class ParamElementScreenshotModel : ParamBase
    {
        public string savePath { get; set; }
        public string cvExecutionServiceURL { get; set; } = "";
        public bool isUseVerifyCode { get; set; } = true;
    }
}
