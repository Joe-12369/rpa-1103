using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons.MouseMotions
{
    public struct MouseRoute
    {
        public MouseRoute(
            int x,
            int y,
            int? delay = null)
        {
            Type = MouseRouteType.Delay;
            X = x;
            Y = y;
            Delay = delay;
        }

        public MouseRouteType Type { get; set; }

        public int X { get; set; }

        public int Y { get; set; }

        public int? Delay { get; set; }
    }
}