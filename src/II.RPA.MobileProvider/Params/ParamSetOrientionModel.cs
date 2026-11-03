using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Params
{
    public class ParamSetOrientionModel
    {
        public string sessionId { get; set; }
        /// <summary>
        ///  Portrait  --竖屏   Landscape --横屏
        /// </summary>
        public string oriention { get; set; }
        public double timeout { get; set; }
    }
}
