using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using II.RPA.WinAutomation.CommonUtility;
using System.Drawing;
using System.Diagnostics;
using MSHTML;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;


namespace II.RPA.Plugin.Core
{
    //暂时先这样 晚点再优化
    public class WindowsByClassFinder
    {
        public delegate bool EnumWindowsDelegate(IntPtr hWnd, IntPtr lparam);

        [DllImport("user32")]
        public static extern IntPtr WindowFromPoint(Point Point);//坐标
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public extern static bool EnumWindows(EnumWindowsDelegate lpEnumFunc, IntPtr lparam);


        [DllImport("User32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr windowHandle, StringBuilder stringBuilder, int nMaxCount);

        [DllImport("user32.dll", EntryPoint = "GetWindowTextLength", SetLastError = true)]
        internal static extern int GetWindowTextLength(IntPtr hwnd);

        [DllImport("user32.dll", EntryPoint = "GetParent", SetLastError = true)]
        public static extern IntPtr GetParent(IntPtr hwnd);

        /// <summary>Find the windows matching the specified class name.</summary>
        public static IEnumerable<IntPtr> WindowsMatching(string className)
        {
            return new WindowsByClassFinder(className)._result;
        }

        private WindowsByClassFinder(string className)
        {
            _className = className;
            EnumWindows(callback, IntPtr.Zero);
        }

        private bool callback(IntPtr hWnd, IntPtr lparam)
        {
            if (GetClassName(hWnd, _apiResult, _apiResult.Capacity) != 0)
            {
                if (string.CompareOrdinal(_apiResult.ToString(), _className) == 0)
                {
                    _result.Add(hWnd);
                }
            }
            return true; // Keep enumerating.
        }

        public static IEnumerable<string> WindowTitlesForClass(string className, Point point)
        {
            foreach (var windowHandle in WindowsMatchingClassName(className, point))
            {
                IntPtr intPtr = GetParent(WindowFromPoint(point));
                if (intPtr == IntPtr.Zero)
                {
                    intPtr = WindowFromPoint(point);
                }
                // Console.WriteLine("句柄打印 --windowHanle" + windowHandle + "  --intPrt" + intPtr);
                if (intPtr == windowHandle)
                {
                    int length = GetWindowTextLength(windowHandle);
                    StringBuilder sb = new StringBuilder(length + 1);
                    GetWindowText(windowHandle, sb, sb.Capacity);
                    yield return sb.ToString();
                }
            }
        }
        public static string WindowTitlesForClass(Point point)
        {
            IntPtr intPtr = GetParent(WindowFromPoint(point));
            while (true)
            {
                if (intPtr != IntPtr.Zero)
                {
                    int length = GetWindowTextLength(intPtr);
                    StringBuilder sb = new StringBuilder(length + 1);
                    GetWindowText(intPtr, sb, sb.Capacity);
                    if (!string.IsNullOrEmpty(sb.ToString()))
                        return sb.ToString();
                }
                else
                {
                    return null;
                }
                intPtr = GetParent(intPtr);
            }
        }
        public static IEnumerable<IntPtr> WindowsMatchingClassName(string className, Point point)
        {
            if (string.IsNullOrWhiteSpace(className))
                throw new ArgumentOutOfRangeException("className", className, "className can't be null or blank.");

            return WindowsMatching(className);
        }

        /// <summary>
        /// 根据窗口标题查找窗口
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public static IEnumerable<IntPtr> WindowsMatchingByTitle(string title)
        {
            var windowList = new List<IntPtr>();
            // 查找第一个匹配的窗口
            IntPtr hWnd = NativeMethods.FindWindow(null, title);
            windowList.Add(hWnd);

            if (hWnd != IntPtr.Zero)
            {
                // 继续查找相同标题的下一个窗口
                IntPtr hWndNext = IntPtr.Zero;
                while (true)
                {
                    hWndNext = NativeMethods.FindWindowEx(IntPtr.Zero, hWndNext, null, title);

                    if (hWndNext == IntPtr.Zero)
                    {
                        break; // 没有更多窗口
                    }
                    windowList.Add(hWndNext);
                }
            }
            return windowList;
        }
        private readonly Point _point;
        private readonly string _className;
        private readonly List<IntPtr> _result = new List<IntPtr>();
        private readonly StringBuilder _apiResult = new StringBuilder(1024);
    }

    public enum GetAncestorFlags
    {
        /// <summary>
        /// Retrieves the parent window. This does not include the owner, as it does with the GetParent function.
        /// </summary>
        GetParent = 1,
        /// <summary>
        /// Retrieves the root window by walking the chain of parent windows.
        /// </summary>
        GetRoot = 2,
        /// <summary>
        /// Retrieves the owned root window by walking the chain of parent and owner windows returned by GetParent.
        /// </summary>
        GetRootOwner = 3
    }
    public enum GET_DEVICE_CAPS_INDEX : uint
    {
        DRIVERVERSION,
        TECHNOLOGY = 2U,
        HORZSIZE = 4U,
        VERTSIZE = 6U,
        HORZRES = 8U,
        VERTRES = 10U,
        BITSPIXEL = 12U,
        PLANES = 14U,
        NUMBRUSHES = 16U,
        NUMPENS = 18U,
        NUMMARKERS = 20U,
        NUMFONTS = 22U,
        NUMCOLORS = 24U,
        PDEVICESIZE = 26U,
        CURVECAPS = 28U,
        LINECAPS = 30U,
        POLYGONALCAPS = 32U,
        TEXTCAPS = 34U,
        CLIPCAPS = 36U,
        RASTERCAPS = 38U,
        ASPECTX = 40U,
        ASPECTY = 42U,
        ASPECTXY = 44U,
        LOGPIXELSX = 88U,
        LOGPIXELSY = 90U,
        SIZEPALETTE = 104U,
        NUMRESERVED = 106U,
        COLORRES = 108U,
        PHYSICALWIDTH = 110U,
        PHYSICALHEIGHT,
        PHYSICALOFFSETX,
        PHYSICALOFFSETY,
        SCALINGFACTORX,
        SCALINGFACTORY,
        VREFRESH,
        DESKTOPVERTRES,
        DESKTOPHORZRES,
        BLTALIGNMENT,
        SHADEBLENDCAPS,
        COLORMGMTCAPS
    }
    public enum GET_WINDOW_CMD : uint
    {
        GW_HWNDFIRST = 0U,
        GW_HWNDLAST = 1U,
        GW_HWNDNEXT = 2U,
        GW_HWNDPREV = 3U,
        GW_OWNER = 4U,
        GW_CHILD = 5U,
        GW_ENABLEDPOPUP = 6U
    }
    [System.Security.SuppressUnmanagedCodeSecurity]
    public static class NativeMethods
    {
        [DllImport("User32", ExactSpelling = true)]
        public static extern bool IsWindowEnabled(IntPtr hWnd);
        [DllImport("Gdi32", ExactSpelling = true)]
        public static extern int GetDeviceCaps(IntPtr hdc, GET_DEVICE_CAPS_INDEX index);

        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern IntPtr GetAncestor(IntPtr hwnd, GetAncestorFlags flags);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SetFocus(IntPtr hWnd);

        public const int WM_MOUSEOVER = 0x0200;
        [DllImport("user32.dll", EntryPoint = "PostMessageA", CharSet = CharSet.Auto)]
        public static extern int PostMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutA")]
        public static extern int SendMessageTimeout(IntPtr hwnd, int msg, int wParam, int lParam, int fuFlags, int uTimeout, out int lpdwResult);

        [DllImport("user32")]
        public static extern IntPtr GetParent(IntPtr hWnd);
        [DllImport("User32")]
        public static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("User32")]
        public static extern bool CloseClipboard();

        [DllImport("User32")]
        public static extern bool EmptyClipboard();

        [DllImport("User32")]
        public static extern bool IsClipboardFormatAvailable(int format);

        [DllImport("User32")]
        public static extern IntPtr GetClipboardData(int uFormat);

        [DllImport("User32", CharSet = CharSet.Unicode)]
        public static extern IntPtr SetClipboardData(int uFormat, IntPtr hMem);
        [DllImport("User32")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        public static extern bool ClientToScreen(IntPtr hwnd, ref Point lpPoint);
        [DllImport("user32.dll")]
        public static extern bool ScreenToClient(IntPtr hwnd, ref Point lpPoint);

        private static ILogger logger = Logger.OfContext();

        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int WM_NCHITTEST = 0x0084;
        public const int HTTRANSPARENT = -1;

        #region 设置窗体置顶
        //窗体置顶
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        //取消窗体置顶
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        //不调整窗体位置
        public const uint SWP_NOMOVE = 0x0002;

        //不调整窗体大小
        public const uint SWP_NOSIZE = 0x0001;

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
        #endregion

        public static bool SetForgroundWindowByProcessId(int processId)
        {
            try
            {
                var handle = Process.GetProcessById(processId).MainWindowHandle;
                SetForegroundWindow(handle);
                return true;

            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                return false;
            }
        }
        public static string GetClassName(IntPtr hWnd)
        {
            StringBuilder sb2 = new StringBuilder(256);
            NativeMethods.GetClassName(hWnd, sb2, sb2.Capacity);
            return sb2.ToString();
        }
        /// <summary>
        /// 匹配某个子句柄的父级元素是否包含指定句柄
        /// </summary>
        /// <param name="subHandle"></param>
        /// <param name="rootHandle"></param>
        /// <returns></returns>
        public static bool MatchAncestor(IntPtr subHandle, IntPtr rootHandle)
        {
            bool result = false;
            IntPtr handle = NativeMethods.GetParent(subHandle);
            while (handle != IntPtr.Zero)
            {
                if (handle == rootHandle)
                {
                    result = true;
                    break;
                }
                else
                {
                    handle = NativeMethods.GetParent(handle);
                }
            }
            return result;
        }
        #region Win32 API


        [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint nCmd);

        [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr GetWindow(IntPtr hWnd, GET_WINDOW_CMD nCmd);
        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr ptr);
        [DllImport("gdi32.dll")]
        public static extern int GetDeviceCaps(
        IntPtr hdc, // handle to DC
        int nIndex // index of capability
        );

        [DllImport("user32.dll", EntryPoint = "ReleaseDC")]
        public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDc);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);

        public static uint OCR_NORMAL = 32512;
        //The text selection (I-beam) cursor.
        public static uint OCR_IBEAM = 32513;
        public static uint OCR_WaitCursor = 32514;
        //The cross-shaped cursor.
        public static uint OCR_CROSS = 32515;
        [DllImport("user32.dll")]
        public static extern bool SetSystemCursor(IntPtr hcur, uint id);
        [DllImport("user32.dll")]
        public static extern IntPtr LoadCursor(IntPtr hInstance, uint lpCursorName);
        [DllImport("user32.dll")]
        public static extern IntPtr CopyIcon(IntPtr pcur);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern Int32 SystemParametersInfo(UInt32 uiAction,
            UInt32 uiParam, String pvParam, UInt32 fWinIni);

        //[DllImport("User32.DLL")]
        //public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);
        [DllImport("user32")]
        public static extern IntPtr LoadCursorFromFile(string fileName);
        [DllImport("user32.dll")]
        public static extern IntPtr LoadCursorFromFileW(string fileName);
        [DllImport("user32.dll")]
        public static extern IntPtr SetCursor(IntPtr cursorHandle);
        [DllImport("user32.dll")]
        public static extern uint DestroyCursor(IntPtr cursorHandle);

        [DllImport("user32")]
        public static extern int GetSystemMetrics(int nIndex);
        #endregion

        #region DeviceCaps常量
        const int HORZRES = 8;
        const int VERTRES = 10;
        const int LOGPIXELSX = 88;
        const int LOGPIXELSY = 90;
        const int DESKTOPVERTRES = 117;
        const int DESKTOPHORZRES = 118;
        #endregion
        /// <summary>
        /// 获取宽度缩放百分比
        /// </summary>
        public static float ScaleX
        {
            get
            {
                IntPtr hdc = GetDC(IntPtr.Zero);
                int t = GetDeviceCaps(hdc, DESKTOPHORZRES);
                int d = GetDeviceCaps(hdc, HORZRES);
                float ScaleX = ((float)t) / d;
                ReleaseDC(IntPtr.Zero, hdc);
                return ScaleX;
            }
        }
        public static float ScaleY
        {
            get
            {
                IntPtr hdc = GetDC(IntPtr.Zero);
                int t = GetDeviceCaps(hdc, DESKTOPVERTRES);
                int d = GetDeviceCaps(hdc, VERTRES);
                float ScaleX = ((float)t) / d;
                ReleaseDC(IntPtr.Zero, hdc);
                return ScaleX;
            }
        }

