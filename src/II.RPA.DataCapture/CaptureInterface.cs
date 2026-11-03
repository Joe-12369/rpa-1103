using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using II.RPA.WinAutomation.CommonUtility;
using MSHTML;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.DataCapture
{
    //Test 123456
    public class CaptureInterface
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetSystemMetrics(int nIndex);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr WindowFromPoint(int xPoint, int yPoint);

        private static ILogger logger = Logger.OfContext();

        public static string IEInitSetting(string strJSFile, int nCode1, int nCode2)
        {
            if (File.Exists(strJSFile))
            {
                PublicConfig.LogTrace(string.Format("StartConfig JS:{0}", strJSFile));
            }
            else
            {
                MessageBox.Show(string.Format("JS文件不存在:{0}", strJSFile), string.Format("警告"),
                                    MessageBoxButtons.OK, MessageBoxIcon.None,
                                    MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                JObject obj = new JObject();
                obj["retCode"] = "ERROR";
                obj["code"] = nCode1;
                obj["info"] = string.Format("JS文件不存在:{0}", strJSFile);
                return obj.ToString();
            }

            int nWidth = GetSystemMetrics(0);  //屏幕宽度    
            int nHeight = GetSystemMetrics(1); //屏幕高度
            PublicConfig.gIntPtrDoc = WindowFromPoint(nWidth / 2, nHeight / 2);
            ResizeWebBrowser nWebBrowser = new ResizeWebBrowser();
            bool bRet1 = nWebBrowser.GetResizedUpIEExecutorDocument();
            if (bRet1 == false)
            {
                return PublicConfig.gstrCapture;
            }

            bool bRet2 = nWebBrowser.ResizeToTopWindow();
            if (bRet2 == false)
            {
                MessageBox.Show(string.Format("{0}", "采集页加载显示有误，请确保该目标采集页为当前屏幕窗体!"), string.Format("警告"),
                                    MessageBoxButtons.OK, MessageBoxIcon.None,
                                    MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                JObject obj = new JObject();
                obj["retCode"] = "ERROR";
                obj["code"] = nCode2;
                obj["info"] = string.Format("{0}", "采集页加载显示有误，请确保该目标采集页为当前屏幕窗体!");
                return obj.ToString();
            }

            return string.Format("");
        }
        public static void IEEndProcess(int nCode)
        {
            int nIter = 0;
            do
            {
                if (PublicConfig.gbExecBeginAnd != 1)
                    break;

                nIter++;
                if (nIter > 30)
                {
                    PublicConfig.LogTrace(string.Format("Thread Not Exit {0}", nCode));
                    nIter = 0;
                }

                Thread.Sleep(100);
            } while (true);

            PublicConfig.RemoveIEDocAndListener();

            SetStartIEBrowser();

            ResizeWebBrowser nWebBrowser = new ResizeWebBrowser();
            nWebBrowser.ResizeIEFrameToTopWindowMax();

            CheckRet();
        }
        public static string StartConfig(string strJSFile, string strlistDesc, string strUrl = "www.baidu.com")
        {
            logger.LogDebug("Interface StartConfig");
            PublicConfig.gformCapture = new DataCaptureForm(1, strUrl);
            PublicConfig.gIntPtrDoc = IntPtr.Zero;
            PublicConfig.gIntPtrIEFrame = IntPtr.Zero;

            string strRetSetting = IEInitSetting(strJSFile, 14, 28);
            if (strRetSetting != string.Format(""))
            {
                PublicConfig.gIntPtrDoc = IntPtr.Zero;
                PublicConfig.gIntPtrIEFrame = IntPtr.Zero;
                PublicConfig.gformCapture = null;

                JObject objRet = JObject.Parse(strRetSetting);
                if (objRet["retCode"].ToString() == "ERROR")
                {
                    throw new Exception(objRet["info"].ToString());
                }

                return objRet["info"].ToString(Newtonsoft.Json.Formatting.None);
            }

            PublicConfig.gbRulesGet = false;
            PublicConfig.gstrCapture = string.Format("");
            PublicConfig.gbCloseForm = false;
            PublicConfig.gstrJSFileName = strJSFile;
            PublicConfig.gstrListDesc = strlistDesc;
            PublicConfig.gformCapture._typeOperation = 1;
            PublicConfig.gformCapture.Opacity = 1.0;
            PublicConfig.gformCapture.ShowDialog();

            PublicConfig.LogTrace(string.Format("StartConfig Ret:{0}", PublicConfig.gstrCapture));

            PublicConfig.gbExecJS = false;
            PublicConfig.gbRulesGet = false;

            IEEndProcess(1);

            PublicConfig.gIntPtrDoc = IntPtr.Zero;
            PublicConfig.gIntPtrIEFrame = IntPtr.Zero;

            string strRet = string.Format("{0}", PublicConfig.gstrCapture);
            PublicConfig.gstrCapture = string.Format("");
            if (strRet != "")
            {
                JObject objRet = JObject.Parse(strRet);
                if (objRet["retCode"].ToString() == "ERROR")
                {
                    throw new Exception(objRet["info"].ToString());
                }

                return objRet["info"].ToString(Newtonsoft.Json.Formatting.None);
            }

            return strRet;
        }

        public static string StartCapture(string strRule, string strJSFile, string strUrl = "www.baidu.com")
        {
            logger.LogDebug("Interface StartCapture");
            PublicConfig.gformCapture = new DataCaptureForm(2, strUrl);
            PublicConfig.gIntPtrDoc = IntPtr.Zero;
            PublicConfig.gIntPtrIEFrame = IntPtr.Zero;

            string strRetSetting = IEInitSetting(strJSFile, 14, 28);
            if (strRetSetting != string.Format(""))
            {
                PublicConfig.gIntPtrDoc = IntPtr.Zero;
                PublicConfig.gIntPtrIEFrame = IntPtr.Zero;
                PublicConfig.gformCapture = null;
                JObject objRet = JObject.Parse(strRetSetting);
                if (objRet["retCode"].ToString() == "ERROR")
                {
                    throw new Exception(objRet["info"].ToString());
                }

                return objRet["info"].ToString();
            }

            PublicConfig.gbRulesGet = false;
            PublicConfig.gstrCapture = string.Format("");
            PublicConfig.gbCloseForm = false;
            PublicConfig.gstrJSFileName = strJSFile;
            PublicConfig.gformCapture._typeOperation = 2;
            PublicConfig.gformCapture._strRule = strRule;
            PublicConfig.gformCapture.Opacity = 1.0;
            PublicConfig.gformCapture.ShowDialog();

            PublicConfig.LogTrace(string.Format("StartCapture Ret:{0}", PublicConfig.gstrCapture));

            PublicConfig.gbExecJS = false;
            PublicConfig.gbRulesGet = false;

            IEEndProcess(2);

            PublicConfig.gIntPtrDoc = IntPtr.Zero;
            PublicConfig.gIntPtrIEFrame = IntPtr.Zero;

            string strRet = string.Format("{0}", PublicConfig.gstrCapture);
            PublicConfig.gstrCapture = string.Format("");
            if (strRet != "")
            {
                JObject objRet = JObject.Parse(strRet);
                if (objRet["retCode"].ToString() == "ERROR")
                {
                    throw new Exception(objRet["info"].ToString());
                }

                return objRet["info"].ToString();
            }

            return strRet;
        }
        public static void CheckRet()
        {
            if (PublicConfig.gstrCapture == "")
            {
                JObject obj = new JObject();
                obj["retCode"] = "ERROR";
                obj["code"] = 20;
                obj["info"] = string.Format("{0}", "人为中止采集配置过程或者采集配置过程报错中止！");
                PublicConfig.gstrCapture = obj.ToString();
            }
        }
        public static void SetStartIEBrowser()
        {
            TransferEngineMessage nTranferToExecutor = new TransferEngineMessage();
            nTranferToExecutor.IEFromEngineToExecutorMessageByStartBrowser();
        }
    }
}
