using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Params
{
    public class ParamAnalogInputModel
    {
        public string sessionId;
        public string text;
        public int timeout;
        public int isEnter { get; set; } = 0;//0-不回车 1-回车
    }
}