        public static Point GetScreenSize()
        {
            return new Point(GetSystemMetrics(0), GetSystemMetrics(1));
        }

        public static Size DESKTOP
        {
            get
            {
                IntPtr hdc = GetDC(IntPtr.Zero);
                Size size = new Size();
                size.Width = GetDeviceCaps(hdc, DESKTOPHORZRES);
                size.Height = GetDeviceCaps(hdc, DESKTOPVERTRES);
                ReleaseDC(IntPtr.Zero, hdc);
                return size;
            }
        }
        public static Size GetRealSize
        {
            get
            {
                Size size = new Size();
                size.Width = (int)(DESKTOP.Width / GetWinScaling(DpiX));
                size.Height = (int)(DESKTOP.Height / GetWinScaling(DpiX));
                return size;
            }
        }
        public const uint WS_EX_LAYERED = 0x80000;
        public const uint WS_EX_TRANSPARENT = 0x20;
        //private const int GWL_STYLE = (-16);
        //private const int GWL_EXSTYLE = (-20);
        public const int LWA_ALPHA = 0;
        //private const int LWA_COLORKEY = 1;

        public const int SMTO_ABORTIFHUNG = 0x2;

        [DllImport("user32", EntryPoint = "SetWindowLong")]
        public static extern uint SetWindowLong(
        IntPtr hwnd,
        int nIndex,
        uint dwNewLong
        );
        [DllImport("user32", EntryPoint = "GetWindowLong")]

        public static extern uint GetWindowLong(IntPtr hwnd, int nIndex);

        [DllImport("user32", EntryPoint = "SetLayeredWindowAttributes")]
        public static extern int SetLayeredWindowAttributes(
        IntPtr hwnd,
        int crKey,
        int bAlpha,
        int dwFlags
        );

        /// <summary>
        /// 当前系统DPI_X 大小 一般为96
        /// </summary>
        public static int DpiX
        {
            get
            {
                IntPtr hdc = GetDC(IntPtr.Zero);
                int DpiX = GetDeviceCaps(hdc, LOGPIXELSX);
                ReleaseDC(IntPtr.Zero, hdc);
                return DpiX;
            }
        }
        public static int DpiY
        {
            get
            {
                IntPtr hdc = GetDC(IntPtr.Zero);
                int DpiX = GetDeviceCaps(hdc, LOGPIXELSY);
                ReleaseDC(IntPtr.Zero, hdc);
                return DpiX;
            }
        }
        /// <summary>
        /// 获取动态的系统DPI_X 大小  防止分辨率改变后拿不到实际改变之后的dpi  !!!!win10以下版本支持有问题
        /// </summary>
        public static int DynamicDpiX
        {
            get
            {
                IntPtr hd1 = FindWindow("Progman", "Program Manager");
                IntPtr hd2 = FindWindow("WorkerW", null);
                IntPtr hd = hd1 == IntPtr.Zero ? hd2 : hd1;
                int DpiX = GetDpiForWindow(hd);
                return DpiX;
            }
        }

        /// <summary>
        /// 获取动态的系统DPI_X 大小  防止分辨率改变后拿不到实际改变之后的dpi  !!!!win10以下版本支持有问题
        /// </summary>
        public static double DynamiDpiScale
        {
            get
            {
                IntPtr hd = IntPtr.Zero;
                IntPtr hd1 = FindWindow("Progman", "Program Manager");

                if (hd1 == null || hd1 == IntPtr.Zero)
                {
                    IntPtr hd2 = FindWindow("WorkerW", null);
                    if (hd2 == null || hd2 == IntPtr.Zero)
                    {
                        return DpiX / 96.0;
                    }
                    hd = hd2;
                }
                else
                {
                    hd = hd1;
                }

                int Dpi = GetDpiForWindow(hd);
                return Dpi / 96.0;
            }
        }

        /// <summary>
        /// 获取动态的系统DPI 大小  防止分辨率改变后拿不到实际改变之后的dpi  !!!!win10以下版本支持有问题
        /// </summary>
        public static int DynamicDpi
        {
            get
            {
                int Dpi = GetDpiForSystem();
                return Dpi;
            }
        }

        //UINT GetDpiForSystem();

        /// <summary>
        /// 获取当前dpix(或者说缩放)的改变比例 用于修正屏幕分辨率动态改变后对元素位置以及框体大小的影响
        /// </summary>
        public static double DynamicScaleX
        {
            get
            {
                double dScale = (double)DynamicDpiX / (double)DpiX;
                return dScale;
            }
        }

        /// <summary>
        /// 获取当前dpi(或者说缩放)的改变比例 用于修正屏幕分辨率动态改变后对元素位置以及框体大小的影响
        /// </summary>
        public static double DynamicScale
        {
            get
            {
                double dScale = (double)DpiX / 96;
                return dScale;
            }
        }


        public static Point GetRealPoint(Point point)
        {
            float a = GetWinScaling(NativeMethods.DpiX);
            return new Point((int)(point.X / a), (int)(point.Y / a));
        }

