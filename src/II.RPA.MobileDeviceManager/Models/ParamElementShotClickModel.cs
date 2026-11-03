using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileDeviceManager.Models
{
    public class ParamElementShotClickModel
    {
        public string imgSrc { get; set; }
        public float thr { get; set; }
        public ScopeRect scopeRect { get; set; }
        public object deviceResolution { get; set; }

    }
    public class ScopeRect
    {
        public int startX { get; set; }
        public int startY { get; set; }
        public int endX { get; set; }
        public int endY { get; set; }
    }
}
