using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Params
{
    public class ParamClickScreenModel
    {
        public string sessionId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int clickType { get; set; }
        public int timeout { get; set; }
    }
    enum EumClickType
    {
        SingleClick,
        DoubleClick,
        LongPress
    }
}
