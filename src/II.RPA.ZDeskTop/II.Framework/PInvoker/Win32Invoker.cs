using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace II.Framework.PInvoker
{
    public class WindowPInvoker
    {
        private readonly CaptureProvider _captureProvider;
        private readonly nint _hWnd;
        public WindowPInvoker(nint hWnd)
        {
            _captureProvider = new CaptureProvider();
            _hWnd = hWnd;
        }

        public WindowPInvoker SetMiniWindow(int left, int top, int width, int height)
        {
            SetWindowPos(_hWnd, HWND_TOPMOST, left, top, width, height, SWP_SHOWWINDOW);
            int exStyle = GetWindowLong(_hWnd, GWL_EXSTYLE).ToInt32();
            exStyle &= ~WS_EX_APPWINDOW;
            exStyle |= WS_EX_TOOLWINDOW;
            SetWindowLong(_hWnd, GWL_EXSTYLE, exStyle);
            return this;
        }
        public WindowPInvoker SetMiniWindow(int left, int top, int width, int height,nint parent)
        {
            SetWindowPos(_hWnd, HWND_TOPMOST, left, top, width, height, SWP_SHOWWINDOW);
            int exStyle = GetWindowLong(_hWnd, GWL_EXSTYLE).ToInt32();
            exStyle &= ~WS_EX_APPWINDOW;
            exStyle |= WS_EX_TOOLWINDOW;
            SetWindowLong(_hWnd, GWL_EXSTYLE, exStyle);
            SetParent(_hWnd, parent);
            return this;
        }
        public WindowPInvoker ResetWindow(int left, int top, int width, int height)
        {
            int exStyle = GetWindowLong(_hWnd, GWL_EXSTYLE).ToInt32();
            SetWindowLong(_hWnd, GWL_EXSTYLE, exStyle & ~WS_EX_TOOLWINDOW);
            SetWindowPos(_hWnd, HWND_NOTOPMOST, left, top, width, height, SWP_SHOWWINDOW);
            return this;
        }

        public WindowPInvoker SetPosition(int left, int top, int width, int height)
        {
            SetWindowPos(_hWnd, HWND_NOTOPMOST, left, top, width, height, SWP_SHOWWINDOW);
            return this;
        }
        public WindowPInvoker HideWindow()
        {
            ShowWindow(_hWnd, SW_HIDE);
            return this;
        }
        public WindowPInvoker ShowWindow()
        {
            ShowWindow(_hWnd, SW_SHOW);
            return this;
        }
        public WindowPInvoker CaptureWindow(out Bitmap bitmap)
        {
            WindowPInvoker.UpdateWindow(_hWnd);
            _captureProvider.Capture(_hWnd, out bitmap);
            return this;
        }
        public WindowPInvoker CaptureWindowUsingBitBlt(bool captureTitlebar, out Bitmap bitmap)
        {
            var displayScalingFactor = GetDisplayScalingFactor(_hWnd);
            Rect targetWindowRectangle;
            if (captureTitlebar)
            {
                GetWindowRect(_hWnd, out targetWindowRectangle);
            }
            else
            {
                GetClientRect(_hWnd, out targetWindowRectangle);
            }
            targetWindowRectangle.Left = Convert.ToInt32(targetWindowRectangle.Left * displayScalingFactor);
            targetWindowRectangle.Right = Convert.ToInt32(targetWindowRectangle.Right * displayScalingFactor);
            targetWindowRectangle.Top = Convert.ToInt32(targetWindowRectangle.Top * displayScalingFactor);
            targetWindowRectangle.Bottom = Convert.ToInt32(targetWindowRectangle.Bottom * displayScalingFactor);

            nint hWndDc = GetDC(_hWnd);
            nint hMemDc = CreateCompatibleDC(hWndDc);
            nint hBitmap = CreateCompatibleBitmap(hWndDc, targetWindowRectangle.Right, targetWindowRectangle.Bottom);
            SelectObject(hMemDc, hBitmap);

            BitBlt(hMemDc, 0, 0, targetWindowRectangle.Right, targetWindowRectangle.Bottom, hWndDc, 0, 0, TernaryRasterOperations.SRCCOPY | TernaryRasterOperations.CAPTUREBLT);
            bitmap = Bitmap.FromHbitmap(hBitmap);
            DeleteObject(hBitmap);
            ReleaseDC(_hWnd, hWndDc);
            if (!DeleteDC(hMemDc))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            return this;
        }
        public WindowPInvoker CloseWindow()
        {
            IntPtr result = SendMessage(_hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            if (result != IntPtr.Zero)
            {
                Console.WriteLine("WM_CLOSE 消息已成功发送到窗口。");
            }
            else
            {
                int errorCode = Marshal.GetLastWin32Error();
                Console.WriteLine($"无法发送 WM_CLOSE 消息。错误代码: {errorCode}");
            }
            return this;
        }
        public WindowPInvoker Sleep(int timeMs)
        {
            Thread.Sleep(timeMs);
            return this;
        }
        public WindowPInvoker GetWindowText(out string text)
        {
            var length = GetWindowTextLength(_hWnd);
            StringBuilder stringBuilder = new(length + 1);
            GetWindowText(_hWnd, stringBuilder, stringBuilder.Capacity);
            if (stringBuilder.Length > 0)
            {
                text = stringBuilder.ToString();
            }
            else
            {
                text = "";
            }
            return this;
        }
        public WindowPInvoker SetForeground()
        {
            SetForegroundWindow(_hWnd);
            return this;
        }
        public WindowPInvoker Output(Action<string> outPut,string text)
        {
            outPut(text);
            return this;
        }
        public WindowPInvoker OutputToConsole(string text)
        {
            Console.WriteLine(text);
            return this;
        }




        private static decimal GetDisplayScalingFactor(nint hwnd)
        {
            var screen = Screen.FromHandle(hwnd);

            var dm = new DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));

            EnumDisplaySettings(screen.DeviceName, -1, ref dm);

            var scalingFactor = Math.Round(Decimal.Divide(dm.dmPelsWidth, screen.Bounds.Width), 2);

            return scalingFactor;
        }
        private static void CreatProcess(string exePath, string arguments)
        {
            // Prepare the STARTUPINFO structure
            STARTUPINFO si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            si.dwFlags = 0;
            si.wShowWindow = 0;  // SW_HIDE

            // Prepare the PROCESS_INFORMATION structure
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();

            // Call CreateProcess
            bool result = CreateProcess(
                null,  // Use lpCommandLine for the full path and arguments
                $"\"{exePath}\" {arguments}",
                IntPtr.Zero,  // Default process security attributes
                IntPtr.Zero,  // Default thread security attributes
                false,  // Do not inherit handles
                0,  // No window
                IntPtr.Zero,  // Use parent's environment block
                null,  // Use the current directory
                ref si,
                out pi
            );

            if (result)
            {
                Console.WriteLine($"Process started with ID: {pi.dwProcessId}");

                // Wait for the process to exit
                WaitForSingleObject(pi.hProcess, INFINITE);  // Wait indefinitely
                CloseHandle(pi.hProcess);
                CloseHandle(pi.hThread);
            }
            else
            {
                int errorCode = Marshal.GetLastWin32Error();
                Console.WriteLine($"Failed to start process. Error code: {errorCode}");
            }
        }

        #region PInvokes
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_APPWINDOW = 0x00040000;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_HIDEWINDOW = 0x0080;
        private const int SW_MINIMIZE = 6;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private static readonly nint HWND_TOPMOST = new IntPtr(-1);
        private static readonly nint HWND_NOTOPMOST = new IntPtr(-2);
        private const uint CREATE_NO_WINDOW = 0x08000000;
        private const uint INFINITE = 0xFFFFFFFF;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);
        private const uint WM_CLOSE = 0x0010;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        public static extern bool UpdateWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern int SetForegroundWindow(IntPtr hwnd);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hdc);

        [DllImport("gdi32.dll", EntryPoint = "DeleteDC")]
        public static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, TernaryRasterOperations dwRop);

        [DllImport("User32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint nFlags);

        [DllImport("gdi32.dll", ExactSpelling = true, PreserveSig = true, SetLastError = true)]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hWnd, out Rect lpRect);
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out Rect rect);
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll")]
        public static extern int GetWindowTextLength(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();
        [DllImport("user32.dll")]
        public static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);
        public enum TernaryRasterOperations : uint
        {
            /// <summary>dest = source</summary>
            SRCCOPY = 0x00CC0020,
            /// <summary>dest = source OR dest</summary>
            SRCPAINT = 0x00EE0086,
            /// <summary>dest = source AND dest</summary>
            SRCAND = 0x008800C6,
            /// <summary>dest = source XOR dest</summary>
            SRCINVERT = 0x00660046,
            /// <summary>dest = source AND (NOT dest)</summary>
            SRCERASE = 0x00440328,
            /// <summary>dest = (NOT source)</summary>
            NOTSRCCOPY = 0x00330008,
            /// <summary>dest = (NOT src) AND (NOT dest)</summary>
            NOTSRCERASE = 0x001100A6,
            /// <summary>dest = (source AND pattern)</summary>
            MERGECOPY = 0x00C000CA,
            /// <summary>dest = (NOT source) OR dest</summary>
            MERGEPAINT = 0x00BB0226,
            /// <summary>dest = pattern</summary>
            PATCOPY = 0x00F00021,
            /// <summary>dest = DPSnoo</summary>
            PATPAINT = 0x00FB0A09,
            /// <summary>dest = pattern XOR dest</summary>
            PATINVERT = 0x005A0049,
            /// <summary>dest = (NOT dest)</summary>
            DSTINVERT = 0x00550009,
            /// <summary>dest = BLACK</summary>
            BLACKNESS = 0x00000042,
            /// <summary>dest = WHITE</summary>
            WHITENESS = 0x00FF0062,
            /// <summary>
            /// Capture window as seen on screen.  This includes layered windows
            /// such as WPF windows with AllowsTransparency="true"
            /// </summary>
            CAPTUREBLT = 0x40000000
        }
        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct DEVMODE
        {
            private const int CCHDEVICENAME = 0x20;
            private const int CCHFORMNAME = 0x20;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public ScreenOrientation dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct STARTUPINFO
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
        struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation
        );
        #endregion
    }
}
