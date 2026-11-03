using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Video2Process
{
    public class PublicVideo2Process
    {
        public static int                       gWidth = 0;
        public static int                       gHeight = 0;
        public static int                       gProcess = 0;
        public static Mutex                     gMutex = new Mutex();
        public static Mutex                     gFileNameMutex = new Mutex();
        public static string                    gVideoFilePath = string.Format("");
        public static string                    gLogFilePath = string.Format("");
        public static JArray                    gJObjectLog = null;
        public static TimeSpan                  gTSInterval = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        public static bool                      gbCapture = true;
        public static string                    gstrElementPicFileName = string.Format("");
    }
}
