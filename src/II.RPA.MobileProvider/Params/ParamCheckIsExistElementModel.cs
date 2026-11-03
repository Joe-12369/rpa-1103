using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Params
{
    public class ParamCheckIsExistElementModel : MobileElementValidationOptions
    {
        public string sessionId { get; set; }
        public string selector { get; set; }
        public string CVExecutionServiceURL { get; set; }
        public bool isCVCapture { get; set; }
        public double timeout { get; set; }
    }
}
