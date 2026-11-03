using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using MSHTML;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using SHDocVw;
using System.Threading;
using II.RPA.WinAutomation.CommonUtility;
using System.Diagnostics;
using II.RPA.Plugin.Core;
using System.Drawing;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Logging.Formatting;

namespace II.RPA.Plugin.IE.V2
{
    public class SwitchIEWebbrowser
    {
        //654321
        [DllImport("user32.dll", EntryPoint = "SendMessageA")]
        private static extern int SendMessage(IntPtr hwnd, int wMsg, int wParam, StringBuilder lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int EnumWindows(EnumWindowsProc ewp, int lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int nMaxCount);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder strText, int nMaxCount);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool IsWindowVisible(int hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowTextLength(int hWnd);
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
        public static extern Int32 ObjectFromLresult(Int32 lResult, ref Guid riid, Int32 wParam, out IServiceProvider ppvObject);
        [DllImport("oleacc.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        public static extern Int32 ObjectFromLresult(Int32 lResult, ref Guid riid, Int32 wParam, out IHTMLDocument2 ppvObject);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, int uMsg, int wParam, int lParam);
        [DllImport("OLEACC.DLL", CharSet = CharSet.Auto)]
        public static extern int ObjectFromLresult(
        int lResult,
        ref System.Guid riid,
        int wParam,
        [System.Runtime.InteropServices.MarshalAs(UnmanagedType.Interface), System.Runtime.InteropServices.In, System.Runtime.InteropServices.Out] ref System.Object ppvObject
        //注意这个函数ObjectFromLresult的声明.
        );
        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("00000114-0000-0000-C000-000000000046")]
        public interface IOleWindow
        {
            void GetWindow(out System.IntPtr phwnd);
            void ContextSensitiveHelp([In] bool fEnterMode);
        }
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowRect(IntPtr hWnd, out HTMLRectEX lpRect);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetTopWindow(IntPtr hWnd);

        private static ILogger logger = Logger.OfContext();
        public const int SMTO_ABORTIFHUNG = 0x2;
        public const int SW_SHOWMAXIMIZED = 0x03;
        public const int WM_GETTEXT = 0x000D;
        public Guid IID_IWebBrowser2 = typeof(SHDocVw.IWebBrowser2).GUID;
        public Guid IID_IServiceProvider = typeof(IServiceProvider).GUID;
        public Guid IID_IHTMLDocument2 = typeof(IHTMLDocument2).GUID;
        public Guid IID_IHTMLWindow2 = typeof(IHTMLWindow2).GUID;
        public Guid IID_IOleWindow = typeof(IOleWindow).GUID;
        public Guid SID_SWebBrowserApp = typeof(SHDocVw.IWebBrowserApp).GUID;
        public Guid SID_SShellBrowser = new Guid("000214E2-0000-0000-C000-000000000046");
        public delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);
        public static List<IntPtr> sArrayWnd = new List<IntPtr>();
        public JObject _jsonWnd { get; set; }
        public SwitchIEWebbrowser()
        {
        }
        public SwitchIEWebbrowser(JObject jsonWnd)
        {
            _jsonWnd = jsonWnd;
        }
        public static IntPtr sHwnd;
        public static string sTitle;
        private static bool EnumWebBrowserWindowsProc(IntPtr hWnd, int lParam)
        {
            int capacity = NativeMethods.GetWindowTextLength(hWnd) * 2 + 1;
            StringBuilder szWndowText = new StringBuilder(capacity);
            StringBuilder szClassName = new StringBuilder(256);

            GetWindowText(hWnd, szWndowText, capacity);
            GetClassName(hWnd, szClassName, 256);

            StringBuilder strClassName = szClassName;

            RegMatchTextChange.DoChangeTitleIE(ref sTitle);
            RegMatchTextChange.DoChangeBuildStringTitleIE(ref szWndowText);
            bool bBankTab = false;
            if (szWndowText.ToString() == "空白页")
            {
                bBankTab = true;
            }

            if (strClassName.ToString().ToUpper() == "IEFRAME")
            {
                ShowWindow(hWnd, SW_SHOWMAXIMIZED);
            }

            if (strClassName.ToString().ToUpper() == "TABTHUMBNAILWINDOW" && szWndowText.ToString() != "" && bBankTab == false)
            {
                string inputTtile = string.Format("{0}", szWndowText.ToString());
                if (inputTtile.CompareTo(sTitle) == 0)
                {
                    int i = 0;
                    for (; i < sArrayWnd.Count; i++)
                    {
                        if (hWnd == sArrayWnd[i])
                            break;
                    }

                    if (i == sArrayWnd.Count)
                    {
                        sHwnd = hWnd;
                        return false;
                    }
                }
                else
                {
                    try
                    {
                        string strtitleT = string.Format("{0}", sTitle);
                        //RegMatchTextChange.ChangeText(ref s_titleT);
                        //RegMatchTextChange.ChangeText(ref inputTtile);

                        string patternT = "";
                        patternT += strtitleT;
                        patternT += "";

                        if (true == Regex.IsMatch(inputTtile, patternT))
                        {
                            int i = 0;
                            for (; i < sArrayWnd.Count; i++)
                            {
                                if (hWnd == sArrayWnd[i])
                                    break;
                            }

                            if (i == sArrayWnd.Count)
                            {
                                sHwnd = hWnd;
                                return false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }

            return true;
        }
        public bool DoSwitchWebBrowser()
        {
            logger.LogDebug("Begin DoSwitchWebBrowser");
            string strTitle = string.Format("");
            if (_jsonWnd.ContainsKey("title") == true)
                strTitle = _jsonWnd["title"].ToString();
            string strUrl = string.Format("");
            if (_jsonWnd.ContainsKey("url") == true)
                strUrl = _jsonWnd["url"].ToString();

            if (strTitle.Length > 0)
            {
                RegMatchTextChange.DoChangeTitleIE(ref strTitle);
                IHTMLDocument2 htmlDoc2 = null;
                IntPtr ptrTarget = IntPtr.Zero;
                if (DoFindCurrentWebBrowser(strTitle, strUrl, ref htmlDoc2, ref ptrTarget) != false)
                {
                    DoSwitchWindow(ptrTarget);
                    return true;
                }

                int nEnumIter = 0;
                SwitchIEWebbrowser.sArrayWnd.Clear();
                do
                {
                    bool bFirstTab = false;
                    SwitchIEWebbrowser.sTitle = string.Format("{0}", strTitle);
                    SwitchIEWebbrowser.sHwnd = IntPtr.Zero;
                    logger.LogDebug("Enum Ie Window");
                    EnumWindowsProc ewp = new EnumWindowsProc(EnumWebBrowserWindowsProc);
                    EnumWindows(ewp, 0);

                    if (SwitchIEWebbrowser.sHwnd == (IntPtr)0 && SwitchIEWebbrowser.sArrayWnd.Count == 0)
                    {
                        logger.LogDebug("  DoSwitchWebBrowser SwitchIEWebbrowser.sHwnd False");
                        return false;
                    }
                    else if (SwitchIEWebbrowser.sHwnd == (IntPtr)0 && SwitchIEWebbrowser.sArrayWnd.Count > 0)
                    {
                        logger.LogDebug("  DoSwitchWebBrowser SwitchIEWebbrowser.sHwnd True");
                        SwitchIEWebbrowser.sHwnd = SwitchIEWebbrowser.sArrayWnd[0];
                        bFirstTab = true;
                    }

                    DoSwitchWindow(SwitchIEWebbrowser.sHwnd);

                    logger.LogDebug(string.Format("Ttile: {0} Url:  {1}", strTitle, strUrl));
                    IHTMLDocument2 htmlDoc21 = null;
                    if (DoFindCurrentWebBrowser(strTitle, strUrl, ref htmlDoc21, ref ptrTarget) != false || bFirstTab == true)
                    {
                        logger.LogDebug(string.Format("Same Url: {0} {1}", strUrl, nEnumIter));
                        return true;
                    }
                    else
                    {
                        logger.LogDebug(string.Format("Diffrent Url: {0} {1}", strUrl, nEnumIter));
                        SwitchIEWebbrowser.sArrayWnd.Add(SwitchIEWebbrowser.sHwnd);
                    }

                    nEnumIter++;
                    if (nEnumIter > 10)
                        break;
                    logger.LogDebug("While DoSwitchWebBrowser");
                } while (true);
            }
            else
            {
                logger.LogDebug("End DoSwitchWebBrowser True");
                return true;
            }
            logger.LogDebug("Begin DoSwitchWebBrowser False");
            return false;
        }
        public bool DoFindCurrentWebBrowser(string strTitle, string strUrl, ref IHTMLDocument2 htmlDoc2, ref IntPtr ptrTarget)
        {
            int nFindIter = 1;
            do
            {
                double dx_lx = 1.0;
                double dy_ly = 1.0;
                HTMLRectEX rectWebBrowser;
                RegMatchTextChange.DoChangeTitleIE(ref strTitle);
                IWebBrowser2 web = DoFindWebBrowser(strTitle, ref dx_lx, ref dy_ly, out rectWebBrowser, ref htmlDoc2, ref ptrTarget);

                logger.LogDebug(LogBlock.NullCheck(web, "web"));
                logger.LogDebug(LogBlock.NullCheck(htmlDoc2, "htmlDoc2"));

                if (web != null || htmlDoc2 != null)
                {
                    string inputTtile = string.Format("");
                    string strTitleT = string.Format("");
                    if (web != null)
                    {
                        try
                        {
                            if (htmlDoc2 != null)
                                htmlDoc2 = web.Document as IHTMLDocument2;
                            logger.LogDebug(LogBlock.NullCheck(htmlDoc2, "htmlDoc2"));
                            StringBuilder strTitleText = new StringBuilder(255);
                            SendMessage((IntPtr)web.HWND, WM_GETTEXT, 255, strTitleText);
                            RegMatchTextChange.DoChangeBuildStringTitleIE(ref strTitleText);
                            if (htmlDoc2.title != "")
                                inputTtile = htmlDoc2.title;
                            else
                                inputTtile = strTitleText.ToString();
                            logger.LogDebug($" inputTtile  {inputTtile}");
                            RegMatchTextChange.DoChangeTitleIE(ref inputTtile);
                            strTitleT = string.Format("{0}", strTitle);
                        }
                        catch (Exception ex)
                        {
                            htmlDoc2 = web.Document as IHTMLDocument2;
                            inputTtile = htmlDoc2.title;
                            RegMatchTextChange.DoChangeTitleIE(ref inputTtile);
                            strTitleT = string.Format("{0}", strTitle);
                        }
                    }
                    else
                    {
                        inputTtile = htmlDoc2.title;
                        RegMatchTextChange.DoChangeTitleIE(ref inputTtile);
                        strTitleT = string.Format("{0}", strTitle);
                        logger.LogDebug($" inputTtile  {inputTtile}  strTitleT  {strTitleT}");
                    }

                    string inputUrl = htmlDoc2.url;
                    logger.LogDebug(string.Format("Pattern Title {0} {1}", strTitleT, inputTtile));

                    if (strTitleT.CompareTo(inputTtile) == 0)
                    {
                        if (strUrl == "" || DoCheckURL(strUrl, inputUrl) == true)
                            return true;
                        else
                            return false;
                    }
                    else
                    {
                        try
                        {
                            //RegMatchTextChange.ChangeText(ref s_strTitle);
                            //RegMatchTextChange.ChangeText(ref inputTtile);

                            string patternT = "";
                            patternT += strTitleT;
                            patternT += "";

                            if (true == Regex.IsMatch(inputTtile, patternT))
                            {
                                if (strUrl == "" || DoCheckURL(strUrl, inputUrl) == true)
                                    return true;
                                else
                                    return false;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogDebug(string.Format("Pattern Title Is Error"));
                        }
                    }
                }
                else if (htmlDoc2 == null)
                {
                    logger.LogDebug(string.Format("Web Is NULL"));
                }

                nFindIter++;
                if (nFindIter > 1)
                    break;

                Thread.Sleep(50);
            } while (true);

            return false;
        }
        public static IntPtr FindWindowHandle(string title)
        {
            logger.LogDebug($"Find Window handle by title {title}");
            List<string> processName = new List<string>() { "iexplore", "360se" };
            foreach (var name in processName)
            {
                Process[] ps = Process.GetProcessesByName(name);
                //return ps[0].MainWindowHandle;
                foreach (Process p in ps)
                {
                    var match = Regex.Matches(p.MainWindowTitle, title, RegexOptions.IgnoreCase);
                    foreach (var item in match)
                    {
                        if (p.MainWindowTitle.IndexOf(item.ToString()) != -1)
                        {
                            return p.MainWindowHandle;
                        }
                    }
                }
            }

            return IntPtr.Zero;
        }
        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        //IMPORTANT : LPARAM  must be a pointer (InterPtr) in VS2005, otherwise an exception will be thrown
        private static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr i);
        //the callback function for the EnumChildWindows
        private delegate bool EnumWindowProc(IntPtr hWnd, IntPtr parameter);

        private string m_classname; // class name to look for

        private IntPtr m_hWnd; // HWND if found
        public IntPtr FoundHandle
        {
            get { return m_hWnd; }
        }
        public void FindWindows(IntPtr hwndParent, string classname)
        {
            m_hWnd = IntPtr.Zero;
            m_classname = classname;
            FindChildClassHwnd(hwndParent, IntPtr.Zero);
        }

        /**/
        /// <summary>
        /// Find the child window, if found m_classname will be assigned 
        /// </summary>
        /// <param name="hwndParent">parent's handle</param>
        /// <param name="lParam">the application value, nonuse</param>
        /// <returns>found or not found</returns>
        //The C++ code is that  lParam is the instance of FindWindow class , if found assign the instance's m_hWnd
        private bool FindChildClassHwnd(IntPtr hwndParent, IntPtr lParam)
        {
            EnumWindowProc childProc = new EnumWindowProc(FindChildClassHwnd);
            IntPtr hwnd = FindWindowEx(hwndParent, IntPtr.Zero, this.m_classname, string.Empty);
            if (hwnd != IntPtr.Zero)
            {
                this.m_hWnd = hwnd; // found: save it
                return false; // stop enumerating
            }
            EnumChildWindows(hwndParent, childProc, IntPtr.Zero); // recurse  redo FindChildClassHwnd
            return true;// keep looking
        }

        private static Rectangle location = new Rectangle();//IE explorer_Server Pane的框体
        public static void GetMousePosition(IntPtr windowIntPtr)
        {
            //1 获取窗口句柄 
            // IntPtr windowIntPtr = NativeMethods.WindowFromPoint(point);
            while (true)
            {
                if (windowIntPtr != IntPtr.Zero)
                {
                    //2 获取窗口位置
                    NativeMethods.RECT rect;
                    int capacity = NativeMethods.GetWindowTextLength(windowIntPtr) * 2 + 1;
                    StringBuilder lpString = new StringBuilder(capacity);
                    var title = NativeMethods.GetWindowText(windowIntPtr, lpString, lpString.Capacity);
                    rect = new NativeMethods.RECT();
                    NativeMethods.GetWindowRect(windowIntPtr, out rect);
                    logger.LogInformation($"GetMousePosition {rect}");
                    if (rect.Top > 20)// ???
                    {
                        location.X = rect.Left;
                        location.Y = rect.Top;
                        location.Width = rect.Right;
                        location.Height = rect.Bottom;
                        logger.LogDebug($"location {location}");
                        //logger.LogInformation($"元素判断偏移问题 location.X{location.X}  location.Y{ location.Y} location.Width{location.Width} location.Height{location.Height}::" + $"rect.Left{rect.Left} rect.Top{rect.Top} location.Width{location.Width} location.Height{location.Height}");
                        break;
                    }
                    windowIntPtr = GetParent(windowIntPtr);
                }
                else
                {
                    logger.LogInformation("IE 窗口不存在");
                    break;
                }
            }
        }
        [DllImport("user32.dll", EntryPoint = "GetParent", SetLastError = true)]
        internal static extern IntPtr GetParent(IntPtr hwnd);

        private static object _locker = new object();
        private static string _title = string.Empty;
        public Guid IID_IHTMLDocument = new Guid("626FC520-A41E-11CF-A731-00A0C9082637");
        public IWebBrowser2 DoFindWebBrowser(string title, ref double dx_lx, ref double dy_ly, out HTMLRectEX rect, ref IHTMLDocument2 htmlDoc2, ref IntPtr ptrTarget)
        {
            logger.LogDebug("Begin DoFindWebBrowser");
            int nWidth = GetSystemMetrics(0);  //屏幕宽度    
            int nHeight = GetSystemMetrics(1); //屏幕高度
            int nPointx = nWidth / 2;
            int nPointy = nHeight / 2;
            rect = new HTMLRectEX();
            rect.Top = 0;
            rect.Bottom = 0;
            rect.Left = 0;
            rect.Right = 0;

            int nIter = 0;
            int nPoint_x = nPointx;
            int nPoint_y = nPointy;
            int nXY = 1;
            IWebBrowser2 web = null;
            htmlDoc2 = null;
            // do
            // {
            lock (_locker)
            {
                //if (nIter >= 1)
                //    break;

                if (web == null && htmlDoc2 == null)
                {
                    try
                    {
                        logger.LogDebug("DoFindWebBrowser RegisterWindowMessage");
                        IntPtr hTarget = IntPtr.Zero;//IE内容窗格Pane
                        IntPtr parent = IntPtr.Zero;//IE整个窗体的句柄
                        if (!string.IsNullOrEmpty(title))
                        {
                            _title = title;
                            parent = FindWindowHandle(title);
                        }
                        else
                        {
                            parent = FindWindowHandle(_title);
                        }
                        if (hTarget == null || hTarget == IntPtr.Zero)
                        {
                            logger.LogDebug($" Find  Parent Windows Handle {parent}");
                            FindWindows(parent, "Internet Explorer_Server");//获取IE内容窗体句柄 IE内部的ClassName为"Internet Explorer_Server"的Pane窗格
                            hTarget = FoundHandle;
                            logger.LogDebug($" Find  FoundHandle {hTarget}");
                        }

                        GetMousePosition(hTarget);
                        rect = new HTMLRectEX();
                        //rect.Top = (int)(location.Top / NativeMethods.ScaleY);
                        rect.Top = (int)(location.Top);
                        rect.Bottom = location.Bottom;
                        //rect.Left = (int)(location.Left / NativeMethods.ScaleX);
                        rect.Left = (int)(location.Left);
                        rect.Right = location.Right;
                        logger.LogDebug($"location {rect}");
                        logger.LogDebug($" FindWindows Handle {hTarget}");

                        int nMsg = RegisterWindowMessage("WM_HTML_GETOBJECT");
                        UIntPtr lRes = UIntPtr.Zero;
                        logger.LogDebug("DoFindWebBrowser  SendMessageTimeout");
                        SendMessageTimeout(hTarget, nMsg, new IntPtr(), new IntPtr(), SMTO_ABORTIFHUNG, 500, out lRes);//获取HTML句柄//500
                        //document
                        if (!(bool)(lRes.ToUInt32() == 0))
                        {
                            int hr = ObjectFromLresult(Convert.ToInt32(lRes.ToUInt32()), ref IID_IHTMLDocument, 0, out htmlDoc2);//获取document htmlDoc2
                            if (htmlDoc2 == null)
                            {
                                logger.LogDebug("htmlDoc2  is null");
                            }
                        }
                        IServiceProvider pro;
                        if (!(bool)(lRes.ToUInt32() == 0))
                        {
                            logger.LogDebug("DoFindWebBrowser ObjectFromLresult");
                            int hr = ObjectFromLresult((int)lRes.ToUInt32(), ref IID_IServiceProvider, 0, out pro);//获取方法库？
                            //if (pro != null)
                            {
                                object objIWebBrowser2;
                                logger.LogDebug("DoFindWebBrowser QueryService");
                                try
                                {
                                    //弹窗IE 不存在 browser
                                    pro.QueryService(ref SID_SWebBrowserApp, ref IID_IWebBrowser2, out objIWebBrowser2);
                                    web = objIWebBrowser2 as IWebBrowser2;
                                }
                                catch (Exception)
                                {
                                }
                                object objHTMLWin2 = null;
                                Guid guidHTML2 = IID_IHTMLWindow2;
                                logger.LogDebug("DoFindWebBrowser QueryService");
                                pro.QueryService(ref guidHTML2, ref guidHTML2, out objHTMLWin2);
                                IHTMLWindow2 oIEWin2 = objHTMLWin2 as IHTMLWindow2;
                                if (oIEWin2 != null)
                                {
                                    htmlDoc2 = oIEWin2.document;
                                    IHTMLScreen2 spScreen2 = oIEWin2.screen as IHTMLScreen2;
                                    if (spScreen2 != null)
                                    {
                                        long dx = 0, dy = 0, lx = 0, ly = 0;

                                        dx = spScreen2.deviceXDPI;
                                        dy = spScreen2.deviceYDPI;
                                        lx = spScreen2.logicalXDPI;
                                        ly = spScreen2.logicalYDPI;

                                        dy_ly = (double)dy / (double)ly;
                                        dx_lx = (double)dx / (double)lx;
                                        //logger.LogInformation($"_dx_lx:{dx_lx} _dy_ly:{dy_ly} 行号 473 FindBrowser");
                                    }
                                }

                                ptrTarget = hTarget;
                                return web;
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        ///获取处于屏幕中心的IE弹窗Pane
                        Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"DoFindWebBrowser Error");

                        web = null;
                        IntPtr hTarget = WindowFromPoint(nPoint_x, nPoint_y);
                        GetWindowRect(hTarget, out rect);

                        int nMsg = RegisterWindowMessage("WM_HTML_GETOBJECT");
                        UIntPtr lRes = UIntPtr.Zero;
                        logger.LogDebug("DoFindWebBrowser  SendMessageTimeout1");
                        SendMessageTimeout(hTarget, nMsg, new IntPtr(), new IntPtr(), SMTO_ABORTIFHUNG, 500, out lRes);//500
                        if (!(bool)(lRes.ToUInt32() == 0))
                        {
                            int hr = ObjectFromLresult((int)lRes.ToUInt32(), ref IID_IHTMLDocument2, 0, out htmlDoc2);
                            if (htmlDoc2 != null)
                            {
                                IHTMLWindow2 htmlWin2 = htmlDoc2.parentWindow as IHTMLWindow2;
                                if (htmlWin2 != null)
                                {
                                    IServiceProvider pro = htmlWin2 as IServiceProvider;
                                    try
                                    {
                                        object objHTMLWin2 = null;
                                        Guid guidHTML2 = IID_IHTMLWindow2;
                                        logger.LogDebug("DoFindWebBrowser QueryService");
                                        pro.QueryService(ref guidHTML2, ref guidHTML2, out objHTMLWin2);
                                        IHTMLWindow2 oIEWin2 = objHTMLWin2 as IHTMLWindow2;
                                        if (oIEWin2 != null)
                                        {
                                            htmlDoc2 = oIEWin2.document as IHTMLDocument2;
                                            IHTMLScreen2 spScreen2 = oIEWin2.screen as IHTMLScreen2;
                                            if (spScreen2 != null)
                                            {
                                                long dx = 0, dy = 0, lx = 0, ly = 0;

                                                dx = spScreen2.deviceXDPI;
                                                dy = spScreen2.deviceYDPI;
                                                lx = spScreen2.logicalXDPI;
                                                ly = spScreen2.logicalYDPI;

                                                dy_ly = (double)dy / (double)ly;
                                                dx_lx = (double)dx / (double)lx;
                                            }
                                        }

                                        ptrTarget = hTarget;
                                        return web;
                                    }
                                    catch (Exception ex1)
                                    {

                                    }
                                }
                            }
                        }
                    }
                }

                //if (web == null && htmlDoc2 == null)
                //{
                //    if (nXY == 1)
                //    {
                //        nPoint_y -= 25;
                //        if (nPoint_y <= 0)
                //        {
                //            nXY = 2;
                //            nPoint_x = nPointx;
                //            nPoint_y = nPointy;
                //        }
                //        title = null;
                //        Thread.Sleep(200);
                //        continue;
                //    }
                //    else if (nXY == 2)
                //    {
                //        nPoint_x -= 25;
                //        if (nPoint_x <= 0)
                //        {
                //            nXY = 3;
                //            nPoint_x = nPointx;
                //            nPoint_y = nPointy;
                //        }
                //        title = null;
                //        Thread.Sleep(200);
                //        continue;
                //    }
                //    else if (nXY == 3)
                //    {
                //        nPoint_x += 25;
                //        if (nPoint_x >= nWidth)
                //        {
                //            nXY = 4;
                //            nPoint_x = nPointx;
                //            nPoint_y = nPointy;
                //        }
                //        title = null;
                //        Thread.Sleep(200);
                //        continue;
                //    }
                //    else if (nXY == 4)
                //    {
                //        nPoint_y += 25;
                //        if (nPoint_y >= nHeight)
                //        {
                //            nXY = 5;
                //            nPoint_x = nPointx;
                //            nPoint_y = nPointy;
                //        }
                //        title = null;
                //        Thread.Sleep(200);
                //        continue;
                //    }
                //    else
                //    {
                //        nXY = 1;
                //        nPoint_x = nPointx;
                //        nPoint_y = nPointy;
                //        nIter++;
                //        title = null;
                //        Thread.Sleep(200);
                //        continue;
                //    }
                //}
            }
            //} //while (true);
            logger.LogDebug("End DoFindWebBrowser");
            return null;
        }
        public void DoSwitchWindow(IntPtr hWnd)
        {
            logger.LogDebug("Begin DoSwitchWindow");
            IntPtr hTop = GetTopWindow(IntPtr.Zero);
            int dwToID;
            int dwFromID;
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

                logger.LogDebug($"End DoSwitchWindow {hWnd}");
            }
        }
        public bool DoCheckURL(string strUrl1, string strUrl2)
        {
            logger.LogDebug(strUrl1 + " " + strUrl2);
            string csUrl1 = string.Format("{0}", strUrl1.ToLower());
            string csUrl11 = "";
            //int nPos1 = csUrl1.IndexOf('?');
            //if (nPos1 >= 0)
            //{
            //    csUrl11 = csUrl1.Substring(0, nPos1);
            //}
            //else
            {
                csUrl11 = csUrl1;
            }
            string csUrl2 = string.Format("{0}", strUrl2.ToLower());
            string csUrl21 = "";
            //int nPos2 = csUrl2.IndexOf('?');
            //if (nPos2 >= 0)
            //{
            //    csUrl21 = csUrl2.Substring(0, nPos2);
            //}
            //else
            {
                csUrl21 = csUrl2;
            }

            if (csUrl11.IndexOf("file:///") >= 0)
            {
                string csUrl11T = csUrl11.Substring(8, csUrl11.Length - 8);
                csUrl11 = csUrl11T.Replace("\\", "/");
            }
            else if (csUrl11.IndexOf("file://") >= 0)
            {
                string csUrl11T = csUrl11.Substring(7, csUrl11.Length - 7);
                csUrl11 = csUrl11T.Replace("\\", "/");
            }

            if (csUrl21.IndexOf("file:///") >= 0)
            {
                string csUrl21T = csUrl21.Substring(8, csUrl21.Length - 8);
                csUrl21 = csUrl21T.Replace("\\", "/");
            }
            else if (csUrl21.IndexOf("file://") >= 0)
            {
                string csUrl21T = csUrl21.Substring(7, csUrl21.Length - 7);
                csUrl21 = csUrl21T.Replace("\\", "/");
            }

            string csUrlT = csUrl11.Substring(csUrl11.Length - 1, 1);
            if (csUrlT != "/")
                csUrl11 += "/";

            csUrlT = csUrl21.Substring(csUrl21.Length - 1, 1);
            if (csUrlT != "/")
                csUrl21 += "/";

            logger.LogDebug(csUrl11 + " " + csUrl21);

            if (csUrl11 == csUrl21)
            {
                return true;
            }
            else
            {
                //RegMatchTextChange.ChangeText(ref csUrl11);
                //RegMatchTextChange.ChangeText(ref csUrl21);

                if (true == Regex.IsMatch(csUrl21, csUrl11))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

