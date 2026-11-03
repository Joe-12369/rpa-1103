using Microsoft.Win32;
using MSHTML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using SHDocVw;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.DataCapture
{
    //654321
    public class JavaScriptExecutor
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetSystemMetrics(int nIndex);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr WindowFromPoint(int xPoint, int yPoint);
        //设置窗口位置和大小
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndlnsertAfter, int X, int Y, int cx, int cy, uint Flags);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool AttachThreadInput(int idAttach, int idAttachTo, int fAttach);

        [ComImport, Guid("6d5140c1-7436-11ce-8034-00aa006009fa"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IServiceProvider
        {
            void QueryService(ref Guid guidService,
                ref Guid riid,
                [MarshalAs(UnmanagedType.Interface)] out object ppvObject);
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int LoadLibrary(string lpLibFileName);
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int RegisterWindowMessage(string message);
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessageTimeout(IntPtr hWnd, int uMsg, IntPtr wParam, IntPtr lParam, int fuFlags, int uTimeout, out UIntPtr lpdwResult);
        [DllImport("oleacc.dll", CharSet = CharSet.Auto, PreserveSig = false)]
        [return: MarshalAs(UnmanagedType.Interface)]
        public static extern object ObjectFromLresult(UIntPtr lResult, [MarshalAs(UnmanagedType.LPStruct)] Guid refiid, IntPtr wParam);
        [DllImport("oleacc.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        public static extern Int32 ObjectFromLresult(Int32 lResult, ref Guid riid, Int32 wParam, out IHTMLDocument2 ppvObject);
        [DllImport("oleacc.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        public static extern Int32 ObjectFromLresult(Int32 lResult, ref Guid riid, Int32 wParam, out IServiceProvider ppvObject);
        [DllImport("OLEACC.DLL", CharSet = CharSet.Auto)]
        public static extern int ObjectFromLresult(
        int lResult,
        ref System.Guid riid,
        int wParam,
        [System.Runtime.InteropServices.MarshalAs(UnmanagedType.Interface), System.Runtime.InteropServices.In, System.Runtime.InteropServices.Out] ref System.Object ppvObject
        //注意这个函数ObjectFromLresult的声明.
        );

        private static ILogger logger = Logger.OfContext();
        public const int SMTO_ABORTIFHUNG = 0x2;
        public delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);
        public static string sTitle;
        public static IntPtr sHwnd;
        public static List<IntPtr> sArrayWnd = new List<IntPtr>();

        internal const int GW_CHILD = 0x5;
        internal byte[] RIID = new byte[] { 32, 197, 111, 98, 30, 164, 207, 17, 167, 49, 0, 160, 201, 8, 38, 55 };

        public const int S_OK = 0;

        public const int SW_HIDE = 0;
        public const int SW_SHOWNORMAL = 1;

        public const int SW_SHOWMINIMIZED = 2;
        public const int SW_SHOWMAXIMIZED = 3;
        public const int SW_MAXIMIZE = 3;
        public const int SW_SHOWNOACTIVATE = 4;
        public const int SW_SHOW = 5;
        public const int SW_MINIMIZE = 6;
        public const int SW_SHOWMINNOACTIVE = 7;
        public const int SW_SHOWNA = 8;
        public const int SW_RESTORE = 9;

        int nProcess = 0;
        public JavaScriptExecutor()
        {
            int hInst = LoadLibrary("OLEACC.DLL");
        }
        private void RegistKey(bool isSilent)
        {
            List<RegistryKey> regKeys = new List<RegistryKey>();
            try
            {
                string path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies";
                RegistryKey reg = Registry.LocalMachine.OpenSubKey(path, true);
                regKeys.Add(reg);
                reg = reg.OpenSubKey("System", true);
                if (reg == null)
                {
                    reg = reg.CreateSubKey("System", RegistryKeyPermissionCheck.ReadWriteSubTree);
                }
                if (reg != null && reg.Name.EndsWith("System"))
                {
                    regKeys.Add(reg);
                    reg.SetValue("ConsentPromptBehaviorAdmin", isSilent ? 0 : 2, RegistryValueKind.DWord);
                }
            }
            finally
            {
                for (int i = regKeys.Count - 1; i >= 0; i--)
                {
                    regKeys[i].Close();
                }
            }
        }
        public void DoExecJavaScriptEX(string title, string url, string jscode, string input, out string result_out)
        {
            string stringOut = string.Empty;
            result_out = string.Empty;
            string uuid = Guid.NewGuid().ToString();
            PublicConfig.LogTrace("Enter uuid:" + uuid);
            nProcess = 0;
            JObject jsonRet = new JObject();
            int nWidth = GetSystemMetrics(0);  //屏幕宽度    
            int nHeight = GetSystemMetrics(1); //屏幕高度
            int nPointx = nWidth / 2;
            int nPointy = Convert.ToInt32((double)nHeight * 0.25);
            Thread thread = new Thread(() =>
            {
                Guid IID_IHTMLDocument2 = typeof(IHTMLDocument2).GUID;
                int nIter = 0;
                int nXY = 1;
                int nPoint_x = nPointx;
                int nPoint_y = nPointy;
                IHTMLDocument2 docRoot = null;
                do
                {
                    if (nIter >= 30)
                    {
                        stringOut = "ERROR100";
                        jsonRet["retCode"] = "ERROR";
                        jsonRet["info"] = string.Format(eCommonMessageType.JavaScriptExecutor_cs_ChaXunQieHuanWangYeYouWenTi.GetMessage());
                        break;
                    }

                    if (docRoot == null)
                    {
                        try
                        {
                            IntPtr hwnd = WindowFromPoint(nPoint_x, nPoint_y);
                            IntPtr hTarget = hwnd;
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

                                    docRoot = web.Document;
                                    if (docRoot != null)
                                    {

                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }

                    bool bRet = false;
                    if (docRoot == null)
                    {
                        nIter++;
                        logger.LogDebug("false");
                        Thread.Sleep(100);
                        continue;
                    }
                    else
                    {
                        PublicConfig.LogTrace("true");
                        int errCode = Marshal.GetLastWin32Error();
                        PublicConfig.LogTrace("LastError:" + errCode);

                        MSHTML.HTMLDocument htmlDoc = docRoot as MSHTML.HTMLDocument;
                        PublicConfig.LogTrace("Title is :" + htmlDoc.title);
                        if (htmlDoc == null)
                        {
                            PublicConfig.LogTrace("htmlDoc is null");
                            nIter++;
                            Thread.Sleep(100);
                            continue;
                        }
                        else
                        {

                        }

                        do
                        {
                            if (htmlDoc.readyState == "complete" || htmlDoc.readyState == "interactive")
                            {
                                break;
                            }

                            Thread.Sleep(100);
                        } while (true);

                        var head = htmlDoc.getElementsByTagName("head").Cast<HTMLHeadElement>().First();
                        IHTMLElement element_script = htmlDoc.createElement("script");
                        var script = (IHTMLScriptElement)element_script;
                        if (script != null && head != null && element_script != null)
                        {
                            element_script.id = "SHI_ZAI_SCRIPT_ELEMENT_IN";
                            script.text = string.Format("test={0}", jscode);
                            head.appendChild((IHTMLDOMNode)script);
                            Thread.Sleep(5);
                            MSHTML.IHTMLWindow2 win = htmlDoc.parentWindow as IHTMLWindow2;
                            string[] strs = input.Split(',');
                            try
                            {
                                var temp = InvokeScript(win, "test", strs);
                                bRet = true;
                                if (temp == null)
                                {
                                    stringOut = string.Empty;
                                    jsonRet["info"] = "";
                                }
                                else
                                {
                                    stringOut = temp.ToString();
                                    jsonRet["info"] = stringOut;
                                }

                                jsonRet["retCode"] = "SUCCEED";
                                break;
                            }
                            catch (Exception ex)
                            {
                                stringOut = "ERROR100";
                                jsonRet["retCode"] = "ERROR";
                                PublicConfig.LogTrace(string.Format("JS 运行失败，原因为{0}", ex.Message));
                                jsonRet["info"] = string.Format(eCommonMessageType.JavaScriptExecutor_cs_JSYunXingShiBaiYuFa.GetMessage());
                            }
                        }
                        else
                        {
                            stringOut = "ERROR100";
                            jsonRet["retCode"] = "ERROR";
                            jsonRet["info"] = string.Format(eCommonMessageType.JavaScriptExecutor_cs_ZhuRuJSCodeShi.GetMessage());
                        }

                        if (bRet == false)
                        {
                            stringOut = "ERROR100";
                            jsonRet["retCode"] = "ERROR";
                            break;
                        }
                        else
                        {
                            jsonRet["retCode"] = "SUCCEED";
                            break;
                        }
                    }
                } while (true);

                nProcess = 1;
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            while (true)
            {
                if (nProcess == 1)
                {
                    break;
                }

                Thread.Sleep(1);
            }

            result_out = jsonRet.ToString();
            PublicConfig.LogTrace(string.Format("DoExecJavaScript Leave,result is {0}", result_out));
            PublicConfig.LogTrace("Leave uuid:" + uuid);
        }

        public string DoExecJavaScript(string title, string url, string jscode, string input)
        {
            PublicConfig.LogTrace("Begin EX");

            string strOut;
            DoExecJavaScriptEX(title, url, jscode, input, out strOut);

            return strOut;
        }

        static object InvokeScript(object callee, string method, params object[] args)
        {
            return callee.GetType().InvokeMember(method,
                BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public,
                null, callee, args);
        }
    }

}

