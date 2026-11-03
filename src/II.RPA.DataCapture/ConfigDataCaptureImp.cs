using II.RPA.WinAutomation.CommonUtility;
using MSHTML;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace II.RPA.DataCapture
{
    //Test 123456
    class ConfigDataCaptureImp
    {
        public bool StartConfig()
        {
            CheckIEWeb nCheck = new CheckIEWeb();
            nCheck.CheckExistIETab();
            DealExecutorJS nExecJS = new DealExecutorJS();
            nExecJS.IEExecutorJS(1);            //配置

            return true;
        }
        public bool StartCapture(string strRule)
        {
            CheckIEWeb nCheck = new CheckIEWeb();
            nCheck.CheckExistIETab();
            DealExecutorJS nExecJS = new DealExecutorJS();
            nExecJS.IEExecutorJS(0);            //采集 

            return true ;
        }
    }
}