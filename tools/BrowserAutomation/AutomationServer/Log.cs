using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.BrowserAutomation
{
    internal class Log
    {
        static string logDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Browser Automation";
        static string logName = logDir + "\\server.log";
        public static void Info(string message)
        {
            if (!System.IO.Directory.Exists(logDir))
            {
                System.IO.Directory.CreateDirectory(logDir);
            }
            System.IO.File.AppendAllText(logName, $"{DateTime.Now.ToString("yyyy-MM-dd HH:MM:ss:fff")}  {message}\n");
        }
    }
}
