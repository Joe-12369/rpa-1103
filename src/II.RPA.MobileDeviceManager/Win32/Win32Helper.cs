using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileDeviceManager.Win32
{
    public class Win32Helper
    {
        public const int WS_CHILD = unchecked((int)0x40000000);
        public const int WS_VISIBLE = 0x10000000;
        public const int WS_CLIPSIBLINGS = 0x04000000;
        public const int WS_CLIPCHILDREN = 0x02000000;
        public const int WS_POPUP = unchecked((int)0x80000000);
        public const int WS_CAPTION = 0x00C00000;
        public const int WS_THICKFRAME = 0x00040000;
        public const int WS_SYSMENU = 0x00080000;
        public const int WS_MINIMIZEBOX = 0x00020000;
        public const int WS_MAXIMIZEBOX = 0x00010000;
        public const int WS_WINDOW_DECORATION = WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;

        [System.Flags]
        public enum SetWindowPosFlags : uint
        {
            SWP_NOSIZE = 0x0001,
            SWP_NOMOVE = 0x0002,
            SWP_NOZORDER = 0x0004,
            SWP_NOACTIVATE = 0x0010,
            SWP_FRAMECHANGED = 0x0020,
            SWP_SHOWWINDOW = 0x0040,
            SWP_NOOWNERZORDER = 0x0200,
        }

        [DllImport("User32", ExactSpelling = true, EntryPoint = "FindWindowW", SetLastError = true)]
        public static extern HWND FindWindow(PWSTR lpClassName, PCWSTR lpWindowName);
        public static unsafe HWND FindWindow(string lpClassName, string lpWindowName)
        {
            fixed (char* lpWindowNameLocal = lpWindowName)
            {
                fixed (char* lpClassNameLocal = lpClassName)
                {
                    HWND __result = FindWindow(lpClassNameLocal, lpWindowNameLocal);
                    return __result;
                }
            }
        }

        [DllImport("User32", ExactSpelling = true, EntryPoint = "GetWindowThreadProcessId", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(HWND hWnd, out uint processId);

        [DllImport("User32", ExactSpelling = true, SetLastError = true)]
        public static extern HWND SetParent(HWND hWndChild, HWND hWndNewParent);
        [DllImport("User32", ExactSpelling = true, EntryPoint = "GetWindowLongW", SetLastError = true)]
        public static extern int GetWindowLong(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex);
        [DllImport("User32", ExactSpelling = true, EntryPoint = "SetWindowLongW", SetLastError = true)]
        public static extern int SetWindowLong(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, int dwNewLong);
        [DllImport("User32", ExactSpelling = true, SetLastError = true)]
        public static extern BOOL SetWindowPos(HWND hWnd, HWND hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("User32", ExactSpelling = true, SetLastError = true)]
        public static extern BOOL MoveWindow(HWND hWnd, int X, int Y, int nWidth, int nHeight, BOOL bRepaint);
        [DllImport("User32", ExactSpelling = true, SetLastError = true)]
        public static extern bool IsWindow(HWND hWnd);
        [DllImport("User32", ExactSpelling = true, SetLastError = true)]
        public static extern bool IsWindowVisible(HWND hWnd);
        [DllImport("User32", ExactSpelling = true, SetLastError = true)]
        public static extern BOOL GetClientRect(HWND hWnd, out RECT lpRect);
        [DllImport("User32", ExactSpelling = true, SetLastError = true)]
        public static extern unsafe BOOL GetWindowRect(HWND hWnd, RECT* lpRect);
        public static unsafe BOOL GetWindowRect(HWND hWnd, out RECT lpRect)
        {
            fixed (RECT* lpRectLocal = &lpRect)
            {
                BOOL __result = GetWindowRect(hWnd, lpRectLocal);
                return __result;
            }
        }
    }
}
