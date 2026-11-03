using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenCapture
{
    public static class DebugHelper
    {
        public static void WriteLine(string message = "")
        {
            NLogger.Default.Info(message);
            //Console.WriteLine(message);
        }
        public static void WriteError(string message = "")
        {
            NLogger.Default.Error(message);
            //Console.WriteLine(message);
        }
    }
}
