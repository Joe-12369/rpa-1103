using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace II.Framework.PInvoker
{
    public class PowerWindowInvoker
    {
        public Win32Types.Rect WindowRect => _windowRect;
        public Win32Types.Rect ClientRect => WinClientRect;
        public int BitmapDataSize => _bmpDataSize;
        public IntPtr BitmapPtr => HBitmap;
        public Win32Types.BitmapInfo BitmapInfo { get; } = new Win32Types.BitmapInfo();

        protected IntPtr HWnd = IntPtr.Zero;
        protected IntPtr HScrDc = IntPtr.Zero;
        protected IntPtr HMemDc = IntPtr.Zero;
        protected IntPtr HBitmap = IntPtr.Zero;
        protected IntPtr HOldBitmap = IntPtr.Zero;

        private Win32Types.Rect _windowRect;
        protected Win32Types.Rect WinClientRect;
        private int _bmpDataSize;
        protected bool CommonInit()
        {
            HScrDc = Win32Funcs.GetWindowDcWrapper(HWnd);
            float sx;
            float sy;
            try
            {
                var windowDPI = Win32Funcs.GetDpiForWindow(HWnd);
                var systemDPI = PrimaryScreen.DpiX;
                sx = systemDPI / (float)windowDPI;
                sy = systemDPI / (float)windowDPI;
            }
            catch
            {
                sx = 1;
                sy = 1;
            }
            Console.WriteLine($"sx:{sx}   sy:{sy}");
            HBitmap = Win32Funcs.CreateCompatibleBitmapWrapper(HScrDc, (int)(_windowRect.Width / sx), (int)(_windowRect.Height / sy));
            HMemDc = Win32Funcs.CreateCompatibleDcWrapper(HScrDc);
            HOldBitmap = Win32Funcs.SelectObjectWrapper(HMemDc, HBitmap);
            return true;
        }

        protected IntPtr DoCapture()
        {
            var ret = Win32Funcs.PrintWindowWrapper(HWnd, HMemDc,
                OperatingSystem.IsWindowsVersionAtLeast(6, 3) ? (uint)Win32Consts.PrintWindowMode.PW_RENDERFULLCONTENT : 0u);
            return ret ? HBitmap : IntPtr.Zero;
        }

        protected bool DoCapture(out IntPtr bitsPtr)
        {
            bitsPtr = HBitmap;
            var ret = Win32Funcs.PrintWindowWrapper(HWnd, HMemDc,
                OperatingSystem.IsWindowsVersionAtLeast(6, 3) ? (uint)Win32Consts.PrintWindowMode.PW_RENDERFULLCONTENT : 0u);
            return ret;
        }
        public bool Init(string windowName)
        {
            var handle = Win32Funcs.FindWindowWrapper(null, windowName);
            if (handle.Equals(IntPtr.Zero))
            {
                return false;
            }

            return Init(handle);
        }

        public bool Init(IntPtr handle)
        {
            HWnd = handle;

            //获取窗口大小
            if (!Win32Funcs.GetWindowRectWrapper(HWnd, out _windowRect)
                || !Win32Funcs.GetClientRectWrapper(HWnd, out WinClientRect))
            {
                return false;
            }

            _bmpDataSize = _windowRect.Width * _windowRect.Height * 3;

            return CommonInit();
        }

        public void Cleanup()
        {
            if (HBitmap.Equals(IntPtr.Zero))
            {
                return;
            }

            //删除用过的对象
            Win32Funcs.SelectObjectWrapper(HMemDc, HOldBitmap);
            Win32Funcs.DeleteObjectWrapper(HBitmap);
            Win32Funcs.DeleteDcWrapper(HMemDc);
            Win32Funcs.ReleaseDcWrapper(HWnd, HScrDc);

            HWnd = IntPtr.Zero;
            HScrDc = IntPtr.Zero;
            HMemDc = IntPtr.Zero;
            HBitmap = IntPtr.Zero;
            HOldBitmap = IntPtr.Zero;
            //_bitsPtr = IntPtr.Zero;
        }

        public bool RefreshWindow()
        {
            return ChangeWindowHandle(HWnd);
        }

        public bool ChangeWindowHandle(string windowName)
        {
            Cleanup();
            return Init(windowName);
        }

        public bool ChangeWindowHandle(IntPtr handle)
        {
            Cleanup();
            return Init(handle);
        }

        public IntPtr Capture()
        {
            if (HBitmap.Equals(IntPtr.Zero) || HMemDc.Equals(IntPtr.Zero) || HScrDc.Equals(IntPtr.Zero))
            {
                return IntPtr.Zero;
            }

            return DoCapture();
        }

        public bool Capture(out IntPtr bitsPtr, out int bufferSize, out Win32Types.Rect rect)
        {
            bitsPtr = IntPtr.Zero;
            bufferSize = _bmpDataSize;
            rect = WinClientRect;

            if (HBitmap.Equals(IntPtr.Zero) || HMemDc.Equals(IntPtr.Zero) || HScrDc.Equals(IntPtr.Zero))
            {
                return false;
            }

            return DoCapture(out bitsPtr);
        }

        /// <summary>
        /// 设置窗口的位置
        /// </summary>
        /// <param name="aDestinationX">横坐标</param>
        /// <param name="aDestinationY">纵坐标</param>
        public void SetWindowPosition(int aDestinationX, int aDestinationY)
        {
            Point pt = new Point(aDestinationX, aDestinationY);
            Win32Funcs.SetWindowPosWrapper(HWnd, IntPtr.Zero, pt.X, pt.Y, 0, 0, Win32Consts.SWP_NOZORDER | Win32Consts.SWP_NOSIZE | Win32Consts.SWP_SHOWWINDOW);
        }
        /// <summary>
        /// 将窗口居中
        /// </summary>
        /// <param name="aBoundOffset">1920*1080分辨率下、offset是2</param>
        /// <returns></returns>
        public bool SetWindowPositionToCenter(int aBoundOffset = 15)
        {
            try
            {
                //Rectangle screen = Screen.FromHandle(HWnd).Bounds;
                Rectangle screen = Screen.PrimaryScreen.Bounds;

                var allowMin = 0 - aBoundOffset;
                var allowMaxWidth = screen.Width + aBoundOffset;
                var allowMaxHeight = screen.Height + aBoundOffset;
                if (_windowRect.Left < allowMin || _windowRect.Top < allowMin || _windowRect.Right > allowMaxWidth || _windowRect.Bottom > allowMaxHeight)
                {
                    Point pt = new Point(screen.Left + screen.Width / 2 - (_windowRect.Right - _windowRect.Left) / 2, screen.Top + screen.Height / 2 - (_windowRect.Bottom - _windowRect.Top) / 2);
                    Win32Funcs.SetWindowPosWrapper(HWnd, IntPtr.Zero, pt.X, pt.Y, 0, 0, Win32Consts.SWP_NOZORDER | Win32Consts.SWP_NOSIZE | Win32Consts.SWP_SHOWWINDOW);
                    return true;
                }
                else
                {
                    return false;
                }

            }
            catch
            {
                ///ignore
                return false;
            }

        }
    }
    public static class Win32Funcs
    {
        public static uint GetDpiForWindow(IntPtr hWnd) => (uint)II.RPA.Compatibility.WindowsDpi.ForWindow(hWnd);
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int width, int height,
            uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("User32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("User32.dll", SetLastError = true)]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass,
            string lpszWindow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out Win32Types.Rect lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out Win32Types.Rect lpRect);

        [DllImport("User32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref Win32Types.Point pt);

        [DllImport("User32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref Win32Types.Point pt);

        [DllImport("User32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref Win32Types.Rect lpRect);

        [DllImport("user32.dll", EntryPoint = "GetWindowDC")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hDc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hDc);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern int UpdateLayeredWindow(IntPtr hWnd, IntPtr hdcDst, ref Win32Types.Point pptDst,
            ref Win32Types.Size psize, IntPtr hdcSrc, ref Win32Types.Point pptSrc, uint crKey,
            ref Win32Types.BlendFunction pblend, uint dwFlags);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hDc, IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hDc);

        [DllImport("user32.dll")]
        private static extern IntPtr ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateDIBSection(IntPtr hdc, ref Win32Types.BitmapInfo bmi,
            uint usage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

        [DllImport("gdiplus.dll", SetLastError = true)]
        private static extern int GdipCreateBitmapFromGdiDib(IntPtr bmpInfo, IntPtr pixData, out IntPtr image);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool BitBlt(
            IntPtr hObject, int nXDest, int nYDest, int nWidth, int nHeight,
            IntPtr hObjectSource, int nXSrc, int nYSrc, uint dwRop);

        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr window, Win32Types.EnumWindowProc callback, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(Win32Types.EnumWindowProc callback, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, ref Win32Types.CopyDataStruct cds);

        [DllImport("user32.dll")]
        private static extern int PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, ref Win32Types.CopyDataStruct cds);

        [DllImport("user32.dll")]
        private static extern int SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("Ws2_32.dll")]
        private static extern long WSAGetLastError();

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("Kernel32.dll")]
        private static extern uint GetLastError();

        [DllImport("kernel32.dll")]
        private static extern void SetLastError(uint dwErrCode);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr GlobalLock(IntPtr handle);

        [DllImport("user32.dll")]
        private static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

        [DllImport("user32.dll")]
        private static extern void LockWorkStation();

        [DllImport("PowrProf.dll", CharSet = CharSet.Auto)]
        private static extern bool SetSuspendState(bool hiberate, bool forceCritical, bool disableWakeEvent);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern long SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        //This is the Import for the SetWindowsHookEx function.
        //Use this function to install a thread-specific hook.
        [DllImport("user32.dll", CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        private static extern int SetWindowsHookEx(int idHook, Win32Types.HookProc lpfn,
            IntPtr hInstance, int threadId);

        //This is the Import for the UnhookWindowsHookEx function.
        //Call this function to uninstall the hook.
        [DllImport("user32.dll", CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        private static extern bool UnhookWindowsHookEx(int idHook);

        //This is the Import for the CallNextHookEx function.
        //Use this function to pass the hook information to the next hook procedure in chain.
        [DllImport("user32.dll", CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        private static extern int CallNextHookEx(int idHook, int nCode,
            IntPtr wParam, IntPtr lParam);

        public static int GetWindowLongWrapper(IntPtr hWnd, int nIndex)
        {
            return GetWindowLong(hWnd, nIndex);
        }

        public static int SetWindowLongWrapper(IntPtr hWnd, int nIndex, int dwNewLong)
        {
            return SetWindowLong(hWnd, nIndex, dwNewLong);
        }

        public static bool SetWindowPosWrapper(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int width, int height,
            uint flags)
        {
            return SetWindowPos(hWnd, hWndInsertAfter, x, y, width, height, flags);
        }

        public static bool MoveWindowWrapper(IntPtr hWnd, int x, int y, int width, int height, bool repaint)
        {
            return MoveWindow(hWnd, x, y, width, height, repaint);
        }

        public static bool SetForegroundWindowWrapper(IntPtr hWnd)
        {
            return SetForegroundWindow(hWnd);
        }

        public static IntPtr SetFocusWrapper(IntPtr hWnd)
        {
            return SetFocus(hWnd);
        }

        public static IntPtr FindWindowWrapper(string lpClassName, string lpWindowName)
        {
            return FindWindow(lpClassName, lpWindowName);
        }

        public static IntPtr GetDesktopWindowWrapper()
        {
            return GetDesktopWindow();
        }

        public static IntPtr FindWindowExWrapper(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass,
            string lpszWindow)
        {
            return FindWindowEx(hwndParent, hwndChildAfter, lpszClass, lpszWindow);
        }

        public static bool ShowWindowWrapper(IntPtr hWnd, int nCmdShow)
        {
            return ShowWindow(hWnd, nCmdShow);
        }

        public static bool GetWindowRectWrapper(IntPtr hWnd, out Win32Types.Rect lpRect)
        {
            return GetWindowRect(hWnd, out lpRect);
        }

        public static bool GetClientRectWrapper(IntPtr hWnd, out Win32Types.Rect lpRect)
        {
            return GetClientRect(hWnd, out lpRect);
        }

        public static bool ClientToScreenWrapper(IntPtr hWnd, ref Win32Types.Point pt)
        {
            return ClientToScreen(hWnd, ref pt);
        }

        public static bool ScreenToClientWrapper(IntPtr hWnd, ref Win32Types.Point pt)
        {
            return ScreenToClient(hWnd, ref pt);
        }

        public static bool ScreenToClientWrapper(IntPtr hWnd, ref Win32Types.Rect lpRect)
        {
            return ScreenToClient(hWnd, ref lpRect);
        }

        public static IntPtr GetWindowDcWrapper(IntPtr hWnd)
        {
            return GetWindowDC(hWnd);
        }

        public static IntPtr GetDcWrapper(IntPtr hWnd)
        {
            return GetDC(hWnd);
        }

        public static IntPtr CreateCompatibleBitmapWrapper(IntPtr hDc, int nWidth, int nHeight)
        {
            return CreateCompatibleBitmap(hDc, nWidth, nHeight);
        }

        public static IntPtr CreateCompatibleDcWrapper(IntPtr hDc)
        {
            return CreateCompatibleDC(hDc);
        }

        public static int UpdateLayeredWindowWrapper(IntPtr hWnd, IntPtr hdcDst, ref Win32Types.Point pptDst,
            ref Win32Types.Size psize, IntPtr hdcSrc, ref Win32Types.Point pptSrc, uint crKey,
            ref Win32Types.BlendFunction pblend, uint dwFlags)
        {
            return UpdateLayeredWindow(hWnd, hdcDst, ref pptDst, ref psize, hdcSrc, ref pptSrc, crKey, ref pblend, dwFlags);
        }

        public static IntPtr SelectObjectWrapper(IntPtr hDc, IntPtr hObject)
        {
            return SelectObject(hDc, hObject);
        }

        public static bool DeleteDcWrapper(IntPtr hDc)
        {
            return DeleteDC(hDc);
        }

        public static IntPtr ReleaseDcWrapper(IntPtr hwnd, IntPtr hdc)
        {
            return ReleaseDC(hwnd, hdc);
        }

        public static bool DeleteObjectWrapper(IntPtr hObject)
        {
            return DeleteObject(hObject);
        }

        public static IntPtr CreateDibSectionWrapper(IntPtr hdc, ref Win32Types.BitmapInfo bmi,
            uint usage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset)
        {
            return CreateDIBSection(hdc, ref bmi, usage, out ppvBits, hSection, dwOffset);
        }

        public static int GdipCreateBitmapFromGdiDibWrapper(IntPtr bmpInfo, IntPtr pixData, out IntPtr image)
        {
            return GdipCreateBitmapFromGdiDib(bmpInfo, pixData, out image);
        }

        public static bool BitBltWrapper(
            IntPtr hObject, int nXDest, int nYDest, int nWidth, int nHeight,
            IntPtr hObjectSource, int nXSrc, int nYSrc, uint dwRop)
        {
            return BitBlt(hObject, nXDest, nYDest, nWidth, nHeight,
                hObjectSource, nXSrc, nYSrc, dwRop);
        }

        public static bool PrintWindowWrapper(IntPtr hwnd, IntPtr hdcBlt, uint nFlags)
        {
            return PrintWindow(hwnd, hdcBlt, nFlags);
        }

        public static bool EnumChildWindowsWrapper(IntPtr window, Win32Types.EnumWindowProc callback, IntPtr lParam)
        {
            return EnumChildWindows(window, callback, lParam);
        }

        public static bool EnumWindowsWrapper(Win32Types.EnumWindowProc callback, IntPtr lParam)
        {
            return EnumWindows(callback, lParam);
        }

        public static int GetWindowTextWrapper(IntPtr hWnd, StringBuilder lpString, int nMaxCount)
        {
            return GetWindowText(hWnd, lpString, nMaxCount);
        }

        public static int GetClassNameWrapper(IntPtr hWnd, StringBuilder lpString, int nMaxCount)
        {
            return GetClassName(hWnd, lpString, nMaxCount);
        }

        public static int SendMessageWrapper(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            return SendMessage(hWnd, msg, wParam, lParam);
        }

        public static int SendMessageWrapper(IntPtr hWnd, uint msg, IntPtr wParam, ref Win32Types.CopyDataStruct cds)
        {
            return SendMessage(hWnd, msg, wParam, ref cds);
        }

        public static int PostMessageWrapper(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            return PostMessage(hWnd, msg, wParam, lParam);
        }

        public static int PostMessageWrapper(IntPtr hWnd, uint msg, IntPtr wParam, ref Win32Types.CopyDataStruct cds)
        {
            return PostMessage(hWnd, msg, wParam, ref cds);
        }

        public static int SetLayeredWindowAttributesWrapper(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags)
        {
            return SetLayeredWindowAttributes(hWnd, crKey, bAlpha, dwFlags);
        }

        public static long WsaGetLastErrorWrapper()
        {
            return WSAGetLastError();
        }

        public static void SetLastErrorWrapper(uint dwErrCode)
        {
            SetLastError(dwErrCode);
        }

        public static bool SetProcessDpiAwareWrapper()
        {
            return SetProcessDPIAware();
        }

        public static uint GetLastErrorWrapper()
        {
            return GetLastError();
        }

        public static IntPtr GlobalLockWrapper(IntPtr handle)
        {
            return GlobalLock(handle);
        }

        public static bool ExitWindowsExWrapper(uint uFlags, uint dwReason)
        {
            return ExitWindowsEx(uFlags, dwReason);
        }

        public static void LockWorkStationWrapper()
        {
            LockWorkStation();
        }

        public static bool SetSuspendStateWrapper(bool hiberate, bool forceCritical, bool disableWakeEvent)
        {
            return SetSuspendState(hiberate, forceCritical, disableWakeEvent);
        }

        public static long SetParentWrapper(IntPtr hWndChild, IntPtr hWndNewParent)
        {
            return SetParent(hWndChild, hWndNewParent);
        }

        public static IntPtr GetParentWrapper(IntPtr hWnd)
        {
            return GetParent(hWnd);
        }

        public static int GetWindowThreadProcessIdWrapper(IntPtr hWnd, out int lpdwProcessId)
        {
            return GetWindowThreadProcessId(hWnd, out lpdwProcessId);
        }

        public static int GetDeviceCapsWrapper(IntPtr hdc, int nIndex)
        {
            return GetDeviceCaps(hdc, nIndex);
        }

        public static int SetWindowsHookExWrapper(int idHook, Win32Types.HookProc lpfn,
            IntPtr hInstance, int threadId)
        {
            return SetWindowsHookEx(idHook, lpfn, hInstance, threadId);
        }

        public static bool UnhookWindowsHookExWrapper(int idHook)
        {
            return UnhookWindowsHookEx(idHook);
        }

        public static int CallNextHookExWrapper(int idHook, int nCode,
            IntPtr wParam, IntPtr lParam)
        {
            return CallNextHookEx(idHook, nCode, wParam, lParam);
        }
    }
    public class Win32Types
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
            public int x;
            public int y;

            public Point(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int Left;    //最左坐标
            public int Top;     //最上坐标
            public int Right;   //最右坐标
            public int Bottom;  //最下坐标

            public int Width => Right - Left;

            public int Height => Bottom - Top;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Size
        {
            public int cx;
            public int cy;

            public Size(int x, int y)
            {
                cx = x;
                cy = y;
            }
        }

        #region BITMAP
        [StructLayout(LayoutKind.Sequential)]
        public struct BitmapInfoHeader
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;

            public void Init()
            {
                biSize = (uint)Marshal.SizeOf(this);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct RgbQuad
        {
            public byte rgbBlue;
            public byte rgbGreen;
            public byte rgbRed;
            public byte rgbReserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BitmapInfo
        {
            public BitmapInfoHeader bmiHeader;
            public RgbQuad bmiColors;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        public struct BitmapFileHeader
        {
            public ushort bfType;
            public uint bfSize;
            public ushort bfReserved1;
            public ushort bfReserved2;
            public uint bfOffBits;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Bitmap
        {
            public int bmType;
            public int bmWidth;
            public int bmHeight;
            public int bmWidthBytes;
            public ushort bmPlanes;
            public ushort bmBitsPixel;
            public IntPtr bmBits;
        }
        #endregion

        [StructLayout(LayoutKind.Sequential)]
        public struct SystemInfo
        {
            public ushort ProcessorArchitecture;
            public ushort Reserved;
            public uint PageSize;
            public IntPtr MinimumApplicationAddress;
            public IntPtr MaximumApplicationAddress;
            public IntPtr ActiveProcessorMask;
            public uint NumberOfProcessors;
            public uint ProcessorType;
            public uint AllocationGranularity;
            public ushort ProcessorLevel;
            public ushort ProcessorRevision;
            public uint OemId => ((uint)ProcessorArchitecture << 8) | Reserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BlendFunction
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CopyDataStruct
        {
            public uint dwData;
            public uint cbData;
            public IntPtr lpData;
        }

        public delegate bool EnumWindowProc(IntPtr hwnd, IntPtr lParam);
        public delegate IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        public delegate int HookProc(int nCode, IntPtr wParam, IntPtr lParam);
    }
    public class Win32Consts
    {
        public enum DibColorMode : uint
        {
            DIB_RGB_COLORS = 0x00,
            DIB_PAL_COLORS = 0x01,
            DIB_PAL_INDICES = 0x02
        }

        public enum BitmapCompressionMode : uint
        {
            BI_RGB = 0,
            BI_RLE8 = 1,
            BI_RLE4 = 2,
            BI_BITFIELDS = 3,
            BI_JPEG = 4,
            BI_PNG = 5
        }

        public enum RasterOperationMode : uint
        {
            SRCCOPY = 0x00CC0020,
            SRCPAINT = 0x00EE0086,
            SRCAND = 0x008800C6,
            SRCINVERT = 0x00660046,
            SRCERASE = 0x00440328,
            NOTSRCCOPY = 0x00330008,
            NOTSRCERASE = 0x001100A6,
            MERGECOPY = 0x00C000CA,
            MERGEPAINT = 0x00BB0226,
            PATCOPY = 0x00F00021,
            PATPAINT = 0x00FB0A09,
            PATINVERT = 0x005A0049,
            DSTINVERT = 0x00550009,
            BLACKNESS = 0x00000042,
            WHITENESS = 0x00FF0062,
            CAPTUREBLT = 0x40000000 //only if WinVer >= 5.0.0 (see wingdi.h)
        }

        public enum PrintWindowMode : uint
        {
            [Description(
                "Only the client area of the window is copied to hdcBlt. By default, the entire window is copied.")]
            PW_CLIENTONLY = 0x00000001,

            [Description("works on windows that use DirectX or DirectComposition")]
            PW_RENDERFULLCONTENT = 0x00000002
        }

        public const int GWL_WNDPROC = -4;
        public const int GWL_HINSTANCE = -6;
        public const int GWL_HWNDPARENT = -8;
        public const int GWL_STYLE = -16;
        public const int GWL_EXSTYLE = -20;
        public const int GWL_USERDATA = -21;
        public const int GWL_ID = -12;

        /*
         * System Menu Command Values
         */
        public const int SC_SIZE = 0xF000;
        public const int SC_MOVE = 0xF010;
        public const int SC_MINIMIZE = 0xF020;
        public const int SC_MAXIMIZE = 0xF030;
        public const int SC_NEXTWINDOW = 0xF040;
        public const int SC_PREVWINDOW = 0xF050;
        public const int SC_CLOSE = 0xF060;
        public const int SC_VSCROLL = 0xF070;
        public const int SC_HSCROLL = 0xF080;
        public const int SC_MOUSEMENU = 0xF090;
        public const int SC_KEYMENU = 0xF100;
        public const int SC_ARRANGE = 0xF110;
        public const int SC_RESTORE = 0xF120;
        public const int SC_TASKLIST = 0xF130;
        public const int SC_SCREENSAVE = 0xF140;
        public const int SC_HOTKEY = 0xF150;

        /*
         * Window Styles
         */
        public const long WS_OVERLAPPED = 0x00000000L;
        public const long WS_POPUP = 0x80000000L;
        public const long WS_CHILD = 0x40000000L;
        public const long WS_MINIMIZE = 0x20000000L;
        public const long WS_VISIBLE = 0x10000000L;
        public const long WS_DISABLED = 0x08000000L;
        public const long WS_CLIPSIBLINGS = 0x04000000L;
        public const long WS_CLIPCHILDREN = 0x02000000L;
        public const long WS_MAXIMIZE = 0x01000000L;
        public const long WS_CAPTION = 0x00C00000L;     /* WS_BORDER | WS_DLGFRAME  */
        public const long WS_BORDER = 0x00800000L;
        public const long WS_DLGFRAME = 0x00400000L;
        public const long WS_VSCROLL = 0x00200000L;
        public const long WS_HSCROLL = 0x00100000L;
        public const long WS_SYSMENU = 0x00080000L;
        public const long WS_THICKFRAME = 0x00040000L;
        public const long WS_GROUP = 0x00020000L;
        public const long WS_TABSTOP = 0x00010000L;

        public const long WS_MINIMIZEBOX = 0x00020000L;
        public const long WS_MAXIMIZEBOX = 0x00010000L;

        public const long WS_TILED = WS_OVERLAPPED;
        public const long WS_ICONIC = WS_MINIMIZE;
        public const long WS_SIZEBOX = WS_THICKFRAME;
        public const long WS_TILEDWINDOW = WS_OVERLAPPEDWINDOW;

        /*
         * ShowWindow() Commands
         */
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

        /*
         * Common Window Styles
         */
        public const long WS_OVERLAPPEDWINDOW = (WS_OVERLAPPED |
                                                 WS_CAPTION |
                                                 WS_SYSMENU |
                                                 WS_THICKFRAME |
                                                 WS_MINIMIZEBOX |
                                                 WS_MAXIMIZEBOX);

        public const long WS_POPUPWINDOW = (WS_POPUP |
                                            WS_BORDER |
                                            WS_SYSMENU);

        public const long WS_CHILDWINDOW = (WS_CHILD);

        public const long WS_EX_TOPMOST = 0x00000008L; //窗口置顶(停留在所有非最高层窗口的上面)
        public const int WS_EX_LAYERED = 0x00080000; //分层或透明窗口,该样式可使用混合特效
        public const long WS_EX_NOACTIVATE = 0x08000000L; //处于顶层但不激活

        //窗口Z序标识（如果uFlags参数中设置了SWP_NOZORDER标记则本参数将被忽略）
        public const int HWND_TOP = 0;
        public const int HWND_BOTTOM = 1;
        public const int HWND_TOPMOST = -1;
        public const int HWND_NOTOPMOST = -2;
        public static readonly IntPtr HWND_BOTTOM_PTR = new IntPtr(HWND_BOTTOM);
        public static readonly IntPtr HWND_TOP_PTR = new IntPtr(HWND_TOP);
        public static readonly IntPtr HWND_TOPMOST_PTR = new IntPtr(HWND_TOPMOST);
        public static readonly IntPtr HWND_NOTOPMOST_PTR = new IntPtr(HWND_NOTOPMOST);

        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOREDRAW = 0x0008;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_FRAMECHANGED = 0x0020;    /* The frame changed: send WM_NCCALCSIZE */
        public const uint SWP_SHOWWINDOW = 0x0040;
        public const uint SWP_HIDEWINDOW = 0x0080;
        public const uint SWP_NOCOPYBITS = 0x0100;
        public const uint SWP_NOOWNERZORDER = 0x0200;   /* Don't do owner Z ordering */
        public const uint SWP_NOSENDCHANGING = 0x0400;  /* Don't send WM_WINDOWPOSCHANGING */

        public const int SE_SHUTDOWN_PRIVILEGE = 0x13;

        public const int WM_ACTIVATEAPP = 0x001C;
        public const int WM_ACTIVATE = 0x0006;
        public const uint WM_SETFOCUS = 0x0007;
        public const uint WM_KILLFOCUS = 0x0008;
        public const uint WM_ENABLE = 0x000A;
        public const uint WM_SETREDRAW = 0x000B;
        public const uint WM_SETTEXT = 0x000C;
        public const uint WM_GETTEXT = 0x000D;
        public const uint WM_GETTEXTLENGTH = 0x000E;
        public const uint WM_PAINT = 0x000F;
        public const uint WM_CLOSE = 0x0010;
        public const uint WM_INITDIALOG = 0x0110;
        public const uint WM_COMMAND = 0x0111;
        public const uint WM_SYSCOMMAND = 0x0112;
        public const uint WM_TIMER = 0x0113;
        public const uint WM_HSCROLL = 0x0114;
        public const uint WM_VSCROLL = 0x0115;
        public const uint WM_INITMENU = 0x0116;
        public const uint WM_INITMENUPOPUP = 0x0117;

        public const uint LWA_COLORKEY = 0x00000001;
        public const uint LWA_ALPHA = 0x00000002;

        public const uint ULW_COLORKEY = 0x00000001;
        public const uint ULW_ALPHA = 0x00000002;
        public const uint ULW_OPAQUE = 0x00000004;

        //
        // currentlly defined blend function
        //
        public const byte AC_SRC_OVER = 0x00;

        //
        // alpha format flags
        //
        public const byte AC_SRC_ALPHA = 0x01;

        /// <summary>
        /// An existing connection was forcibly closed by the remote host.
        /// </summary>
        public const long WSAECONNRESET = 10054L;

        /* Device Parameters for GetDeviceCaps() */
        public const int DRIVERVERSION = 0;     /* Device driver version                    */
        public const int TECHNOLOGY = 2;        /* Device classification                    */
        public const int HORZSIZE = 4;          /* Horizontal size in millimeters           */
        public const int VERTSIZE = 6;          /* Vertical size in millimeters             */
        public const int HORZRES = 8;           /* Horizontal width in pixels               */
        public const int VERTRES = 10;          /* Vertical height in pixels                */
        public const int BITSPIXEL = 12;        /* Number of bits per pixel                 */
        public const int PLANES = 14;           /* Number of planes                         */
        public const int NUMBRUSHES = 16;       /* Number of brushes the device has         */
        public const int NUMPENS = 18;          /* Number of pens the device has            */
        public const int NUMMARKERS = 20;       /* Number of markers the device has         */
        public const int NUMFONTS = 22;         /* Number of fonts the device has           */
        public const int NUMCOLORS = 24;        /* Number of colors the device supports     */
        public const int PDEVICESIZE = 26;      /* Size required for device descriptor      */
        public const int CURVECAPS = 28;        /* Curve capabilities                       */
        public const int LINECAPS = 30;         /* Line capabilities                        */
        public const int POLYGONALCAPS = 32;    /* Polygonal capabilities                   */
        public const int TEXTCAPS = 34;         /* Text capabilities                        */
        public const int CLIPCAPS = 36;         /* Clipping capabilities                    */
        public const int RASTERCAPS = 38;       /* Bitblt capabilities                      */
        public const int ASPECTX = 40;          /* Length of the X leg                      */
        public const int ASPECTY = 42;          /* Length of the Y leg                      */
        public const int ASPECTXY = 44;         /* Length of the hypotenuse                 */

        public const int LOGPIXELSX = 88;       /* Logical pixels/inch in X                 */
        public const int LOGPIXELSY = 90;       /* Logical pixels/inch in Y                 */

        public const int SIZEPALETTE = 104;     /* Number of entries in physical palette    */
        public const int NUMRESERVED = 106;     /* Number of reserved entries in palette    */
        public const int COLORRES = 108;        /* Actual color resolution                  */

        // Printing related DeviceCaps. These replace the appropriate Escapes

        public const int PHYSICALWIDTH = 110;   /* Physical Width in device units           */
        public const int PHYSICALHEIGHT = 111;  /* Physical Height in device units          */
        public const int PHYSICALOFFSETX = 112; /* Physical Printable Area x margin         */
        public const int PHYSICALOFFSETY = 113; /* Physical Printable Area y margin         */
        public const int SCALINGFACTORX = 114;  /* Scaling factor x                         */
        public const int SCALINGFACTORY = 115;  /* Scaling factor y                         */

        // Display driver specific

        public const int VREFRESH = 116;        /* Current vertical refresh rate of the    */
        /* display device (for displays only) in Hz*/
        public const int DESKTOPVERTRES = 117;  /* Horizontal width of entire desktop in   */
        /* pixels                                  */
        public const int DESKTOPHORZRES = 118;  /* Vertical height of entire desktop in    */
        /* pixels                                  */
        public const int BLTALIGNMENT = 119;    /* Preferred blt alignment                 */

        #region (WINVER >= 0x0500)
        public const int SHADEBLENDCAPS = 120;  /* Shading and blending caps               */
        public const int COLORMGMTCAPS = 121;   /* Color Management caps                   */
        #endregion /* WINVER >= 0x0500 */

        public const int WH_MSGFILTER = -1;
        public const int WH_JOURNALRECORD = 0;
        public const int WH_JOURNALPLAYBACK = 1;
        public const int WH_KEYBOARD = 2;

        public const int WH_GETMESSAGE = 3;

        //Installs a hook procedure that monitors messages before the system
        //sends them to the destination window procedure. For more information,
        //see the CallWndProc hook procedure.
        public const int WH_CALLWNDPROC = 4;

        public const int WH_CBT = 5;

        //Installs a hook procedure that monitors messages generated as a result of an input event
        //in a dialog box, message box, menu, or scroll bar. The hook procedure monitors these
        //messages for all applications in the same desktop as the calling thread. For more information,
        //see the SysMsgProc hook procedure.
        public const int WH_SYSMSGFILTER = 6;
        public const int WH_MOUSE = 7;
        public const int WH_DEBUG = 9;
        public const int WH_SHELL = 10;
        public const int WH_FOREGROUNDIDLE = 11;
        public const int WH_CALLWNDPROCRET = 12;
        public const int WH_KEYBOARD_LL = 13;
        public const int WH_MOUSE_LL = 14;

        public const uint WM_COPYDATA = 0x004A;
        public const uint WM_CANCELJOURNAL = 0x004B;
    }

    public class PrimaryScreen
    {
        #region Win32 API  
        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr ptr);
        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(
        IntPtr hdc, // handle to DC  
        int nIndex // index of capability  
        );
        [DllImport("user32.dll", EntryPoint = "ReleaseDC")]
        static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDc);
        #endregion
        #region DeviceCaps常量  
        const int HORZRES = 8;
        const int VERTRES = 10;
        const int LOGPIXELSX = 88;
        const int LOGPIXELSY = 90;
        const int DESKTOPVERTRES = 117;
        const int DESKTOPHORZRES = 118;
        #endregion

        #region 属性  
        /// <summary>  
        /// 获取屏幕分辨率当前物理大小  
        /// </summary>  
        public static Size WorkingArea
        {
            get
            {
                IntPtr hdc = GetDC(IntPtr.Zero);
                Size size = new Size();
                size.Width = GetDeviceCaps(hdc, HORZRES);
                size.Height = GetDeviceCaps(hdc, VERTRES);
                ReleaseDC(IntPtr.Zero, hdc);
                return size;
            }
        }
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
        /// <summary>  
        /// 当前系统DPI_Y 大小 一般为96  
        /// </summary>  
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
        /// 获取真实设置的桌面分辨率大小  
        /// </summary>  
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
                float ScaleX = (float)GetDeviceCaps(hdc, DESKTOPHORZRES) / (float)GetDeviceCaps(hdc, HORZRES);
                ReleaseDC(IntPtr.Zero, hdc);
                return ScaleX;
            }
        }
        /// <summary>  
        /// 获取高度缩放百分比  
        /// </summary>  
        public static float ScaleY
        {
            get
            {
                IntPtr hdc = GetDC(IntPtr.Zero);
                float ScaleY = (float)(float)GetDeviceCaps(hdc, DESKTOPVERTRES) / (float)GetDeviceCaps(hdc, VERTRES);
                ReleaseDC(IntPtr.Zero, hdc);
                return ScaleY;
            }
        }
        #endregion
    }
}
