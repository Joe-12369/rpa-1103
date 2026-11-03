using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Observable.States.OsStates
{
    public class ScreenInfo
    {
        public int Height { get; set; }

        public int Width { get; set; }

        public double Scale { get; set; }

        public static ScreenInfo Empty { get; } = new ScreenInfo();

        public override string ToString()
        {
            return $"{Width} x {Height} ({Scale:P})";
        }
    }
}
