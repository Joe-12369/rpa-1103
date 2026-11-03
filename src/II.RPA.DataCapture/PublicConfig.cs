using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MSHTML;
using SHDocVw;
using System.Threading;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Newtonsoft.Json;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.DataCapture
{
    //Test 654321
    class PublicConfig
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetSystemMetrics(int nIndex);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr WindowFromPoint(int xPoint, int yPoint);
        [ComImport, Guid("6d5140c1-7436-11ce-8034-00aa006009fa"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IServiceProvider
        {
            void QueryService(ref Guid guidService,
                ref Guid riid,
                [MarshalAs(UnmanagedType.Interface)] out object ppvObject);
        }
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int RegisterWindowMessage(string message);
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessageTimeout(IntPtr hWnd, int uMsg, IntPtr wParam, IntPtr lParam, int fuFlags, int uTimeout, out UIntPtr lpdwResult);
        [DllImport("oleacc.dll", CharSet = CharSet.Auto, PreserveSig = false)]
        [return: MarshalAs(UnmanagedType.Interface)]
        public static extern object ObjectFromLresult(UIntPtr lResult, [MarshalAs(UnmanagedType.LPStruct)] Guid refiid, IntPtr wParam);
        [DllImport("oleacc.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        public static extern Int32 ObjectFromLresult(Int32 lResult, ref Guid riid, Int32 wParam, out IServiceProvider ppvObject);
        [DllImport("oleacc.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        public static extern Int32 ObjectFromLresult(Int32 lResult, ref Guid riid, Int32 wParam, out IHTMLDocument2 ppvObject);
        [DllImport("OLEACC.DLL", CharSet = CharSet.Auto)]
        public static extern int ObjectFromLresult(
        int lResult,
        ref System.Guid riid,
        int wParam,
        [System.Runtime.InteropServices.MarshalAs(UnmanagedType.Interface), System.Runtime.InteropServices.In, System.Runtime.InteropServices.Out] ref System.Object ppvObject
        //注意这个函数ObjectFromLresult的声明.
        );

        private static ILogger logger = Logger.OfContext();
        public static IHTMLDocument2 ghtmlUpDoc2 = null;
        public static HTMLDocument ghtmlUpDoc = null;
        public static IHTMLDocument2 ghtmlDownDoc2 = null;
        public static HTMLDocument ghtmlDownDoc = null;

        public static DocumentEventListenerUp gdocEventListenerUp = null;
        public static DocumentEventListenerDown gdocEventListenerDown = null;
        public static Mutex glogMutex = new Mutex();

        public static List<string> glistStringEventUp = new List<string>();
        public static List<string> glistStringEventDown = new List<string>();

        public static Mutex gUpMutex = new Mutex();
        public static Mutex gUpMutex2 = new Mutex();
        public static Mutex gUpMutexInit = new Mutex();
        public static Mutex gDownMutex = new Mutex();
        public static Mutex gDownMutex2 = new Mutex();

        public static string gstrCapture = string.Format("");
        public static string gstrJSFileName = string.Format("");
        public static string gstrListDesc = string.Format("");

        public static bool gbCloseForm = false;

        public static DataCaptureForm gformCapture = null;
        public static bool gbRefresh = false;
        public static readonly string gstrShiZaiEventIn = string.Format("SHI_ZAI_EVENT_IN");
        public static readonly string gstrShiZaiEventOut = string.Format("SHI_ZAI_EVENT_OUT");

        public static bool gbExecJS = true;
        public static int gbExecBeginAnd = 2;

        public static uint WM_WINFORM_CLOSE = 0x0401;
        public static IntPtr gIntPtrWnd = IntPtr.Zero;
        public static bool gbRulesGet = false;
        public static IntPtr gIntPtrDoc = IntPtr.Zero;
        public static IntPtr gIntPtrIEFrame = IntPtr.Zero;

        public static void RemoveIEDocAndListener()
        {
            PublicConfig.gDownMutex.WaitOne();
            PublicConfig.gDownMutex2.WaitOne();
            try
            {
                if (PublicConfig.ghtmlDownDoc != null && PublicConfig.gdocEventListenerDown != null)
                {
                    PublicConfig.LogTrace(string.Format("Configure removeEventListener 11"));
                    PublicConfig.ghtmlDownDoc.removeEventListener(PublicConfig.gstrShiZaiEventOut, PublicConfig.gdocEventListenerDown, false);
                }
            }
            catch (Exception EX)
            {
                PublicConfig.LogTrace(string.Format("Configure removeEventListener Faild 11"));
            }

            PublicConfig.ghtmlDownDoc2 = null;
            PublicConfig.ghtmlDownDoc = null;
            PublicConfig.gdocEventListenerDown = null;
            PublicConfig.gDownMutex2.ReleaseMutex();
            PublicConfig.gDownMutex.ReleaseMutex();

            PublicConfig.LogTrace("Document Execute NULL 21");

            PublicConfig.gUpMutexInit.WaitOne();
            try
            {
                if (PublicConfig.ghtmlUpDoc != null && PublicConfig.gdocEventListenerUp != null)
                {
                    PublicConfig.LogTrace(string.Format("Execute removeEventListener 11"));
                    PublicConfig.ghtmlUpDoc.removeEventListener(PublicConfig.gstrShiZaiEventOut, PublicConfig.gdocEventListenerUp, false);
                }
            }
            catch (Exception EX)
            {
                PublicConfig.LogTrace(string.Format("Execute removeEventListener Faild 11"));
            }

            PublicConfig.ghtmlUpDoc2 = null;
            PublicConfig.ghtmlUpDoc = null;
            PublicConfig.gdocEventListenerUp = null;
            PublicConfig.gUpMutexInit.ReleaseMutex();

            PublicConfig.LogTrace("Document Execute NULL 22");
        }
        public static void RemoveIEExecuteDocAndListener(int nType)
        {
            try
            {
                if (PublicConfig.ghtmlUpDoc != null && PublicConfig.gdocEventListenerUp != null)
                {
                    PublicConfig.LogTrace(string.Format("Execute removeEventListener {0}", nType));
                    PublicConfig.ghtmlUpDoc.removeEventListener(PublicConfig.gstrShiZaiEventOut, PublicConfig.gdocEventListenerUp, false);
                }
            }
            catch (Exception EX)
            {
                PublicConfig.LogTrace(string.Format("Execute removeEventListener Faild 4"));
            }
        }
        public static bool GetReadyStateComplete(IHTMLDocument2 htmlDoc2)
        {
            do
            {
                try
                {
                    if (htmlDoc2.readyState == "complete" || htmlDoc2.readyState == "interactive")
                    {
                        break;
                    }
                }
                catch (Exception EX)
                {
                    //ReturnErrorInfo(string.Format("采集页加载显示有误，请确保该目标采集页存在，未关闭!"), 18);
                    return false;
                }

                Thread.Sleep(100);
            } while (true);

            return true;
        }
        public static void LogTrace(string strInfo)
        {
            PublicConfig.glogMutex.WaitOne();
            logger.LogDebug(strInfo);
            PublicConfig.glogMutex.ReleaseMutex();
        }
        public static void ReturnErrorInfo(string strInfo, int nCode)
        {
            JObject obj = new JObject();
            obj["retCode"] = "ERROR";
            obj["code"] = nCode;
            obj["info"] = string.Format("{0}", strInfo);
            PublicConfig.gstrCapture = JsonConvert.SerializeObject(obj);
            PublicConfig.gbExecJS = false;
            PublicConfig.gbExecBeginAnd = 2;
            PostMessage(PublicConfig.gIntPtrWnd, PublicConfig.WM_WINFORM_CLOSE, (IntPtr)0, (IntPtr)0);
            if (nCode == 12 || nCode == 11 || nCode == 2 || nCode == 1)
            {
                MessageBox.Show(string.Format("{0}", strInfo), string.Format("警告"),
                    MessageBoxButtons.OK, MessageBoxIcon.None,
                    MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
            }
        }

        public static bool GetDocument2(out IHTMLDocument2 htmlDoc)
        {
            const int SMTO_ABORTIFHUNG = 0x2;
            Guid IID_IHTMLDocument2 = typeof(IHTMLDocument2).GUID;
            int nIter = 0;
            htmlDoc = null;
            do
            {
                if (nIter >= 100)
                    break;

                try
                {
                    int nWidth = GetSystemMetrics(0);  //屏幕宽度    
                    int nHeight = GetSystemMetrics(1); //屏幕高度
                    PublicConfig.gIntPtrDoc = WindowFromPoint(nWidth / 2, Convert.ToInt32((double)nHeight * 0.25));
                    IntPtr hTarget = PublicConfig.gIntPtrDoc;
                    int nMsg = RegisterWindowMessage("WM_HTML_GETOBJECT");
                    UIntPtr lRes = UIntPtr.Zero;
                    SendMessageTimeout(hTarget, nMsg, new IntPtr(), new IntPtr(), SMTO_ABORTIFHUNG, 500, out lRes);
                    IServiceProvider pro;
                    if (!(bool)(lRes.ToUInt32() == 0))
                    {
                        Guid IID_IWebBrowser2 = typeof(SHDocVw.IWebBrowser2).GUID;
                        Guid IID_IServiceProvider = typeof(IServiceProvider).GUID;
                        Guid SID_SWebBrowserApp = typeof(SHDocVw.IWebBrowserApp).GUID;
                        int hr = ObjectFromLresult((int)lRes.ToUInt32(), ref IID_IServiceProvider, 0, out pro);
                        if (pro != null)
                        {
                            object objIWebBrowser2;
                            pro.QueryService(ref SID_SWebBrowserApp, ref IID_IWebBrowser2, out objIWebBrowser2);
                            IWebBrowser2 web = objIWebBrowser2 as IWebBrowser2;
                            //do
                            //{
                            //    if (web.Busy == false)
                            //    {
                            //        break;
                            //    }

                            //    Thread.Sleep(100);
                            //} while (true);

                            htmlDoc = web.Document;
                            if (htmlDoc != null)
                            {
                                return true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                }

                Thread.Sleep(30);

                nIter++;
            } while (true);

            PublicConfig.LogTrace(string.Format("Document WebBrowser Get {0}", nIter));
            return false;
        }

        public static bool GetDocument2(int x, int y, out IHTMLDocument2 htmlDoc)
        {
            const int SMTO_ABORTIFHUNG = 0x2;
            Guid IID_IHTMLDocument2 = typeof(IHTMLDocument2).GUID;
            int nIter = 0;
            htmlDoc = null;
            do
            {
                if (nIter >= 100)
                    break;

                try
                {
                    IntPtr hTarget = WindowFromPoint(x, y);
                    int nMsg = RegisterWindowMessage("WM_HTML_GETOBJECT");
                    UIntPtr lRes = UIntPtr.Zero;
                    SendMessageTimeout(hTarget, nMsg, new IntPtr(), new IntPtr(), SMTO_ABORTIFHUNG, 500, out lRes);
                    if (!(bool)(lRes.ToUInt32() == 0))
                    {
                        int hr = ObjectFromLresult((int)lRes.ToUInt32(), ref IID_IHTMLDocument2, 0, out htmlDoc);
                        if (htmlDoc != null)
                        {
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {

                }

                Thread.Sleep(30);

                nIter++;
            } while (true);

            PublicConfig.LogTrace(string.Format("Document WebBrowser Get {0}", nIter));
            return false;
        }
    }
}
