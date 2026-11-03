using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Params
{
    public class ParamGetElementInfoModel
    {
        public string sessionId { get; set; }
        public string filepath { get; set; }
        public int timeout { get; set; }
        public string isSimilar { get; set; } = "0";
    }
}
