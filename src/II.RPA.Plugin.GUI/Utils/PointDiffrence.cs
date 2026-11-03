using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Utils
{
    class PointDiffrence
    {
        public static bool IsSamePoint(System.Drawing.Point PointA, System.Drawing.Point PointB)
        {
            return Math.Abs(PointA.X - PointB.X) + Math.Abs(PointA.Y - PointB.Y) <= 5;
        }
    }
}
