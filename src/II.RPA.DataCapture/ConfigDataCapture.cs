using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.DataCapture
{
    class ConfigDataCapture
    {
        public bool StartDataConfig()
        {
            ConfigDataCaptureImp Imp = new ConfigDataCaptureImp();
            bool bRet = Imp.StartConfig();
            return bRet;
        }
        public bool StartDataCapture(string strRule)
        {
            ConfigDataCaptureImp Imp = new ConfigDataCaptureImp();
            bool bRet = Imp.StartCapture(strRule);
            return bRet;
        }
    }
}
