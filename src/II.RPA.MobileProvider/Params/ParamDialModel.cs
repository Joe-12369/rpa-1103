using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Params
{
    public class ParamDialModel
    {
        public string sessionId { get; set; }
        public string diaNumber { get; set; }
        public int timeout { get; set; }
        public string mobileBrand { get; set; }//手机品牌
    }
}
