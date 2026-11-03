using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Video2Process
{
    public class Video2ProcessInterface
    {
        public static bool StartRecord(string strVideoFilePath, string strLogFilePath)
        {
            Video2Process nProcess = new Video2Process();
            return nProcess.StartRecord(strVideoFilePath, strLogFilePath);
        }
        public static string RecordOnce(string strElementPicPath)
        {
            Video2Process nProcess = new Video2Process();
            return nProcess.RecordOnce(strElementPicPath);
        }
        public static bool PauseRecord()
        {
            Video2Process nProcess = new Video2Process();
            return nProcess.PauseRecord();
        }
        public static bool RestartRecord()
        {
            Video2Process nProcess = new Video2Process();
            return nProcess.RestartRecord();
        }
        public static bool StopRecord()
        {
            Video2Process nProcess = new Video2Process();
            return nProcess.StopRecord();
        }
    }
}