        public static float GetWinScaling(int dpiX)
        {
            return ((float)dpiX) / 96;
        }
        #region [查询窗口信息]
        //先写在这 
        public static bool WindowState(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            foreach (Process proc in processes)
            {
                if (proc.ProcessName == processName)
                {
                    if (proc.MainWindowHandle != IntPtr.Zero)
                    {
                        var placement = GetPlacement(proc.MainWindowHandle);
                        if (placement.showCmd.ToString() == "Hide" || placement.showCmd.ToString() == "Minimized")
                            return false;
                        else
                            return true;
                    }
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        //目前只用了 ShowWindowCommands
        public static WINDOWPLACEMENT GetPlacement(IntPtr hwnd)
        {
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(placement);
            GetWindowPlacement(hwnd, ref placement);
            return placement;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowPlacement(
            IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public ShowWindowCommands showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }

        public enum ShowWindowCommands : int
        {
            Hide = 0,
            Normal = 1,
            Minimized = 2,
            Maximized = 3,
        }
        public static class ShowWindowTypes
        {
            public const int SW_HIDE = 0;
            public const int SW_SHOWNORMAL = 1;
            public const int SW_NORMAL = 1;
            public const int SW_SHOWMINIMIZED = 2;
            public const int SW_SHOWMAXIMIZED = 3;
            public const int SW_MAXIMIZE = 3;
            public const int SW_SHOWNOACTIVATE = 4;
            public const int SW_SHOW = 5;
            public const int SW_MINIMIZE = 6;
            public const int SW_SHOWMINNOACTIVE = 7;
            public const int SW_SHOWNA = 8;
            public const int SW_RESTORE = 9;
            public const int SW_SHOWDEFAULT = 10;
            public const int SW_FORCEMINIMIZE = 11;
            public const int SW_MAX = 11;
        }
        #endregion
        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        /// <summary>
        /// 获取目标窗体的dpi   !!!!win10以下版本支持有问题
        /// </summary>
        /// <param name="hwnd"></param>
        /// <returns></returns>
        private static int GetDpiForWindow(IntPtr hwnd) => II.RPA.Compatibility.WindowsDpi.ForWindow(hwnd);

        /// <summary>
        /// 获取系统的dpi
        /// </summary>
        /// <param name="hwnd"></param>
        /// <returns></returns>
        private static int GetDpiForSystem() => II.RPA.Compatibility.WindowsDpi.ForSystem();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string lclassName, string windowTitle);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowTextLength(IntPtr hWnd);
        [DllImport("user32")]
        public static extern IntPtr WindowFromPoint(Point Point);//坐标
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessageTimeout(IntPtr hWnd, int uMsg, IntPtr wParam, IntPtr lParam, int fuFlags, int uTimeout, out UIntPtr lpdwResult);
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetShellWindow();
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int RegisterWindowMessage(string message);
        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("oleacc.dll", CharSet = CharSet.Auto, PreserveSig = false)]
        [return: MarshalAs(UnmanagedType.Interface)]
        public static extern object ObjectFromLresult(UIntPtr lResult, [MarshalAs(UnmanagedType.LPStruct)] Guid refiid, IntPtr wParam);
        [DllImport("oleacc.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        public static extern Int32 ObjectFromLresult(Int32 lResult, ref Guid riid, Int32 wParam, out IHTMLDocument2 ppvObject);



        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr WindowFromPoint(int xPoint, int yPoint);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int ObjectFromLresult(
        int lResult,
        ref System.Guid riid,
        int wParam,
        [System.Runtime.InteropServices.MarshalAs(UnmanagedType.Interface), System.Runtime.InteropServices.In, System.Runtime.InteropServices.Out] ref System.Object ppvObject
        //注意这个函数ObjectFromLresult的声明.
        );
        public static string GetLastWin32ErrorMessage(string functionName)
        {
            var code = Marshal.GetLastWin32Error();
            var ex = new Win32Exception(Marshal.GetLastWin32Error());
            string message = String.Format("{0} Error: {1} {2}", functionName, code, ex.Message);
            return message;
        }
        public static Win32Exception GetLastWin32Error(string functionName)
        {
            var code = Marshal.GetLastWin32Error();
            var ex = new Win32Exception(Marshal.GetLastWin32Error());
            // return new Win32Exception(Marshal.GetLastWin32Error());
            return new Win32Exception(Marshal.GetLastWin32Error(), functionName + "::" + ex.Message);
        }
        public const int SW_FORCEMINIMIZE = 11; // Minimizes a window, even if the thread that owns the window is not responding. This flag should only be used when minimizing windows from a different thread.
        public const int SW_HIDE = 0; // Hides the window and activates another window.
        public const int SW_MAXIMIZE = 3; // Maximizes the specified window.
        public const int SW_MINIMIZE = 2; // Minimizes the specified window and activates the next top-level window in the Z order.
        public const int SW_RESTORE = 9; // Activates and displays the window. If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when restoring a minimized window.
        public const int SW_SHOW = 5; // Activates the window and displays it in its current size and position.
        public const int SW_SHOWDEFAULT = 10; // Sets the show state based on the SW_ value specified in the STARTUPINFO structure passed to the CreateProcess function by the program that started the application.
        public const int SW_SHOWMAXIMIZED = 3; // Activates the window and displays it as a maximized window.
        public const int SW_SHOWMINIMIZED = 2; // Activates the window and displays it as a minimized window.
        public const int SW_SHOWMINNOACTIVE = 7; // Displays the window as a minimized window. This value is similar to SW_SHOWMINIMIZED, except the window is not activated.
        public const int SW_SHOWNA = 8; // Displays the window in its current size and position. This value is similar to SW_SHOW, except that the window is not activated.
        public const int SW_SHOWNOACTIVATE = 4; // Displays a window in its most recent size and position. This value is similar to SW_SHOWNORMAL, except that the window is not activated.
        public const int SW_SHOWNORMAL = 1; // Activates and displays a window. If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when displaying the window for the first time.

        public static readonly int GWL_WNDPROC = -4; // Retrieves the address of the window procedure, or a handle representing the address of the window procedure. You must use the CallWindowProc function to call the window procedure.
        public static readonly int GWL_HINSTANCE = -6;
        public static readonly int GWL_HWNDPARENT = -8;
        public static readonly int GWL_STYLE = -16; // Retrieves the window styles.
        public static readonly int GWL_EXSTYLE = -20; // Retrieves the extended window styles.
        public static readonly int GWL_USERDATA = -21; // Gets the 32-bit value associated with the window. Each window has a corresponding 32-bit value intended for use by the application that created the window. Windows CE versions 2.0 and later support this value, but Windows CE versions 1.0 and 1.01 do not.
        public static readonly int GWL_ID = -12; // Not supported.

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtrRaw(IntPtr hWnd, int nIndex);

        public static long GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            return GetWindowLongPtrRaw(hWnd, nIndex).ToInt64();
        }

        #region 通过消息的方式，确认下载，取消
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetDlgItem(IntPtr hWnd, int nIDDlgItem);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        // 消息常量
        public const uint BM_CLICK = 0x00F5; // 点击按钮的消息

        // 假设“保存”按钮的控件 ID 和“取消”按钮的控件 ID
        public const int SAVE_BUTTON_ID = 1;
        public const int CANCEL_BUTTON_ID = 2;
        #endregion
        // This static method is required because legacy OSes do not support
        // SetWindowLongPtr
        public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, long dwNewLong)
        {
            return SetWindowLongPtr(hWnd, nIndex, new IntPtr(dwNewLong));
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
        [DllImport("user32")]
        public static extern int UpdateWindow(IntPtr hwnd);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetDoubleClickTime();
        [DllImport("wtsapi32.dll", SetLastError = true)]
        public static extern bool WTSDisconnectSession(IntPtr hServer, int sessionId, bool bWait);
        [DllImport("wtsapi32.dll", SetLastError = true)]
        public static extern bool WTSLogoffSession(IntPtr hServer, int SessionId, bool bWait);
        [DllImport("wtsapi32.dll")]
        public static extern void WTSFreeMemory(IntPtr pMemory);
        [DllImport("wtsapi32.dll", SetLastError = true)]
        public static extern int WTSEnumerateSessions(
                System.IntPtr hServer,
                int Reserved,
                int Version,
                ref System.IntPtr ppSessionInfo,
                ref int pCount);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        public static extern IntPtr WTSOpenServer(string pServerName);


        [DllImport("wtsapi32.dll")]
        public static extern void WTSCloseServer(IntPtr hServer);
        [StructLayout(LayoutKind.Sequential)]
        public struct WTS_SESSION_INFO
        {
            public Int32 SessionID;

            /// <summary>本机为指向会话名字符串的指针，x86 为 4 字节、x64 为 8 字节。</summary>
            public IntPtr pWinStationName;

            public WTS_CONNECTSTATE_CLASS State;
        }

        public static string GetWtsSessionWinStationName(ref WTS_SESSION_INFO info)
        {
            return info.pWinStationName == IntPtr.Zero ? null : Marshal.PtrToStringUni(info.pWinStationName);
        }
        public enum WTS_CONNECTSTATE_CLASS
        {
            WTSActive,
            WTSConnected,
            WTSConnectQuery,
            WTSShadow,
            WTSDisconnected,
            WTSIdle,
            WTSListen,
            WTSReset,
            WTSDown,
            WTSInit
        }
        public static bool Launch(System.Diagnostics.Process p, string CurrentDirectory, string appCmdLine)
        {
            var priToken = GetPrimaryToken(p);
            if (priToken == IntPtr.Zero) return false;
            IntPtr envBlock = IntPtr.Zero;
            try
            {
                if (!CreateEnvironmentBlock(out envBlock, priToken, false)) throw GetLastWin32Error("Launch:CreateEnvironmentBlock");
                if (!LaunchProcessAsUser(CurrentDirectory, appCmdLine, priToken, envBlock, out System.Diagnostics.Process newProcess, true, out string ErrorMessage))
                {
                    throw GetLastWin32Error("Launch:CreateEnvironmentBlock");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw;
            }
            finally
            {
                if (envBlock != IntPtr.Zero) DestroyEnvironmentBlock(envBlock);
                if (priToken != IntPtr.Zero) CloseHandle(priToken);
            }
            return true;
        }
        public static bool LaunchProcessAsUser(string CurrentDirectory, string cmdLine, IntPtr token, IntPtr envBlock, out System.Diagnostics.Process newProcess, bool AllowClosingProcess, out string ErrorMessage)
        {
            newProcess = null;
            ErrorMessage = null;

#pragma warning disable IDE0059 // Unnecessary assignment of a value
            var pi = new PROCESS_INFORMATION();
#pragma warning restore IDE0059 // Unnecessary assignment of a value
            var saProcess = new SECURITY_ATTRIBUTES();
            var saThread = new SECURITY_ATTRIBUTES();
            saProcess.nLength = uint.Parse(Marshal.SizeOf(saProcess).ToString());
            saThread.nLength = uint.Parse(Marshal.SizeOf(saThread).ToString());
            var si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            si.lpDesktop = "WinSta0\\Default";
            if (AllowClosingProcess) si.lpDesktop = "";
            si.dwFlags = (int)(STARTF.STARTF_USESHOWWINDOW | STARTF.STARTF_FORCEONFEEDBACK);
            si.wShowWindow = SW_SHOW;

            if (!CreateProcessAsUser(token, null, cmdLine, ref saProcess, ref saThread, false, (uint)CreateProcessFlags.CREATE_UNICODE_ENVIRONMENT, envBlock, CurrentDirectory, ref si, out pi))
            {
                throw GetLastWin32Error("LaunchProcessAsUser:CreateProcessAsUser");
            }
            else
            {
                newProcess = System.Diagnostics.Process.GetProcessById(pi.dwProcessId);
            }
            return true;
        }
        public static string GetDomainName() => System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;
        public static string GetHostName() => System.Net.Dns.GetHostName();
        public static string GetFQDN()
        {
            string domainName = GetDomainName();
            string hostName = GetHostName().ToLower();
            if (!hostName.EndsWith(domainName) && !string.IsNullOrEmpty(domainName)) // if hostname does not already include domain name
            {
                domainName = "." + domainName.ToLower();
                hostName += domainName; // add the domain name part
            }
            return hostName; // return the fully qualified name
        }
        public static bool MachineMemberOfDomain() => GetDomainName().Contains(".");
        public static bool IsAdmin(string username)
        {
            var wi = new System.Security.Principal.WindowsIdentity(username);
            var wp = new System.Security.Principal.WindowsPrincipal(wi);
            return wp.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        public static bool IsAdmin(IntPtr p_token)
        {
            var wi = new System.Security.Principal.WindowsIdentity(p_token);
            var wp = new System.Security.Principal.WindowsPrincipal(wi);
            return wp.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        public static bool IsDomainAdmin(IntPtr p_token)
        {
            var DomainName = GetDomainName();
            if (string.IsNullOrEmpty(DomainName)) return false;
            var d = System.DirectoryServices.ActiveDirectory.Domain.GetDomain(new System.DirectoryServices.ActiveDirectory.DirectoryContext(System.DirectoryServices.ActiveDirectory.DirectoryContextType.Domain, DomainName));
            var de = d.GetDirectoryEntry();
            byte[] domainSidArray = de.Properties["objectSid"].Value as byte[];
            var domainSid = new System.Security.Principal.SecurityIdentifier(domainSidArray, 0);
            var domainAdminsSid = new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.AccountDomainAdminsSid, domainSid);

            var wi = new System.Security.Principal.WindowsIdentity(p_token);
            var wp = new System.Security.Principal.WindowsPrincipal(wi);
            return wp.IsInRole(domainAdminsSid);
        }
        public static IntPtr GetPrimaryToken(System.Diagnostics.Process p)
        {
            IntPtr primaryToken = IntPtr.Zero;
            //Gets impersonation token 
            if (!OpenProcessToken(p.Handle, TOKEN_DUPLICATE, out IntPtr token))
            {
                throw GetLastWin32Error("GetPrimaryToken:OpenProcessToken");
            }
            SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
            sa.nLength = (uint)Marshal.SizeOf(sa);
            //Convert the impersonation token into Primary token 
            if (!DuplicateTokenEx(
                token,
                TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_QUERY,
                ref sa,
                (int)SECURITY_IMPERSONATION_LEVEL.SecurityIdentification,
                (int)TOKEN_TYPE.TokenPrimary,
                ref primaryToken))
            {
                throw GetLastWin32Error("GetPrimaryToken:DuplicateTokenEx");
            }

            //Close the Token that was previously opened. 
            CloseHandle(token);
            return primaryToken;
        }
        public static System.Diagnostics.Process GetParentProcessId(System.Diagnostics.Process p = null)
        {
            if (p == null) p = System.Diagnostics.Process.GetCurrentProcess();
            NativeMethods.PROCESS_BASIC_INFORMATION pbi = new NativeMethods.PROCESS_BASIC_INFORMATION();

            //Get a handle to our own process
            IntPtr hProc = p.Handle; // OpenProcess((ProcessAccessFlags)0x001F0FFF, false, Process.GetCurrentProcess().Id);

            try
            {
                int queryStatus = NativeMethods.NtQueryInformationProcess(hProc, 0, ref pbi, pbi.Size, out int sizeInfoReturned);
            }
            finally
            {
                if (!hProc.Equals(IntPtr.Zero))
                {
                    //Close handle and free allocated memory
                    // CloseHandle(hProc);
                }
            }
            var pid = (int)pbi.InheritedFromUniqueProcessId;
            if (pid < 1) return null;
            return System.Diagnostics.Process.GetProcessById(pid);
        }
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out IntPtr TokenHandle);
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out SafeTokenHandle TokenHandle);
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool GetTokenInformation(IntPtr TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, IntPtr TokenInformation, uint TokenInformationLength, out uint ReturnLength);
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool GetTokenInformation(SafeTokenHandle TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, IntPtr TokenInformation, uint TokenInformationLength, out uint ReturnLength);
        public static byte[] GetSIDByteArr(IntPtr processHandle)
        {
            int MAX_INTPTR_BYTE_ARR_SIZE = 512;
            byte[] sidBytes;

            // Get the Process Token
            if (!OpenProcessToken(processHandle, TOKEN_READ, out IntPtr tokenHandle)) throw GetLastWin32Error("GetSIDByteArr:OpenProcessToken");

            // get the token info length
            if (!GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenUser, IntPtr.Zero, 0, out uint tokenInfoLength)) throw GetLastWin32Error("GetSIDByteArr:GetTokenInformation");

            IntPtr tokenInfo = Marshal.AllocHGlobal((int)tokenInfoLength);

            // Get the User SID
            if (GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenUser, tokenInfo, 0, out _))
            {
                TOKEN_USER tokenUser = (TOKEN_USER)Marshal.PtrToStructure(tokenInfo, typeof(TOKEN_USER));
                sidBytes = new byte[MAX_INTPTR_BYTE_ARR_SIZE];  // Since I don't yet know how to be more precise w/ the size of the byte arr, it is being set to 512
                Marshal.Copy(tokenUser.User.Sid, sidBytes, 0, MAX_INTPTR_BYTE_ARR_SIZE);  // get a byte[] representation of the SID
            }
            else throw GetLastWin32Error("GetSIDByteArr:GetTokenInformation");

            return sidBytes;
        }
        // Using IntPtr for pSID insted of Byte[]
        [DllImport("advapi32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool ConvertSidToStringSid(IntPtr pSID, out IntPtr ptrSid);
        [System.Runtime.InteropServices.DllImport("NTDLL.DLL", SetLastError = true)]
        public static extern int NtQueryInformationProcess(IntPtr hProcess, PROCESSINFOCLASS pic, ref PROCESS_BASIC_INFORMATION pbi, int cb, out int pSize);
        public static string GetProcessSID(System.Diagnostics.Process process = null)
        {
            if (process == null) process = System.Diagnostics.Process.GetCurrentProcess();
            SafeTokenHandle hToken;
            try
            {
                if (!NativeMethods.OpenProcessToken(process.Handle, NativeMethods.TOKEN_QUERY, out hToken))
                {
                    return "";
                }
            }
            catch (Exception)
            {
                return "";
            }
            // first call gets lenght of TokenInformation
            _ = GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenUser, IntPtr.Zero, 0, out uint TokenInfLength);
            IntPtr TokenInformation = Marshal.AllocHGlobal(int.Parse(TokenInfLength.ToString()));
            if (GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenUser, TokenInformation, TokenInfLength, out _))
            {
                TOKEN_USER TokenUser = (TOKEN_USER)Marshal.PtrToStructure(TokenInformation, typeof(TOKEN_USER));
                _ = ConvertSidToStringSid(TokenUser.User.Sid, out IntPtr pstr);
                string sidstr = Marshal.PtrToStringAuto(pstr);
                LocalFree(pstr);
                return sidstr;
            }
            return "";
        }
        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);
        public static IntPtr OpenProcess(System.Diagnostics.Process proc, ProcessAccessFlags flags)
        {
            return OpenProcess(flags, false, proc.Id);
        }
        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool QueryFullProcessImageName(IntPtr hProcess, uint flags, StringBuilder exeName, ref uint nameLen);
        [DllImport("kernel32.dll")]
        public static extern uint WaitForSingleObject(IntPtr h, uint milliseconds);
        public static int UOI_FLAGS = 1;

