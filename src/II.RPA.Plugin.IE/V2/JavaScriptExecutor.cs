using Microsoft.Win32;
using MSHTML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.IE.V2
{
    //123456
    public class JavaScriptExecutor
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int EnumWindows(EnumWindowsProc ewp, int lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int nMaxCount);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder strText, int nMaxCount);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool IsWindowVisible(int hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowTextLength(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetSystemMetrics(int nIndex);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr WindowFromPoint(int xPoint, int yPoint);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowThreadProcessId(IntPtr hwnd, out int ID);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern int GetCurrentThreadId();
        //将指定窗口激活
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
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
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, int uMsg, int wParam, int lParam);
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetTopWindow(IntPtr hWnd);
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
            logger.LogDebug("Enter uuid:" + uuid);

            nProcess = 0;
            JObject jsonRet = new JObject();
            int nWidth = GetSystemMetrics(0);  //屏幕宽度    
            int nHeight = GetSystemMetrics(1); //屏幕高度
            int nPointx = nWidth / 2;
            int nPointy = nHeight / 2;
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
                    if (nIter >= 1)
                    {
                        stringOut = "ERROR100";
                        jsonRet["retCode"] = "ERROR";
                        jsonRet["info"] = string.Format(eCommonMessageType.JavaScriptExecutor_cs_ChaXunQieHuanWangYeYouWenTi.GetMessage());
                        break;
                    }

                    if (nXY == 5)
                    {
                        logger.LogDebug("ReStart 5");
                    }

                    if (docRoot == null)
                    {
                        try
                        {
                            IntPtr hTarget = WindowFromPoint(nPoint_x, nPoint_y);
                            int nMsg = RegisterWindowMessage("WM_HTML_GETOBJECT");
                            UIntPtr lRes = UIntPtr.Zero;
                            SendMessageTimeout(hTarget, nMsg, new IntPtr(), new IntPtr(), SMTO_ABORTIFHUNG, 500, out lRes);//500

                            if (!(bool)(lRes.ToUInt32() == 0))
                            {
                                int hr = ObjectFromLresult((int)lRes.ToUInt32(),
                                    ref IID_IHTMLDocument2, 0, out docRoot);

                                if (docRoot != null)
                                {
                                }
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }

                    if (docRoot == null)
                    {
                        if (nXY == 1)
                        {
                            nPoint_y -= 25;
                            if (nPoint_y <= 0)
                            {
                                nXY = 2;
                                nPoint_x = nPointx;
                                nPoint_y = nPointy;
                                logger.LogDebug("Go Left");
                            }
                            Thread.Sleep(1);
                            continue;
                        }
                        else if (nXY == 2)
                        {
                            nPoint_x -= 25;
                            if (nPoint_x <= 0)
                            {
                                nXY = 3;
                                nPoint_x = nPointx;
                                nPoint_y = nPointy;
                                logger.LogDebug("Go Right");
                            }
                            Thread.Sleep(1);
                            continue;
                        }
                        else if (nXY == 3)
                        {
                            nPoint_x += 25;
                            if (nPoint_x >= nWidth)
                            {
                                nXY = 4;
                                nPoint_x = nPointx;
                                nPoint_y = nPointy;
                                logger.LogDebug("Go Down");
                            }
                            Thread.Sleep(1);
                            continue;
                        }
                        else if (nXY == 4)
                        {
                            nPoint_y += 25;
                            if (nPoint_y >= nHeight)
                            {
                                nXY = 5;
                                nPoint_x = nPointx;
                                nPoint_y = nPointy;
                                logger.LogDebug("Go Up");
                            }
                            Thread.Sleep(1);
                            continue;
                        }
                        else
                        {
                            nXY = 1;
                            nPoint_x = nPointx;
                            nPoint_y = nPointy;
                            nIter++;
                            logger.LogDebug("Go Break");
                            Thread.Sleep(1);
                            continue;
                        }
                    }

                    bool bRet = false;
                    if (docRoot == null)
                    {
                        nXY = 1;
                        nIter++;
                        nPoint_x = nPointx;
                        nPoint_y = nPointy;
                        logger.LogDebug("false");
                        Thread.Sleep(1);
                        continue;
                    }
                    else
                    {
                        logger.LogDebug("true");

                        int errCode = Marshal.GetLastWin32Error();
                        logger.LogDebug("LastError:" + errCode);

                        MSHTML.HTMLDocument htmlDoc = docRoot as MSHTML.HTMLDocument;
                        logger.LogDebug("Title is :" + htmlDoc.title);
                        if (htmlDoc == null)
                        {
                            logger.LogDebug("htmlDoc is null");
                            nXY = 1;
                            nIter++;
                            nPoint_x = nPointx;
                            nPoint_y = nPointy;
                            Thread.Sleep(100);
                            continue;
                        }
                        else
                        {

                        }

                        var head = htmlDoc.getElementsByTagName("head").Cast<HTMLHeadElement>().First();
                        var script = (IHTMLScriptElement)htmlDoc.createElement("script");
                        if (script != null && head != null)
                        {
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
                                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "JS 运行失败");
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
            logger.LogDebug(string.Format("DoExecJavaScript Leave,result is {0}", result_out));
            logger.LogDebug("Leave uuid:" + uuid);
        }

        public string DoExecJavaScript(string title, string url, string jscode, string input)
        {
            RegMatchTextChange.DoChangeTitleIE(ref title);
            JavaScriptExecutor.sTitle = title;

            logger.LogDebug(JavaScriptExecutor.sTitle);

            JavaScriptExecutor.sHwnd = (IntPtr)0;
            nProcess = 0;

            int nWidth = GetSystemMetrics(0);  //屏幕宽度    
            int nHeight = GetSystemMetrics(1); //屏幕高度
            int nPointx = nWidth / 2;
            int nPointy = nHeight / 2;

            logger.LogDebug("Begin");

            Guid IID_IHTMLDocument2 = typeof(IHTMLDocument2).GUID;

            int nIter = 0;
            int nXY = 1;
            int nPoint_x = nPointx;
            int nPoint_y = nPointy;
            IHTMLDocument2 document = null;
            bool bCurFlag = false;
            do
            {
                if (nIter >= 1)
                    break;

                if (document == null)
                {
                    try
                    {
                        IntPtr hTarget = WindowFromPoint(nPoint_x, nPoint_y);
                        int nMsg = RegisterWindowMessage("WM_HTML_GETOBJECT");
                        UIntPtr lRes = UIntPtr.Zero;
                        SendMessageTimeout(hTarget, nMsg, new IntPtr(), new IntPtr(), SMTO_ABORTIFHUNG, 1000, out lRes);

                        if (!(bool)(lRes.ToUInt32() == 0))
                        {
                            int hr = ObjectFromLresult((int)lRes.ToUInt32(),
                                ref IID_IHTMLDocument2, 0, out document);

                            if ((document != null))
                            {

                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }

                if (document == null)
                {
                    if (nXY == 1)
                    {
                        nPoint_y -= 25;
                        if (nPoint_y <= 0)
                        {
                            nXY = 2;
                            nPoint_x = nPointx;
                            nPoint_y = nPointy;
                            logger.LogDebug("Go Left");
                        }
                        Thread.Sleep(1);
                        continue;
                    }
                    else if (nXY == 2)
                    {
                        nPoint_x -= 25;
                        if (nPoint_x <= 0)
                        {
                            nXY = 3;
                            nPoint_x = nPointx;
                            nPoint_y = nPointy;
                            logger.LogDebug("Go Right");
                        }
                        Thread.Sleep(1);
                        continue;
                    }
                    else if (nXY == 3)
                    {
                        nPoint_x += 25;
                        if (nPoint_x >= nWidth)
                        {
                            nXY = 4;
                            nPoint_x = nPointx;
                            nPoint_y = nPointy;
                            logger.LogDebug("Go Down");
                        }
                        Thread.Sleep(1);
                        continue;
                    }
                    else if (nXY == 4)
                    {
                        nPoint_y += 25;
                        if (nPoint_y >= nHeight)
                        {
                            nXY = 5;
                            nPoint_x = nPointx;
                            nPoint_y = nPointy;
                            logger.LogDebug("Go Up");
                        }
                        Thread.Sleep(1);
                        continue;
                    }
                    else
                    {
                        nXY = 1;
                        nPoint_x = nPointx;
                        nPoint_y = nPointy;
                        nIter++;
                        logger.LogDebug("Go Break");
                        Thread.Sleep(1);
                        continue;
                    }
                }
                else
                {
                    break;
                }
            } while (true);

            SwitchIEWebbrowser nSwitch = new SwitchIEWebbrowser();
            if (document != null && title != "")
            {
                IHTMLDocument2 htmlDoc2 = null;
                IntPtr pTarget = IntPtr.Zero;
                if (nSwitch.DoFindCurrentWebBrowser(JavaScriptExecutor.sTitle, url, ref htmlDoc2, ref pTarget) != false)
                {
                    bCurFlag = true;
                    nSwitch.DoSwitchWindow(pTarget);
                }
            }
            else if (document != null && title == "")
            {
                bCurFlag = true;
            }

            int nIter2 = 0;
            if (bCurFlag == false)
            {
                JavaScriptExecutor.sArrayWnd.Clear();
                do
                {
                    JavaScriptExecutor.sHwnd = IntPtr.Zero;
                    EnumWindowsProc ewp = new EnumWindowsProc(Title_EnumWindowsProc);
                    EnumWindows(ewp, 0);

                    if (JavaScriptExecutor.sHwnd == IntPtr.Zero)
                    {
                        logger.LogDebug(string.Format("DoExecJavaScript Find HWND {0} Title {1} {2}", JavaScriptExecutor.sHwnd, JavaScriptExecutor.sTitle, url));
                        nIter2 = 11;
                        break;
                    }
                    else
                    {
                        logger.LogDebug(string.Format("DoExecJavaScript Find HWND {0} Title {1} {2} And Do Switch Windows", JavaScriptExecutor.sHwnd, JavaScriptExecutor.sTitle, url));
                        nSwitch.DoSwitchWindow(JavaScriptExecutor.sHwnd);
                    }

                    logger.LogDebug(string.Format("Ttile: {0} Url:  {1}", sTitle, url));
                    IHTMLDocument2 htmlDoc2 = null;
                    IntPtr pTarget = IntPtr.Zero;
                    if (nSwitch.DoFindCurrentWebBrowser(JavaScriptExecutor.sTitle, url, ref htmlDoc2, ref pTarget) != false)
                    {
                        logger.LogDebug(string.Format("Same Url: {0} {1}", url, nIter2));
                        break;
                    }
                    else
                    {
                        logger.LogDebug(string.Format("Diffrent Url: {0} {1}", url, nIter2));
                        JavaScriptExecutor.sArrayWnd.Add(JavaScriptExecutor.sHwnd);
                    }

                    if (nIter2++ >= 10)
                        break;

                } while (true);
            }
            else
            {
                logger.LogDebug(string.Format("DoExecJavaScript Do Not Switch Windows {0}", title));
            }

            if (nIter2 >= 11)
            {
                JObject jsonRet = new JObject();
                jsonRet["retCode"] = "ERROR";
                jsonRet["info"] = string.Format(eCommonMessageType.JavaScriptExecutor_cs_ChaXunQieHuanWangYeYouWenTi1.GetMessage());
                return jsonRet.ToString();
            }

            logger.LogDebug("Begin EX");

            string strOut;
            DoExecJavaScriptEX(title, url, jscode, input, out strOut);

            return strOut;
        }
        private static bool Title_EnumWindowsProc(IntPtr hWnd, int lParam)
        {
            int capacity = GetWindowTextLength(hWnd) * 2 + 1;
            StringBuilder szWndowText = new StringBuilder(capacity);
            StringBuilder szClassName = new StringBuilder(256);

            GetWindowText(hWnd, szWndowText, capacity);
            GetClassName(hWnd, szClassName, 256);
            StringBuilder strClassName = szClassName;

            RegMatchTextChange.DoChangeTitleIE(ref JavaScriptExecutor.sTitle);
            RegMatchTextChange.DoChangeBuildStringTitleIE(ref szWndowText);

            string szText = szWndowText.ToString();
            bool bBankTab = false;
            if (szText == "空白页")
            {
                bBankTab = true;
            }

            if (strClassName.ToString().ToUpper() == "TABTHUMBNAILWINDOW" && szWndowText.ToString() != ""
                && szWndowText.ToString().CompareTo(JavaScriptExecutor.sTitle) == 0 && bBankTab == false)
            {
                int i = 0;
                for (; i < sArrayWnd.Count; i++)
                {
                    if (hWnd == sArrayWnd[i])
                        break;
                }

                if (i == sArrayWnd.Count)
                {
                    JavaScriptExecutor.sHwnd = hWnd;
                    return false;
                }
            }
            else if (strClassName.ToString().ToUpper() == "TABTHUMBNAILWINDOW" && szWndowText.ToString() != "" && bBankTab == false)
            {
                try
                {
                    string strTitleT = string.Format("{0}", JavaScriptExecutor.sTitle);
                    //RegMatchTextChange.ChangeText(ref strTitleT);
                    //RegMatchTextChange.ChangeStringBuildText(ref szWndowText);

                    string inputTitle = szWndowText.ToString();
                    string patternT = "";
                    patternT += strTitleT;
                    patternT += "";

                    if (true == Regex.IsMatch(inputTitle, patternT))
                    {
                        int i = 0;
                        for (; i < sArrayWnd.Count; i++)
                        {
                            if (hWnd == sArrayWnd[i])
                                break;
                        }

                        if (i == sArrayWnd.Count)
                        {
                            JavaScriptExecutor.sHwnd = hWnd;
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }

            return true;
        }

        public bool DoSwitchWindow(IntPtr hWnd)
        {
            int dwToID;
            int dwFromID;
            IntPtr hTop = GetTopWindow(IntPtr.Zero);
            GetWindowThreadProcessId(hWnd, out dwToID);
            GetWindowThreadProcessId(hTop, out dwFromID);
            if (dwToID != dwFromID)
            {
                IntPtr hForeWnd = (IntPtr)0;
                int dwForeID = 0;
                int dwCurID = 0;
                IntPtr hWndT = hWnd;

                hForeWnd = GetForegroundWindow();
                dwForeID = GetCurrentThreadId();
                int outid;
                dwCurID = GetWindowThreadProcessId(hForeWnd, out outid);
                AttachThreadInput(dwCurID, dwForeID, 1);
                //ShowWindow(hWndT, SW_SHOWMAXIMIZED);
                IntPtr f1 = (IntPtr)(-1);
                IntPtr f2 = (IntPtr)(-2);
                uint f3 = 1;
                uint f4 = 2;
                SetWindowPos(hWndT, f1, 0, 0, 0, 0, f3 | f4);
                SetWindowPos(hWndT, f2, 0, 0, 0, 0, f3 | f4);
                SetForegroundWindow(hWndT);
                AttachThreadInput(dwCurID, dwForeID, 0);

                Thread.Sleep(250);
            }

            return true;
        }

        static object InvokeScript(object callee, string method, params object[] args)
        {
            return callee.GetType().InvokeMember(method,
                BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public,
                null, callee, args);
        }
    }

}


