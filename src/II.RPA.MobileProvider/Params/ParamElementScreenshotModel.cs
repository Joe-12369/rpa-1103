using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Params
{
    public class ParamElementScreenshotModel : MobileElementValidationOptions
    {
        public string sessionId { get; set; }
        public string selector { get; set; }
        public string savePath { get; set; }
        public bool autoId { get; set; }
        public string fileName { get; set; }
        public double timeout { get; set; }
    }
}
