using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Params
{
    public class ParamGetSimilarElementInfoModel : ParamBase
    {
        public string sessionId { get; set; }
        public string selector { get; set; }
        public string filepath { get; set; }
        public int timeout { get; set; }
        public int isSimilar { get; set; } = 0;
    }
}