        // GetUserObjectSecurity
        [DllImport("user32.dll", EntryPoint = "GetUserObjectSecurity")]
        public static extern int GetUserObjectSecurity([System.Runtime.InteropServices.InAttribute()] IntPtr hObj,
            [In] ref uint pSIRequested, System.IntPtr pSID, uint nLength, [System.Runtime.InteropServices.OutAttribute()] out uint lpnLengthNeeded);
        public static int GetUserObjectInformationW([In] IntPtr hObj, int nIndex, object pvInfo, int nLength, out int LengthNeeded)
        {
            int flagsLength;
            int retValue;

            // First figure out how much length is needed
            retValue = GetUserObjectInformationW(hObj, UOI_FLAGS, IntPtr.Zero, 0, out LengthNeeded);

            // Set the length to the length needed
            flagsLength = LengthNeeded;

            // Now make the call again with the right size and structure
            GCHandle h0 = GCHandle.Alloc(pvInfo, GCHandleType.Pinned);
            try
            {
                retValue = GetUserObjectInformationW(hObj, UOI_FLAGS, h0.AddrOfPinnedObject(), flagsLength, out LengthNeeded);
            }
            finally
            {
                h0.Free();
            }

            return retValue;
        }
        //public static string GetProcessUserName(System.Diagnostics.Process process = null)
        public static string GetProcessUserName(int PId)
        {
            var hProcess = OpenProcess(ProcessAccessFlags.QueryInformation, false, PId);    //PId is a target process id
            if (hProcess == IntPtr.Zero) throw GetLastWin32Error("GetProcessUserName:OpenProcess");
            SafeTokenHandle hToken;
            try
            {
                // IntPtr handle = OpenProcess( ProcessAccessFlags.Synchronize, false, process.Id);
                if (!OpenProcessToken(hProcess, TOKEN_QUERY, out hToken))
                //if (!OpenProcessToken(OpenProcess(process, ProcessAccessFlags.QueryInformation), TOKEN_QUERY, out hToken))
                {
                    throw GetLastWin32Error("GetProcessUserName:OpenProcessToken");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
                return "";
            }
            // first call gets lenght of TokenInformation
            _ = GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenUser, IntPtr.Zero, 0, out uint TokenInfLength);
            IntPtr TokenInformation = Marshal.AllocHGlobal(int.Parse(TokenInfLength.ToString()));
            if (GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenUser, TokenInformation, TokenInfLength, out _))
            {
                TOKEN_USER TokenUser = (TOKEN_USER)Marshal.PtrToStructure(TokenInformation, typeof(TOKEN_USER));
                var a = new System.Security.Principal.SecurityIdentifier(TokenUser.User.Sid);
                return a.Translate(typeof(System.Security.Principal.NTAccount)).ToString();
            }
            else
            {
                throw GetLastWin32Error("GetProcessUserName:GetTokenInformation2");
            }
        }
        public static string GetProcessUserName_old(System.Diagnostics.Process process = null)
        {
            if (process == null) process = System.Diagnostics.Process.GetCurrentProcess();
            // if (process.HasExited) return "";
            SafeTokenHandle hToken;
            try
            {
                var me = process = System.Diagnostics.Process.GetCurrentProcess();
                var htok = IntPtr.Zero;
                if (!OpenProcessToken(me.Handle,
                    System.Security.Principal.TokenAccessLevels.AdjustPrivileges | System.Security.Principal.TokenAccessLevels.Query, out htok))
                {
                    throw GetLastWin32Error("GetProcessUserName_old:OpenProcessToken");
                }
                var tkp = new TOKEN_PRIVILEGES { PrivilegeCount = 1, Privileges = new LUID_AND_ATTRIBUTES[1] };
                LUID luid;
                if (!LookupPrivilegeValue(null, "SeDebugPrivilege", out luid))
                {
                    throw GetLastWin32Error("GetProcessUserName_old:LookupPrivilegeValue");
                }
                tkp.Privileges[0].LUID = luid;
                tkp.Privileges[0].Attributes = (uint)(2);
                TOKEN_PRIVILEGES prv;
                uint rb;
                if (!AdjustTokenPrivileges(htok, false, tkp, 256, out prv, out rb))
                {
                    throw GetLastWin32Error("GetProcessUserName_old:AdjustTokenPrivileges");
                }
                if (!OpenProcessToken(me.Handle, NativeMethods.TOKEN_QUERY, out hToken))
                {
                    throw GetLastWin32Error("GetProcessUserName_old:OpenProcessToken");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                return "";
            }
            // first call gets lenght of TokenInformation
            _ = GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenUser, IntPtr.Zero, 0, out uint TokenInfLength);
            IntPtr TokenInformation = Marshal.AllocHGlobal(int.Parse(TokenInfLength.ToString()));
            if (GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenUser, TokenInformation, TokenInfLength, out _))
            {
                TOKEN_USER TokenUser = (TOKEN_USER)Marshal.PtrToStructure(TokenInformation, typeof(TOKEN_USER));
                var a = new System.Security.Principal.SecurityIdentifier(TokenUser.User.Sid);
                return a.Translate(typeof(System.Security.Principal.NTAccount)).ToString();
            }
            else
            {
                throw GetLastWin32Error("GetProcessUserName_old:GetTokenInformation");
            }
        }
        [DllImport("kernel32.dll")]
        public static extern IntPtr LocalFree(IntPtr hMem);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean CloseHandle(IntPtr hObject);



        public static string GetSecurityEntityValue(SecurityEntity securityEntity)
        {
            switch (securityEntity)
            {
                case SecurityEntity.SE_ASSIGNPRIMARYTOKEN_NAME:
                    return "SeAssignPrimaryTokenPrivilege";
                case SecurityEntity.SE_AUDIT_NAME:
                    return "SeAuditPrivilege";
                case SecurityEntity.SE_BACKUP_NAME:
                    return "SeBackupPrivilege";
                case SecurityEntity.SE_CHANGE_NOTIFY_NAME:
                    return "SeChangeNotifyPrivilege";
                case SecurityEntity.SE_CREATE_GLOBAL_NAME:
                    return "SeCreateGlobalPrivilege";
                case SecurityEntity.SE_CREATE_PAGEFILE_NAME:
                    return "SeCreatePagefilePrivilege";
                case SecurityEntity.SE_CREATE_PERMANENT_NAME:
                    return "SeCreatePermanentPrivilege";
                case SecurityEntity.SE_CREATE_SYMBOLIC_LINK_NAME:
                    return "SeCreateSymbolicLinkPrivilege";
                case SecurityEntity.SE_CREATE_TOKEN_NAME:
                    return "SeCreateTokenPrivilege";
                case SecurityEntity.SE_DEBUG_NAME:
                    return "SeDebugPrivilege";
                case SecurityEntity.SE_ENABLE_DELEGATION_NAME:
                    return "SeEnableDelegationPrivilege";
                case SecurityEntity.SE_IMPERSONATE_NAME:
                    return "SeImpersonatePrivilege";
                case SecurityEntity.SE_INC_BASE_PRIORITY_NAME:
                    return "SeIncreaseBasePriorityPrivilege";
                case SecurityEntity.SE_INCREASE_QUOTA_NAME:
                    return "SeIncreaseQuotaPrivilege";
                case SecurityEntity.SE_INC_WORKING_SET_NAME:
                    return "SeIncreaseWorkingSetPrivilege";
                case SecurityEntity.SE_LOAD_DRIVER_NAME:
                    return "SeLoadDriverPrivilege";
                case SecurityEntity.SE_LOCK_MEMORY_NAME:
                    return "SeLockMemoryPrivilege";
                case SecurityEntity.SE_MACHINE_ACCOUNT_NAME:
                    return "SeMachineAccountPrivilege";
                case SecurityEntity.SE_MANAGE_VOLUME_NAME:
                    return "SeManageVolumePrivilege";
                case SecurityEntity.SE_PROF_SINGLE_PROCESS_NAME:
                    return "SeProfileSingleProcessPrivilege";
                case SecurityEntity.SE_RELABEL_NAME:
                    return "SeRelabelPrivilege";
                case SecurityEntity.SE_REMOTE_SHUTDOWN_NAME:
                    return "SeRemoteShutdownPrivilege";
                case SecurityEntity.SE_RESTORE_NAME:
                    return "SeRestorePrivilege";
                case SecurityEntity.SE_SECURITY_NAME:
                    return "SeSecurityPrivilege";
                case SecurityEntity.SE_SHUTDOWN_NAME:
                    return "SeShutdownPrivilege";
                case SecurityEntity.SE_SYNC_AGENT_NAME:
                    return "SeSyncAgentPrivilege";
                case SecurityEntity.SE_SYSTEM_ENVIRONMENT_NAME:
                    return "SeSystemEnvironmentPrivilege";
                case SecurityEntity.SE_SYSTEM_PROFILE_NAME:
                    return "SeSystemProfilePrivilege";
                case SecurityEntity.SE_SYSTEMTIME_NAME:
                    return "SeSystemtimePrivilege";
                case SecurityEntity.SE_TAKE_OWNERSHIP_NAME:
                    return "SeTakeOwnershipPrivilege";
                case SecurityEntity.SE_TCB_NAME:
                    return "SeTcbPrivilege";
                case SecurityEntity.SE_TIME_ZONE_NAME:
                    return "SeTimeZonePrivilege";
                case SecurityEntity.SE_TRUSTED_CREDMAN_ACCESS_NAME:
                    return "SeTrustedCredManAccessPrivilege";
                case SecurityEntity.SE_UNDOCK_NAME:
                    return "SeUndockPrivilege";
                default:
                    throw new ArgumentOutOfRangeException(typeof(SecurityEntity).Name);
            }
        }

        public enum SecurityEntity
        {
            SE_CREATE_TOKEN_NAME,
            SE_ASSIGNPRIMARYTOKEN_NAME,
            SE_LOCK_MEMORY_NAME,
            SE_INCREASE_QUOTA_NAME,
            SE_UNSOLICITED_INPUT_NAME,
            SE_MACHINE_ACCOUNT_NAME,
            SE_TCB_NAME,
            SE_SECURITY_NAME,
            SE_TAKE_OWNERSHIP_NAME,
            SE_LOAD_DRIVER_NAME,
            SE_SYSTEM_PROFILE_NAME,
            SE_SYSTEMTIME_NAME,
            SE_PROF_SINGLE_PROCESS_NAME,
            SE_INC_BASE_PRIORITY_NAME,
            SE_CREATE_PAGEFILE_NAME,
            SE_CREATE_PERMANENT_NAME,
            SE_BACKUP_NAME,
            SE_RESTORE_NAME,
            SE_SHUTDOWN_NAME,
            SE_DEBUG_NAME,
            SE_AUDIT_NAME,
            SE_SYSTEM_ENVIRONMENT_NAME,
            SE_CHANGE_NOTIFY_NAME,
            SE_REMOTE_SHUTDOWN_NAME,
            SE_UNDOCK_NAME,
            SE_SYNC_AGENT_NAME,
            SE_ENABLE_DELEGATION_NAME,
            SE_MANAGE_VOLUME_NAME,
            SE_IMPERSONATE_NAME,
            SE_CREATE_GLOBAL_NAME,
            SE_CREATE_SYMBOLIC_LINK_NAME,
            SE_INC_WORKING_SET_NAME,
            SE_RELABEL_NAME,
            SE_TIME_ZONE_NAME,
            SE_TRUSTED_CREDMAN_ACCESS_NAME
        }
        public static void EnableDisablePrivilege(string PrivilegeName, bool EnableDisable)
        {
            var htok = IntPtr.Zero;
            if (!OpenProcessToken(System.Diagnostics.Process.GetCurrentProcess().Handle,
                System.Security.Principal.TokenAccessLevels.AdjustPrivileges | System.Security.Principal.TokenAccessLevels.Query, out htok)) throw GetLastWin32Error("EnableDisablePrivilege:OpenProcessToken");
            var tkp = new TOKEN_PRIVILEGES { PrivilegeCount = 1, Privileges = new LUID_AND_ATTRIBUTES[1] };
            LUID luid;
            if (!LookupPrivilegeValue(null, PrivilegeName, out luid)) throw GetLastWin32Error("EnableDisablePrivilege:LookupPrivilegeValue");
            tkp.Privileges[0].LUID = luid;
            tkp.Privileges[0].Attributes = (uint)(EnableDisable ? 2 : 0);
            TOKEN_PRIVILEGES prv;
            uint rb;
            if (!AdjustTokenPrivileges(htok, false, tkp, 256, out prv, out rb)) throw GetLastWin32Error("EnableDisablePrivilege:AdjustTokenPrivileges");
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint Bufferlength, IntPtr PreviousState, IntPtr ReturnLength);

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID
        {
            private uint lp;
            private int hp;

            public uint LowPart
            {
                get { return lp; }
                set { lp = value; }
            }

            public int HighPart
            {
                get { return hp; }
                set { hp = value; }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID_AND_ATTRIBUTES
        {
            private LUID luid;
            private uint attributes;

            public LUID LUID
            {
                get { return luid; }
                set { luid = value; }
            }

            public uint Attributes
            {
                get { return attributes; }
                set { attributes = value; }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TOKEN_PRIVILEGES
        {
            private uint prvct;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            private LUID_AND_ATTRIBUTES[] privileges;

            public uint PrivilegeCount
            {
                get { return prvct; }
                set { prvct = value; }
            }

            public LUID_AND_ATTRIBUTES[] Privileges
            {
                get { return privileges; }
                set { privileges = value; }
            }
        }

        [DllImport("advapi32", SetLastError = true)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, System.Security.Principal.TokenAccessLevels DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, TOKEN_PRIVILEGES NewState, uint BufferLength, out TOKEN_PRIVILEGES PreviousState, out uint ReturnLength);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);



        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern Boolean AllocConsole();
        [DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx", SetLastError = true)]
        private static extern bool DuplicateTokenEx(IntPtr hExistingToken, uint dwDesiredAccess, ref SECURITY_ATTRIBUTES lpThreadAttributes, Int32 ImpersonationLevel,
            Int32 dwTokenType, ref IntPtr phNewToken);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SetForegroundWindow(IntPtr hWnd);
        [DllImport("User32")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern void SwitchToThisWindow(IntPtr hwnd, bool fUnknown);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsImmersiveProcess(IntPtr hWnd);

        // Win32 RECT
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // GetWindowRect gets the win32 RECT by a window handle.
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        // MoveWindow moves a window or changes its size based on a window handle.
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT rect);

        [DllImport("user32.dll")]
        public static extern bool ClientToScreen(IntPtr hWnd, ref System.Windows.Point lpPoint);
        [DllImport("user32.dll")]
        public static extern uint ScreenToClient(IntPtr hwnd, ref POINT p);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool CreateProcessAsUser(IntPtr hToken, string lpApplicationName, string lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes,
    ref SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory,
    ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);
        [DllImport("userenv.dll", SetLastError = true)]
        public static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);
        [DllImport("userenv.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ExitWindowsEx(uint uFlags, uint dwReason);
        [Flags]
        public enum ExitWindows : uint
        {
            // ONE of the following five:
            LogOff = 0x00,
            ShutDown = 0x01,
            Reboot = 0x02,
            PowerOff = 0x08,
            RestartApps = 0x40,
            // plus AT MOST ONE of the following two:
            Force = 0x04,
            ForceIfHung = 0x10,
        }
        [Flags]
        public enum ShutdownReason : uint
        {
            MajorApplication = 0x00040000,
            MajorHardware = 0x00010000,
            MajorLegacyApi = 0x00070000,
            MajorOperatingSystem = 0x00020000,
            MajorOther = 0x00000000,
            MajorPower = 0x00060000,
            MajorSoftware = 0x00030000,
            MajorSystem = 0x00050000,

            MinorBlueScreen = 0x0000000F,
            MinorCordUnplugged = 0x0000000b,
            MinorDisk = 0x00000007,
            MinorEnvironment = 0x0000000c,
            MinorHardwareDriver = 0x0000000d,
            MinorHotfix = 0x00000011,
            MinorHung = 0x00000005,
            MinorInstallation = 0x00000002,
            MinorMaintenance = 0x00000001,
            MinorMMC = 0x00000019,
            MinorNetworkConnectivity = 0x00000014,
            MinorNetworkCard = 0x00000009,
            MinorOther = 0x00000000,
            MinorOtherDriver = 0x0000000e,
            MinorPowerSupply = 0x0000000a,
            MinorProcessor = 0x00000008,
            MinorReconfig = 0x00000004,
            MinorSecurity = 0x00000013,
            MinorSecurityFix = 0x00000012,
            MinorSecurityFixUninstall = 0x00000018,
            MinorServicePack = 0x00000010,
            MinorServicePackUninstall = 0x00000016,
            MinorTermSrv = 0x00000020,
            MinorUnstable = 0x00000006,
            MinorUpgrade = 0x00000003,
            MinorWMI = 0x00000015,

            FlagUserDefined = 0x40000000,
            FlagPlanned = 0x80000000
        }

        public const int PROCESSBASICINFORMATION = 0;
        private static readonly uint STANDARD_RIGHTS_READ = 0x00020000;
        private static readonly uint TOKEN_ASSIGN_PRIMARY = 0x0001;
        private static readonly uint TOKEN_DUPLICATE = 0x0002;
        private static readonly uint TOKEN_QUERY = 0x0008;
        private static readonly uint TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);
        //private static readonly uint STANDARD_RIGHTS_REQUIRED = 0x000F0000;
        //private static readonly uint TOKEN_IMPERSONATE = 0x0004;
        //private static readonly uint TOKEN_QUERY_SOURCE = 0x0010;
        //private static readonly uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        //private static readonly uint TOKEN_ADJUST_GROUPS = 0x0040;
        //private static readonly uint TOKEN_ADJUST_DEFAULT = 0x0080;
        //private static readonly uint TOKEN_ADJUST_SESSIONID = 0x0100;
        //public const int SW_HIDE = 0;
        //public const int SW_SHOWNORMAL = 1;
        //public const int SW_SHOWMINIMIZED = 2;
        //public const int SW_SHOWMAXIMIZED = 3;
        //public const int SW_MINIMIZE = 6;
        //public const int SW_RESTORE = 9;
        //public const int SW_SHOWDEFAULT = 10;
        //public const int SW_SHOWMINNOACTIVE = 7;
        //public const int SW_SHOWNA = 8;
        //public const int SW_SHOWNOACTIVATE = 4;

        //public const int SW_NORMAL = 1;
        //public const int SW_MAXIMIZE = 3;
        //public const int SW_SHOW = 5;
        [Flags]
        enum CreateProcessFlags
        {
            CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
            CREATE_DEFAULT_ERROR_MODE = 0x04000000,
            CREATE_NEW_CONSOLE = 0x00000010,
            CREATE_NEW_PROCESS_GROUP = 0x00000200,
            CREATE_NO_WINDOW = 0x08000000,
            CREATE_PROTECTED_PROCESS = 0x00040000,
            CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
            CREATE_SEPARATE_WOW_VDM = 0x00000800,
            CREATE_SHARED_WOW_VDM = 0x00001000,
            CREATE_SUSPENDED = 0x00000004,
            CREATE_UNICODE_ENVIRONMENT = 0x00000400,
            DEBUG_ONLY_THIS_PROCESS = 0x00000002,
            DEBUG_PROCESS = 0x00000001,
            DETACHED_PROCESS = 0x00000008,
            EXTENDED_STARTUPINFO_PRESENT = 0x00080000,
            INHERIT_PARENT_AFFINITY = 0x00010000
        }
        [Flags]
        public enum STARTF : uint
        {
            STARTF_USESHOWWINDOW = 0x00000001,
            STARTF_USESIZE = 0x00000002,
            STARTF_USEPOSITION = 0x00000004,
            STARTF_USECOUNTCHARS = 0x00000008,
            STARTF_USEFILLATTRIBUTE = 0x00000010,
            STARTF_RUNFULLSCREEN = 0x00000020,  // ignored for non-x86 platforms
            STARTF_FORCEONFEEDBACK = 0x00000040,
            STARTF_FORCEOFFFEEDBACK = 0x00000080,
            STARTF_USESTDHANDLES = 0x00000100,
        }

        // This also works with CharSet.Ansi as long as the calling function uses the same character set.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct STARTUPINFOEX
        {
            public STARTUPINFO StartupInfo;
            public IntPtr lpAttributeList;
        }

        // This also works with CharSet.Ansi as long as the calling function uses the same character set.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }
        internal enum SECURITY_IMPERSONATION_LEVEL
        {
            SecurityAnonymous,
            SecurityIdentification,
            SecurityImpersonation,
            SecurityDelegation
        }
        internal enum TOKEN_TYPE
        {
            TokenPrimary = 1,
            TokenImpersonation
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct SECURITY_ATTRIBUTES
        {
            public uint nLength;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct SID_AND_ATTRIBUTES
        {
            public IntPtr Sid;
            public int Attributes;
        }
        public struct TOKEN_USER
        {
            public SID_AND_ATTRIBUTES User;
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
        public struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr ExitStatus;
            public IntPtr PebBaseAddress;
            public IntPtr AffinityMask;
            public IntPtr BasePriority;
            public UIntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;
            public int Size
            {
                get { return (int)System.Runtime.InteropServices.Marshal.SizeOf(typeof(PROCESS_BASIC_INFORMATION)); }
                // get { return (1 * sizeof(int) + 5 * IntPtr.Size); }
            }
        }
        public enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin
        }
        public enum PROCESSINFOCLASS : int
        {
            ProcessBasicInformation = 0, // 0, q: PROCESS_BASIC_INFORMATION, PROCESS_EXTENDED_BASIC_INFORMATION
            ProcessQuotaLimits, // qs: QUOTA_LIMITS, QUOTA_LIMITS_EX
            ProcessIoCounters, // q: IO_COUNTERS
            ProcessVmCounters, // q: VM_COUNTERS, VM_COUNTERS_EX
            ProcessTimes, // q: KERNEL_USER_TIMES
            ProcessBasePriority, // s: KPRIORITY
            ProcessRaisePriority, // s: ULONG
            ProcessDebugPort, // q: HANDLE
            ProcessExceptionPort, // s: HANDLE
            ProcessAccessToken, // s: PROCESS_ACCESS_TOKEN
            ProcessLdtInformation, // 10
            ProcessLdtSize,
            ProcessDefaultHardErrorMode, // qs: ULONG
            ProcessIoPortHandlers, // (kernel-mode only)
            ProcessPooledUsageAndLimits, // q: POOLED_USAGE_AND_LIMITS
            ProcessWorkingSetWatch, // q: PROCESS_WS_WATCH_INFORMATION[]; s: void
            ProcessUserModeIOPL,
            ProcessEnableAlignmentFaultFixup, // s: BOOLEAN
            ProcessPriorityClass, // qs: PROCESS_PRIORITY_CLASS
            ProcessWx86Information,
            ProcessHandleCount, // 20, q: ULONG, PROCESS_HANDLE_INFORMATION
            ProcessAffinityMask, // s: KAFFINITY
            ProcessPriorityBoost, // qs: ULONG
            ProcessDeviceMap, // qs: PROCESS_DEVICEMAP_INFORMATION, PROCESS_DEVICEMAP_INFORMATION_EX
            ProcessSessionInformation, // q: PROCESS_SESSION_INFORMATION
            ProcessForegroundInformation, // s: PROCESS_FOREGROUND_BACKGROUND
            ProcessWow64Information, // q: ULONG_PTR
            ProcessImageFileName, // q: UNICODE_STRING
            ProcessLUIDDeviceMapsEnabled, // q: ULONG
            ProcessBreakOnTermination, // qs: ULONG
            ProcessDebugObjectHandle, // 30, q: HANDLE
            ProcessDebugFlags, // qs: ULONG
            ProcessHandleTracing, // q: PROCESS_HANDLE_TRACING_QUERY; s: size 0 disables, otherwise enables
            ProcessIoPriority, // qs: ULONG
            ProcessExecuteFlags, // qs: ULONG
            ProcessResourceManagement,
            ProcessCookie, // q: ULONG
            ProcessImageInformation, // q: SECTION_IMAGE_INFORMATION
            ProcessCycleTime, // q: PROCESS_CYCLE_TIME_INFORMATION
            ProcessPagePriority, // q: ULONG
            ProcessInstrumentationCallback, // 40
            ProcessThreadStackAllocation, // s: PROCESS_STACK_ALLOCATION_INFORMATION, PROCESS_STACK_ALLOCATION_INFORMATION_EX
            ProcessWorkingSetWatchEx, // q: PROCESS_WS_WATCH_INFORMATION_EX[]
            ProcessImageFileNameWin32, // q: UNICODE_STRING
            ProcessImageFileMapping, // q: HANDLE (input)
            ProcessAffinityUpdateMode, // qs: PROCESS_AFFINITY_UPDATE_MODE
            ProcessMemoryAllocationMode, // qs: PROCESS_MEMORY_ALLOCATION_MODE
            ProcessGroupInformation, // q: USHORT[]
            ProcessTokenVirtualizationEnabled, // s: ULONG
            ProcessConsoleHostProcess, // q: ULONG_PTR
            ProcessWindowInformation, // 50, q: PROCESS_WINDOW_INFORMATION
            ProcessHandleInformation, // q: PROCESS_HANDLE_SNAPSHOT_INFORMATION // since WIN8
            ProcessMitigationPolicy, // s: PROCESS_MITIGATION_POLICY_INFORMATION
            ProcessDynamicFunctionTableInformation,
            ProcessHandleCheckingMode,
            ProcessKeepAliveCount, // q: PROCESS_KEEPALIVE_COUNT_INFORMATION
            ProcessRevokeFileHandles, // s: PROCESS_REVOKE_FILE_HANDLES_INFORMATION
            MaxProcessInfoClass
        };



        public static class LsaPrivateData
        {
            public static void RemoveAutologin()
            {
                var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", true);
                key.SetValue("AutoAdminLogon", "0");
                SetValue("DefaultPassword", null);
                var v = key.GetValue("AutoLogonCount");
                if (v != null)
                {
                    key.DeleteValue("AutoLogonCount");
                }
            }
            public static void SetAutologin(string Username, string Password, int AutoLogonCount)
            {
                var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", true);
                key.SetValue("AutoAdminLogon", "1");
                key.SetValue("DefaultUsername", Username);
                // key.SetValue("DefaultPassword", Password);
                SetValue("DefaultPassword", Password);
                if (AutoLogonCount > 0)
                {
                    key.SetValue("AutoLogonCount", AutoLogonCount);
                }
                else
                {
                    var v = key.GetValue("AutoLogonCount");
                    if (v != null)
                    {
                        key.DeleteValue("AutoLogonCount");
                    }
                }
            }

            [Flags]
            private enum CryptProtectFlags
            {
                // for remote-access situations where ui is not an option
                // if UI was specified on protect or unprotect operation, the call
                // will fail and GetLastError() will indicate ERROR_PASSWORD_RESTRICTION
                CRYPTPROTECT_UI_FORBIDDEN = 0x1,

                // per machine protected data -- any user on machine where CryptProtectData
                // took place may CryptUnprotectData
                CRYPTPROTECT_LOCAL_MACHINE = 0x4,

                // force credential synchronize during CryptProtectData()
                // Synchronize is only operation that occurs during this operation
                CRYPTPROTECT_CRED_SYNC = 0x8,

                // Generate an Audit on protect and unprotect operations
                CRYPTPROTECT_AUDIT = 0x10,

                // Protect data with a non-recoverable key
                CRYPTPROTECT_NO_RECOVERY = 0x20,


                // Verify the protection of a protected blob
                CRYPTPROTECT_VERIFY_PROTECTION = 0x40
            }
            [Flags]
            private enum CryptProtectPromptFlags
            {
                // prompt on unprotect
                CRYPTPROTECT_PROMPT_ON_UNPROTECT = 0x1,

                // prompt on protect
                CRYPTPROTECT_PROMPT_ON_PROTECT = 0x2
            }
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            private struct CRYPTPROTECT_PROMPTSTRUCT
            {
                public int cbSize;
                public CryptProtectPromptFlags dwPromptFlags;
                public IntPtr hwndApp;
                public String szPrompt;
            }
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            private struct DATA_BLOB
            {
                public int cbData;
                public IntPtr pbData;
            }
            [DllImport("Crypt32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool CryptProtectData(ref DATA_BLOB pDataIn, String szDataDescr, ref DATA_BLOB pOptionalEntropy,
                IntPtr pvReserved, ref CRYPTPROTECT_PROMPTSTRUCT pPromptStruct, CryptProtectFlags dwFlags, ref DATA_BLOB pDataOut);
            [DllImport("Crypt32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool CryptUnprotectData(ref DATA_BLOB pDataIn, StringBuilder szDataDescr, ref DATA_BLOB pOptionalEntropy, IntPtr pvReserved,
                ref CRYPTPROTECT_PROMPTSTRUCT pPromptStruct, CryptProtectFlags dwFlags, ref DATA_BLOB pDataOut);
            public static void RemoveValue(string key)
            {
                SetValue(key, value: null);
            }

            public static void SetValue(string key, string value = null)
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key));

                if (key.Length == 0)
                    throw new ArgumentException($"{nameof(key)} must not be empty", nameof(key));

                var objectAttributes = new LSA_OBJECT_ATTRIBUTES();
                var localsystem = new LSA_UNICODE_STRING();
                var secretName = new LSA_UNICODE_STRING(key);

                var lusSecretData = !string.IsNullOrEmpty(value) ? new LSA_UNICODE_STRING(value) : default;

                var lsaPolicyHandle = GetLsaPolicy(ref objectAttributes, ref localsystem);

                var result = LsaStorePrivateData(lsaPolicyHandle, ref secretName, ref lusSecretData);
                ReleaseLsaPolicy(lsaPolicyHandle);

                var winErrorCode = LsaNtStatusToWinError(result);
                if (winErrorCode != 0)
                    throw new Win32Exception(winErrorCode, "StorePrivateData failed: " + winErrorCode);
            }

            public static string GetValue(string key)
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key));

                if (key.Length == 0)
                    throw new ArgumentException($"{nameof(key)} must not be empty", nameof(key));

                var objectAttributes = new LSA_OBJECT_ATTRIBUTES();
                var localsystem = new LSA_UNICODE_STRING();
                var secretName = new LSA_UNICODE_STRING(key);

                // Get LSA policy
                var lsaPolicyHandle = GetLsaPolicy(ref objectAttributes, ref localsystem);

                var result = LsaRetrievePrivateData(lsaPolicyHandle, ref secretName, out var privateData);
                ReleaseLsaPolicy(lsaPolicyHandle);

                if (result == STATUS_OBJECT_NAME_NOT_FOUND)
                    return null;

                var winErrorCode = LsaNtStatusToWinError(result);
                if (winErrorCode != 0)
                    throw new Win32Exception(winErrorCode, "LsaRetrievePrivateData failed: " + winErrorCode);

                if (privateData == IntPtr.Zero)
                    return null;

                //var lusSecretData = Marshal.PtrToStructure<LSA_UNICODE_STRING>(privateData);
                LSA_UNICODE_STRING lusSecretData = (LSA_UNICODE_STRING)Marshal.PtrToStructure(privateData, typeof(LSA_UNICODE_STRING));
                var value = Marshal.PtrToStringAuto(lusSecretData.Buffer)?.Substring(0, lusSecretData.Length / UnicodeEncoding.CharSize);

                FreeMemory(privateData);

                return value;
            }

            private static IntPtr GetLsaPolicy(ref LSA_OBJECT_ATTRIBUTES objectAttributes, ref LSA_UNICODE_STRING localsystem)
            {
                var ntsResult = LsaOpenPolicy(ref localsystem, ref objectAttributes, (uint)LSA_AccessPolicy.POLICY_GET_PRIVATE_INFORMATION, out var lsaPolicyHandle);
                var winErrorCode = LsaNtStatusToWinError(ntsResult);
                if (winErrorCode != 0)
                    throw new Win32Exception(winErrorCode, "LsaOpenPolicy failed: " + winErrorCode);

                return lsaPolicyHandle;
            }

            private static void ReleaseLsaPolicy(IntPtr lsaPolicyHandle)
            {
                var ntsResult = LsaClose(lsaPolicyHandle);
                var winErrorCode = LsaNtStatusToWinError(ntsResult);
                if (winErrorCode != 0)
                    throw new Win32Exception(winErrorCode, "LsaClose failed: " + winErrorCode);
            }

            private static void FreeMemory(IntPtr buffer)
            {
                var ntsResult = LsaFreeMemory(buffer);
                var winErrorCode = LsaNtStatusToWinError(ntsResult);
                if (winErrorCode != 0)
                    throw new Win32Exception(winErrorCode, "LsaFreeMemory failed: " + winErrorCode);
            }

            private const uint STATUS_OBJECT_NAME_NOT_FOUND = 0xC0000034;

            [StructLayout(LayoutKind.Sequential)]
            private struct LSA_UNICODE_STRING
            {
                public LSA_UNICODE_STRING(string value)
                {
                    Buffer = Marshal.StringToHGlobalUni(value);
                    Length = (ushort)(value.Length * UnicodeEncoding.CharSize);
                    MaximumLength = (ushort)((value.Length + 1) * UnicodeEncoding.CharSize);
                }

                public ushort Length;
                public ushort MaximumLength;
                public IntPtr Buffer;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct LSA_OBJECT_ATTRIBUTES
            {
                public int Length;
                public IntPtr RootDirectory;
                public LSA_UNICODE_STRING ObjectName;
                public uint Attributes;
                public IntPtr SecurityDescriptor;
                public IntPtr SecurityQualityOfService;
            }

            private enum LSA_AccessPolicy : long
            {
                POLICY_VIEW_LOCAL_INFORMATION = 0x00000001L,
                POLICY_VIEW_AUDIT_INFORMATION = 0x00000002L,
                POLICY_GET_PRIVATE_INFORMATION = 0x00000004L,
                POLICY_TRUST_ADMIN = 0x00000008L,
                POLICY_CREATE_ACCOUNT = 0x00000010L,
                POLICY_CREATE_SECRET = 0x00000020L,
                POLICY_CREATE_PRIVILEGE = 0x00000040L,
                POLICY_SET_DEFAULT_QUOTA_LIMITS = 0x00000080L,
                POLICY_SET_AUDIT_REQUIREMENTS = 0x00000100L,
                POLICY_AUDIT_LOG_ADMIN = 0x00000200L,
                POLICY_SERVER_ADMIN = 0x00000400L,
                POLICY_LOOKUP_NAMES = 0x00000800L,
                POLICY_NOTIFICATION = 0x00001000L,
            }

            [DllImport("advapi32.dll", SetLastError = true, PreserveSig = true)]
            private static extern uint LsaRetrievePrivateData(IntPtr PolicyHandle, ref LSA_UNICODE_STRING KeyName, out IntPtr PrivateData);

            [DllImport("advapi32.dll", SetLastError = true, PreserveSig = true)]
            private static extern uint LsaStorePrivateData(IntPtr policyHandle, ref LSA_UNICODE_STRING KeyName, ref LSA_UNICODE_STRING PrivateData);

            [DllImport("advapi32.dll", SetLastError = true, PreserveSig = true)]
            private static extern uint LsaOpenPolicy(ref LSA_UNICODE_STRING SystemName, ref LSA_OBJECT_ATTRIBUTES ObjectAttributes, uint DesiredAccess, out IntPtr PolicyHandle);

            [DllImport("advapi32.dll", SetLastError = true, PreserveSig = true)]
            private static extern int LsaNtStatusToWinError(uint status);

            [DllImport("advapi32.dll", SetLastError = true, PreserveSig = true)]
            private static extern uint LsaClose(IntPtr policyHandle);

            [DllImport("advapi32.dll", SetLastError = true, PreserveSig = true)]
            private static extern uint LsaFreeMemory(IntPtr buffer);
        }



        public const Int32 HC_ACTION = 0;
        public const Int32 WH_KEYBOARD_LL = 13;
        public const Int32 WH_MOUSE_LL = 14;
        public delegate IntPtr LLProc(Int32 nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(Int32 idHook, LLProc lpfn, IntPtr hMod, UInt32 dwThreadId);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, Int32 nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern Boolean UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")]
        public static extern Boolean GetPhysicalCursorPos(ref POINT pt);
        [DllImport("user32.dll")]
        public static extern Int16 GetKeyState(Int32 nVirtKey);
        public struct POINT
        {
            public int x;
            public int y;
        }
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetCursorPos(out POINT lpPoint);


        public const Int32 VK_EXTENDED = 0x100;

        public const Int32 VK_LSHIFT = 0xA0;

        public const Int32 VK_RSHIFT = 0xA1;

        public const Int32 VK_LCONTROL = 0xA2;

        public const Int32 VK_RCONTROL = 0xA3;

        public const Int32 VK_LMENU = 0xA4;

        public const Int32 VK_RMENU = 0xA5;

        public const Int32 VK_LWIN = 0x5B;

        public const Int32 VK_RWIN = 0x5C;

        public const Int32 VK_PACKET = 0xE7;

        public const Int32 WM_KEYUP = 0x0101;

        public const Int32 WM_SYSKEYUP = 0x0105;

        public const Int32 WM_KEYDOWN = 0x0100;

        public const Int32 WM_SYSKEYDOWN = 0x0104;

        public const Int32 WM_LBUTTONUP = 0x0202;

        public const Int32 WM_LBUTTONDOWN = 0x0201;

        public const Int32 WM_MBUTTONUP = 0x0208;

        public const Int32 WM_MBUTTONDOWN = 0x0207;

        public const Int32 WM_MOUSEMOVE = 0x0200;

        public const Int32 WM_RBUTTONUP = 0x0205;

        public const Int32 WM_RBUTTONDOWN = 0x0204;

        public const Int32 LLKHF_EXTENDED = 0x01;
        public const Int32 WM_MouseWheel = 522;

        public abstract class WindowStyles
        {
            public const uint WS_OVERLAPPED = 0x00000000;
            public const uint WS_POPUP = 0x80000000;
            public const uint WS_CHILD = 0x40000000;
            public const uint WS_MINIMIZE = 0x20000000;
            public const uint WS_VISIBLE = 0x10000000;
            public const uint WS_DISABLED = 0x08000000;
            public const uint WS_CLIPSIBLINGS = 0x04000000;
            public const uint WS_CLIPCHILDREN = 0x02000000;
            public const uint WS_MAXIMIZE = 0x01000000;
            public const uint WS_CAPTION = 0x00C00000;     /* WS_BORDER | WS_DLGFRAME  */
            public const uint WS_BORDER = 0x00800000;
            public const uint WS_DLGFRAME = 0x00400000;
            public const uint WS_VSCROLL = 0x00200000;
            public const uint WS_HSCROLL = 0x00100000;
            public const uint WS_SYSMENU = 0x00080000;
            public const uint WS_THICKFRAME = 0x00040000;
            public const uint WS_GROUP = 0x00020000;
            public const uint WS_TABSTOP = 0x00010000;

            public const uint WS_MINIMIZEBOX = 0x00020000;
            public const uint WS_MAXIMIZEBOX = 0x00010000;

            public const uint WS_TILED = WS_OVERLAPPED;
            public const uint WS_ICONIC = WS_MINIMIZE;
            public const uint WS_SIZEBOX = WS_THICKFRAME;
            public const uint WS_TILEDWINDOW = WS_OVERLAPPEDWINDOW;

            // Common Window Styles

            public const uint WS_OVERLAPPEDWINDOW =
                (WS_OVERLAPPED |
                  WS_CAPTION |
                  WS_SYSMENU |
                  WS_THICKFRAME |
                  WS_MINIMIZEBOX |
                  WS_MAXIMIZEBOX);

            public const uint WS_POPUPWINDOW =
                (WS_POPUP |
                  WS_BORDER |
                  WS_SYSMENU);

            public const uint WS_CHILDWINDOW = WS_CHILD;

            //Extended Window Styles

            public const uint WS_EX_DLGMODALFRAME = 0x00000001;
            public const uint WS_EX_NOPARENTNOTIFY = 0x00000004;
            public const uint WS_EX_TOPMOST = 0x00000008;
            public const uint WS_EX_ACCEPTFILES = 0x00000010;
            public const uint WS_EX_TRANSPARENT = 0x00000020;

            //#if(WINVER >= 0x0400)
            public const uint WS_EX_MDICHILD = 0x00000040;
            public const uint WS_EX_TOOLWINDOW = 0x00000080;
            public const uint WS_EX_WINDOWEDGE = 0x00000100;
            public const uint WS_EX_CLIENTEDGE = 0x00000200;
            public const uint WS_EX_CONTEXTHELP = 0x00000400;

            public const uint WS_EX_RIGHT = 0x00001000;
            public const uint WS_EX_LEFT = 0x00000000;
            public const uint WS_EX_RTLREADING = 0x00002000;
            public const uint WS_EX_LTRREADING = 0x00000000;
            public const uint WS_EX_LEFTSCROLLBAR = 0x00004000;
            public const uint WS_EX_RIGHTSCROLLBAR = 0x00000000;

            public const uint WS_EX_CONTROLPARENT = 0x00010000;
            public const uint WS_EX_STATICEDGE = 0x00020000;
            public const uint WS_EX_APPWINDOW = 0x00040000;

            public const uint WS_EX_OVERLAPPEDWINDOW = (WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE);
            public const uint WS_EX_PALETTEWINDOW = (WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST);
            //#endif /* WINVER >= 0x0400 */

            //#if(_WIN32_WINNT >= 0x0500)
            public const uint WS_EX_LAYERED = 0x00080000;
            //#endif /* _WIN32_WINNT >= 0x0500 */

            //#if(WINVER >= 0x0500)
            public const uint WS_EX_NOINHERITLAYOUT = 0x00100000; // Disable inheritence of mirroring by children
            public const uint WS_EX_LAYOUTRTL = 0x00400000; // Right to left mirroring
                                                            //#endif /* WINVER >= 0x0500 */

            //#if(_WIN32_WINNT >= 0x0500)
            public const uint WS_EX_COMPOSITED = 0x02000000;
            public const uint WS_EX_NOACTIVATE = 0x08000000;
            //#endif /* _WIN32_WINNT >= 0x0500 */
        }

        public struct KBDLLHOOKSTRUCT
        {
#pragma warning disable 649
            public UInt32 vkCode;
            public UInt32 scanCode;
            public LLKHF flags;
            public UInt32 time;
            public UIntPtr dwExtraInfo;
        }

        [Flags]
        public enum LLKHF : UInt32
        {
            LLKHF_EXTENDED = 0x01,
            LLKHF_INJECTED = 0x10,
            LLKHF_ALTDOWN = 0x20,
            LLKHF_UP = 0x80
        }

        public static Int32 LOBYTE(Int32 x) => x & 0xFF;

        public static Int32 HIBYTE(Int32 x) => x >> 8;

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "IDE1006")]
        //public static extern void mouse_event(long dwFlags, long dx, long dy, long cButtons, long dwExtraInfo);
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;
        public const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        public const int MOUSEEVENTF_RIGHTUP = 0x10;
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetCursorPos(int x, int y);
        /// <summary>
        /// Delegate declaration that matches WndProc signatures.
        /// </summary>
        public delegate IntPtr MessageHandler(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled);
        [DllImport("shell32.dll", EntryPoint = "CommandLineToArgvW", CharSet = CharSet.Unicode)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "IDE1006")]
        private static extern IntPtr _CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string cmdLine, out int numArgs);
        [DllImport("kernel32.dll", EntryPoint = "LocalFree", SetLastError = true)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "IDE1006")]
        private static extern IntPtr _LocalFree(IntPtr hMem);
        public static string[] CommandLineToArgvW(string cmdLine)
        {
            IntPtr argv = IntPtr.Zero;
            try
            {

                argv = _CommandLineToArgvW(cmdLine, out int numArgs);
                if (argv == IntPtr.Zero)
                {
                    throw new Win32Exception();
                }
                var result = new string[numArgs];

                for (int i = 0; i < numArgs; i++)
                {
                    IntPtr currArg = Marshal.ReadIntPtr(argv, i * Marshal.SizeOf(typeof(IntPtr)));
                    result[i] = Marshal.PtrToStringUni(currArg);
                }

                return result;
            }
            finally
            {
                _ = _LocalFree(argv);
                // Otherwise LocalFree failed.
                // Assert.AreEqual(IntPtr.Zero, p);
            }
        }
        public enum ErrorFlags
        {
            ERROR_INSUFFICIENT_BUFFER = 122,
            ERROR_INVALID_PARAMETER = 87,
            ERROR_NO_MORE_ITEMS = 259
        }
        public enum InternetFlags
        {
            INTERNET_COOKIE_HTTPONLY = 8192, //Requires IE 8 or higher   
            INTERNET_COOKIE_THIRD_PARTY = 131072,
            INTERNET_FLAG_RESTRICTED_ZONE = 16
        }
        [SuppressUnmanagedCodeSecurity, SecurityCritical, DllImport("wininet.dll", EntryPoint = "InternetGetCookieExW", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        public static extern bool InternetGetCookieEx([In] string Url, [In] string cookieName, [Out] StringBuilder cookieData, [In, Out] ref uint pchCookieData, uint flags, IntPtr reserved);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern UInt32 SendInput(UInt32 numberOfInputs, INPUT[] inputs, Int32 sizeOfInputStructure);
        [DllImport("user32.dll")]
        public static extern UInt16 VkKeyScanEx(Char ch, IntPtr dwhkl);
        [DllImport("user32.dll")]
        public static extern IntPtr GetKeyboardLayout(UInt32 idThread);
        [DllImport("user32.dll")]
        public static extern Boolean SetPhysicalCursorPos(Int32 X, Int32 Y);
        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public const UInt32 INPUT_MOUSE = 0;

            public const UInt32 INPUT_KEYBOARD = 1;

            public UInt32 type;
            public INPUTUNION u;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct INPUTUNION
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;

            [FieldOffset(0)]
            public KEYBDINPUT ki;
        }
        public struct MOUSEINPUT
        {
            public Int32 dx;
            public Int32 dy;
            public UInt32 mouseData;
            public MOUSEEVENTF dwFlags;
            public UInt32 time;
            public UIntPtr dwExtraInfo;
        }
        public struct KEYBDINPUT
        {
            public UInt16 wVk;
            public UInt16 wScan;
            public KEYEVENTF dwFlags;
            public UInt32 time;
            public UIntPtr dwExtraInfo;
        }
        [Flags]
        public enum KEYEVENTF : UInt32
        {
            KEYEVENTF_EXTENDEDKEY = 0x0001,
            KEYEVENTF_KEYUP = 0x0002,
            KEYEVENTF_UNICODE = 0x0004,
            KEYEVENTF_SCANCODE = 0x0008,
        }
        [Flags]
        public enum MOUSEEVENTF : UInt32
        {
            MOUSEEVENTF_ABSOLUTE = 0x8000,
            MOUSEEVENTF_HWHEEL = 0x01000,
            MOUSEEVENTF_MOVE = 0x0001,
            MOUSEEVENTF_MOVE_NOCOALESCE = 0x2000,
            MOUSEEVENTF_LEFTDOWN = 0x0002,
            MOUSEEVENTF_LEFTUP = 0x0004,
            MOUSEEVENTF_RIGHTDOWN = 0x0008,
            MOUSEEVENTF_RIGHTUP = 0x0010,
            MOUSEEVENTF_MIDDLEDOWN = 0x0020,
            MOUSEEVENTF_MIDDLEUP = 0x0040,
            MOUSEEVENTF_VIRTUALDESK = 0x4000,
            MOUSEEVENTF_WHEEL = 0x0800,
            MOUSEEVENTF_XDOWN = 0x0080,
            MOUSEEVENTF_XUP = 0x0100
        }


        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int
    wDest, int hDest, IntPtr hdcSource, int xSrc, int ySrc, CopyPixelOperation rop);
        [DllImport("gdi32.dll")]
        public static extern IntPtr DeleteDC(IntPtr hDc);
        [DllImport("gdi32.dll")]
        public static extern IntPtr DeleteObject(IntPtr hDc);
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr bmp);
        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr ptr);

        public static IntPtr FindWindowByClassNameAndTitle(IntPtr parentHandle, string className, string title, string flagStr)
        {
            return FindWindowByClassNameAndTitle(parentHandle, className, title, flagStr, true);
        }
        public static IntPtr FindWindowByClassNameAndTitle(IntPtr parentHandle, string className, string title, string flagStr, bool strictMatching)
        {
            string wndTitle = title + flagStr;// " - Google Chrome";
            IntPtr hwndChildAfter = IntPtr.Zero;
            IntPtr firstSuspected = IntPtr.Zero;
            while (true)
            {
                IntPtr hNextWnd = NativeMethods.FindWindowEx(parentHandle, hwndChildAfter, className, null);
                if (hNextWnd == IntPtr.Zero)
                {
                    break;
                }
                var titleLenght = wndTitle.Length + 1;
                StringBuilder textSB = new StringBuilder(titleLenght);
                NativeMethods.GetWindowText(hNextWnd, textSB, titleLenght);
                if (textSB.ToString().EqualIgnoreCase(wndTitle)
                    || (!strictMatching && textSB.ToString().IndexOf(wndTitle, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return hNextWnd;
                }
                if (firstSuspected == IntPtr.Zero && !string.IsNullOrEmpty(flagStr) && textSB.ToString().Contains(flagStr))
                {
                    firstSuspected = hNextWnd;
                }
                hwndChildAfter = hNextWnd;
            }
            return firstSuspected;
        }
        //[DllImport("dwmapi")]
        //public static extern unsafe int DwmGetWindowAttribute(
        //  IntPtr hwnd,
        //  DWMWINDOWATTRIBUTE dwAttribute,
        //  void* pvAttribute,
        //  uint cbAttribute);
        //public static unsafe bool IsInvisibleWin10Background(IntPtr _hWnd)
        //{
        //    if (Environment.OSVersion.Version.Major < 6)
        //    {
        //        return false;
        //    }

        //    int structure;

        //    if (DwmGetWindowAttribute(
        //            _hWnd,
        //            DWMWINDOWATTRIBUTE.DWMWA_CLOAKED,
        //            (void*)&structure,
        //            (uint)Marshal.SizeOf(structure))
        //        < 0)
        //    {
        //        structure = 0;
        //    }

        //    return structure != 0;
        //}
        //public enum DWMWINDOWATTRIBUTE
        //{
        //    DWMWA_NCRENDERING_ENABLED = 1,
        //    DWMWA_NCRENDERING_POLICY = 2,
        //    DWMWA_TRANSITIONS_FORCEDISABLED = 3,
        //    DWMWA_ALLOW_NCPAINT = 4,
        //    DWMWA_CAPTION_BUTTON_BOUNDS = 5,
        //    DWMWA_NONCLIENT_RTL_LAYOUT = 6,
        //    DWMWA_FORCE_ICONIC_REPRESENTATION = 7,
        //    DWMWA_FLIP3D_POLICY = 8,
        //    DWMWA_EXTENDED_FRAME_BOUNDS = 9,
        //    DWMWA_HAS_ICONIC_BITMAP = 10, // 0x0000000A
        //    DWMWA_DISALLOW_PEEK = 11, // 0x0000000B
        //    DWMWA_EXCLUDED_FROM_PEEK = 12, // 0x0000000C
        //    DWMWA_CLOAK = 13, // 0x0000000D
        //    DWMWA_CLOAKED = 14, // 0x0000000E
        //    DWMWA_FREEZE_REPRESENTATION = 15, // 0x0000000F
        //    DWMWA_PASSIVE_UPDATE_MODE = 16, // 0x00000010
        //    DWMWA_USE_HOSTBACKDROPBRUSH = 17, // 0x00000011
        //    DWMWA_USE_IMMERSIVE_DARK_MODE = 20, // 0x00000014
        //    DWMWA_WINDOW_CORNER_PREFERENCE = 33, // 0x00000021
        //    DWMWA_BORDER_COLOR = 34, // 0x00000022
        //    DWMWA_CAPTION_COLOR = 35, // 0x00000023
        //    DWMWA_TEXT_COLOR = 36, // 0x00000024
        //    DWMWA_VISIBLE_FRAME_BORDER_THICKNESS = 37, // 0x00000025
        //    DWMWA_LAST = 38, // 0x00000026
        //}
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hWnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        public static extern bool PostThreadMessage(int idThread, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern int GetMessage(out NativeMessage lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        public static extern bool TranslateMessage(ref NativeMessage lpMsg);

        [DllImport("user32.dll")]
        public static extern IntPtr DispatchMessage(ref NativeMessage lpMsg);

        [StructLayout(LayoutKind.Sequential)]
        public struct NativeMessage
        {
            public IntPtr hWnd;
            public uint msg;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public int ptX;
            public int ptY;
        }
    }

    public enum WM
    {
        NULL = 0x0000,
        CREATE = 0x0001,
        DESTROY = 0x0002,
        MOVE = 0x0003,
        SIZE = 0x0005,
        ACTIVATE = 0x0006,
        SETFOCUS = 0x0007,
        KILLFOCUS = 0x0008,
        ENABLE = 0x000A,
        SETREDRAW = 0x000B,
        SETTEXT = 0x000C,
        GETTEXT = 0x000D,
        GETTEXTLENGTH = 0x000E,
        PAINT = 0x000F,
        CLOSE = 0x0010,
        QUERYENDSESSION = 0x0011,
        QUIT = 0x0012,
        QUERYOPEN = 0x0013,
        ERASEBKGND = 0x0014,
        SYSCOLORCHANGE = 0x0015,
        SHOWWINDOW = 0x0018,
        ACTIVATEAPP = 0x001C,
        SETCURSOR = 0x0020,
        MOUSEACTIVATE = 0x0021,
        CHILDACTIVATE = 0x0022,
        QUEUESYNC = 0x0023,
        GETMINMAXINFO = 0x0024,

        WINDOWPOSCHANGING = 0x0046,
        WINDOWPOSCHANGED = 0x0047,

        CONTEXTMENU = 0x007B,
        STYLECHANGING = 0x007C,
        STYLECHANGED = 0x007D,
        DISPLAYCHANGE = 0x007E,
        GETICON = 0x007F,
        SETICON = 0x0080,
        NCCREATE = 0x0081,
        NCDESTROY = 0x0082,
        NCCALCSIZE = 0x0083,
        NCHITTEST = 0x0084,
        NCPAINT = 0x0085,
        NCACTIVATE = 0x0086,
        GETDLGCODE = 0x0087,
        SYNCPAINT = 0x0088,
        NCMOUSEMOVE = 0x00A0,
        NCLBUTTONDOWN = 0x00A1,
        NCLBUTTONUP = 0x00A2,
        NCLBUTTONDBLCLK = 0x00A3,
        NCRBUTTONDOWN = 0x00A4,
        NCRBUTTONUP = 0x00A5,
        NCRBUTTONDBLCLK = 0x00A6,
        NCMBUTTONDOWN = 0x00A7,
        NCMBUTTONUP = 0x00A8,
        NCMBUTTONDBLCLK = 0x00A9,

        SYSKEYDOWN = 0x0104,
        SYSKEYUP = 0x0105,
        SYSCHAR = 0x0106,
        SYSDEADCHAR = 0x0107,
        COMMAND = 0x0111,
        SYSCOMMAND = 0x0112,

        MOUSEMOVE = 0x0200,
        LBUTTONDOWN = 0x0201,
        LBUTTONUP = 0x0202,
        LBUTTONDBLCLK = 0x0203,
        RBUTTONDOWN = 0x0204,
        RBUTTONUP = 0x0205,
        RBUTTONDBLCLK = 0x0206,
        MBUTTONDOWN = 0x0207,
        MBUTTONUP = 0x0208,
        MBUTTONDBLCLK = 0x0209,
        MOUSEWHEEL = 0x020A,
        XBUTTONDOWN = 0x020B,
        XBUTTONUP = 0x020C,
        XBUTTONDBLCLK = 0x020D,
        MOUSEHWHEEL = 0x020E,


        CAPTURECHANGED = 0x0215,

        ENTERSIZEMOVE = 0x0231,
        EXITSIZEMOVE = 0x0232,

        IME_SETCONTEXT = 0x0281,
        IME_NOTIFY = 0x0282,
        IME_CONTROL = 0x0283,
        IME_COMPOSITIONFULL = 0x0284,
        IME_SELECT = 0x0285,
        IME_CHAR = 0x0286,
        IME_REQUEST = 0x0288,
        IME_KEYDOWN = 0x0290,
        IME_KEYUP = 0x0291,

        NCMOUSELEAVE = 0x02A2,

        DWMCOMPOSITIONCHANGED = 0x031E,
        DWMNCRENDERINGCHANGED = 0x031F,
        DWMCOLORIZATIONCOLORCHANGED = 0x0320,
        DWMWINDOWMAXIMIZEDCHANGE = 0x0321,

        #region Windows 7
        DWMSENDICONICTHUMBNAIL = 0x0323,
        DWMSENDICONICLIVEPREVIEWBITMAP = 0x0326,
        #endregion

        USER = 0x0400,

        // This is the hard-coded message value used by WinForms for Shell_NotifyIcon.
        // It's relatively safe to reuse.
        TRAYMOUSEMESSAGE = 0x800, //WM_USER + 1024
        APP = 0x8000,
    }
    public class SafeTokenHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeTokenHandle() : base(true) { }
        public SafeTokenHandle(IntPtr handle) : base(true)
        {
            SetHandle(handle);
        }
        protected override bool ReleaseHandle()
        {
            return NativeMethods.CloseHandle(handle);
        }
    }
    public class ProcessSecurity : NativeObjectSecurity
    {
        public ProcessSecurity(SafeHandle processHandle)
            : base(false, ResourceType.KernelObject, processHandle, AccessControlSections.Access)
        {

        }

        public void AddAccessRule(ProcessAccessRule rule)
        {
            base.AddAccessRule(rule);
        }

        // this is not a full impl- it only supports writing DACL changes
        public void SaveChanges(SafeHandle processHandle)
        {
            Persist(processHandle, AccessControlSections.Access);
        }

        public override Type AccessRightType
        {
            get { return typeof(ProcessAccessRights); }
        }

        public override AccessRule AccessRuleFactory(System.Security.Principal.IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
        {
            return new ProcessAccessRule(identityReference, (ProcessAccessRights)accessMask, isInherited, inheritanceFlags, propagationFlags, type);
        }

        public override Type AccessRuleType
        {
            get { return typeof(ProcessAccessRule); }
        }

        public override AuditRule AuditRuleFactory(System.Security.Principal.IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
        {
            throw new NotImplementedException();
        }

        public override Type AuditRuleType
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class ProcessAccessRule : AccessRule
    {
        public ProcessAccessRule(System.Security.Principal.IdentityReference identityReference, ProcessAccessRights accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
            : base(identityReference, (int)accessMask, isInherited, inheritanceFlags, propagationFlags, type)
        {
        }

        public ProcessAccessRights ProcessAccessRights { get { return (ProcessAccessRights)AccessMask; } }
    }

    [Flags]
    public enum ProcessAccessRights
    {
        STANDARD_RIGHTS_REQUIRED = (0x000F0000),
        DELETE = (0x00010000), // Required to delete the object. 
        READ_CONTROL = (0x00020000), // Required to read information in the security descriptor for the object, not including the information in the SACL. To read or write the SACL, you must request the ACCESS_SYSTEM_SECURITY access right. For more information, see SACL Access Right. 
        WRITE_DAC = (0x00040000), // Required to modify the DACL in the security descriptor for the object. 
        WRITE_OWNER = (0x00080000), // Required to change the owner in the security descriptor for the object. 

        PROCESS_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0xFFF, //All possible access rights for a process object.
        PROCESS_CREATE_PROCESS = (0x0080), // Required to create a process. 
        PROCESS_CREATE_THREAD = (0x0002), // Required to create a thread. 
        PROCESS_DUP_HANDLE = (0x0040), // Required to duplicate a handle using DuplicateHandle. 
        PROCESS_QUERY_INFORMATION = (0x0400), // Required to retrieve certain information about a process, such as its token, exit code, and priority class (see OpenProcessToken, GetExitCodeProcess, GetPriorityClass, and IsProcessInJob). 
        PROCESS_QUERY_LIMITED_INFORMATION = (0x1000),
        PROCESS_SET_INFORMATION = (0x0200), // Required to set certain information about a process, such as its priority class (see SetPriorityClass). 
        PROCESS_SET_QUOTA = (0x0100), // Required to set memory limits using SetProcessWorkingSetSize. 
        PROCESS_SUSPEND_RESUME = (0x0800), // Required to suspend or resume a process. 
        PROCESS_TERMINATE = (0x0001), // Required to terminate a process using TerminateProcess. 
        PROCESS_VM_OPERATION = (0x0008), // Required to perform an operation on the address space of a process (see VirtualProtectEx and WriteProcessMemory). 
        PROCESS_VM_READ = (0x0010), // Required to read memory in a process using ReadProcessMemory. 
        PROCESS_VM_WRITE = (0x0020), // Required to write to memory in a process using WriteProcessMemory. 
        SYNCHRONIZE = (0x00100000), // Required to wait for the process to terminate using the wait functions. 
    }
}
