using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Params
{
    public class ParamMobileElementTextModel : MobileElementValidationOptions
    {
        public string sessionId { get; set; }
        public string selector { get; set; }
        public double timeout { get; set; }
    }
}
