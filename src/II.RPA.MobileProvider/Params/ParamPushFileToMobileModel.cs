using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Params
{
    public class ParamPushFileToMobileModel
    {
        public string sessionId { get; set; }
        public string source { get; set; }
        public string target { get; set; }
        public double timeout { get; set; }
    }
}
