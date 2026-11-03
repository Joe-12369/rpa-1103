using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Core.Commons
{
    public class RunContext
    {

        private static Stopwatch logStopwatch = new Stopwatch();
        public static long TimeInertval
        {
            get
            {
                long t = logStopwatch.ElapsedMilliseconds;
                logStopwatch.Restart();
                return t;
            }
        }
    }
}
