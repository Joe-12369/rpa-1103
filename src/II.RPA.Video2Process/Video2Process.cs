using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Video2Process
{
    public class Video2Process
    {
        public bool StartRecord(string strVideoFilePath, string strLogFilePath)
        {
            Video2ProcessImp imp = new Video2ProcessImp();
            return imp.StartRecord(strVideoFilePath, strLogFilePath);
        }
        public string RecordOnce(string strElementPicPath)
        {
            Video2ProcessImp imp = new Video2ProcessImp();
            return imp.RecordOnce(strElementPicPath);
        }
        public bool PauseRecord()
        {
            Video2ProcessImp imp = new Video2ProcessImp();
            return imp.PauseRecord();
        }
        public bool RestartRecord()
        {
            Video2ProcessImp imp = new Video2ProcessImp();
            return imp.RestartRecord();
        }
        public bool StopRecord()
        {
            Video2ProcessImp imp = new Video2ProcessImp();
            return imp.StopRecord();
        }
    }
}
