using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Params
{
    public class ParamClickScreenByPointModel
    {
        public int screenX { get; set; }
        public int screenY { get; set; }
        public string sessionId { get; set; }
        ///参见EumClickType
        public int clickType { get; set; }
        public int offsetX { get; set; }
        public int offsetY { get; set; }
        public int timeout { get; set; }
        public bool isRoundClick { get; set; }
        public int stayTime { get; set; }
        public int clickTimes { get; set; }
        public int interval { get; set; }
    }
}
