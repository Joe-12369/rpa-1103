using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Params
{
    public class ParamCvClickModel : ParamClickScreenModel
    {
        public string selector { get; set; }
        public string CVExecutionServiceURL { get; set; }
        public int offsetX { get; set; } = 0;
        public int offsetY { get; set; } = 0;
    }
}
