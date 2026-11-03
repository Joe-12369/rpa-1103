
namespace II.RPA.WinAPI
{
    #region import
    using Microsoft.Win32.SafeHandles;
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using II.RPA.WinAPI.Struct;
    using System.Runtime.InteropServices.ComTypes;
    using System.Drawing;
    using System.Security;
    using System.Collections.Generic;
    #endregion
    public class User32Extension
    { 
        private class EnumWindowsProcData
        {
            public List<IntPtr> hWndList { get; set; }
            public Func<IntPtr, bool> Predicate { get; set; }
        }
        private static bool EnumAllProc(IntPtr hWnd, int lParam)
        {
            GCHandle procDataHandle = GCHandle.FromIntPtr(new IntPtr(lParam));
            if (procDataHandle.Target == null)
            {
                return false;
            }
            User32Extension.EnumWindowsProcData procData = procDataHandle.Target as User32Extension.EnumWindowsProcData;
            if (procData.Predicate(hWnd))
            {
                procData.hWndList.Add(hWnd);
            }
            return true;
        }
        public static List<IntPtr> QueryAllChildWindows(IntPtr hwndParent, Func<IntPtr, bool> predicate)
        {
            User32Extension.EnumWindowsProcData procData = new User32Extension.EnumWindowsProcData
            {
                hWndList = new List<IntPtr>(),
                Predicate = predicate
            };
            GCHandle procDataHandle = GCHandle.Alloc(procData);
            IntPtr procDataPtr = GCHandle.ToIntPtr(procDataHandle);
            try
            {
                User32.EnumChildWindows(hwndParent, new Struct.EnumWindowsProc(User32Extension.EnumAllProc), procDataPtr);
            }
            finally
            {
                procDataHandle.Free();
            }
            return procData.hWndList;
        }
    }
    public class User32
    {
        [DllImport("user32.dll")]
        public static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetGUIThreadInfo(uint hTreadID, ref GUITHREADINFO lpgui);
        // When you don't want the ProcessId, use this overload and pass 
        // IntPtr.Zero for the second parameter
        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);
        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        public static extern bool AttachThreadInput(uint idAttach,
            uint idAttachTo, bool fAttach);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool BringWindowToTop(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool BringWindowToTop(HandleRef hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr WindowFromPoint(int xPoint, int yPoint);

        [DllImport("user32.dll", EntryPoint = "GetParent", SetLastError = true)]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder strText, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool SetProcessDPIAware();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetDC(IntPtr ptr);

        /// <summary>
        /// SetWindowPos Flags
        /// </summary>
        public static class SWP
        {
            public static readonly uint
            NOSIZE = 0x0001,
            NOMOVE = 0x0002,
            NOZORDER = 0x0004,
            NOREDRAW = 0x0008,
            NOACTIVATE = 0x0010,
            DRAWFRAME = 0x0020,
            FRAMECHANGED = 0x0020,
            SHOWWINDOW = 0x0040,
            HIDEWINDOW = 0x0080,
            NOCOPYBITS = 0x0100,
            NOOWNERZORDER = 0x0200,
            NOREPOSITION = 0x0200,
            NOSENDCHANGING = 0x0400,
            DEFERERASE = 0x2000,
            ASYNCWINDOWPOS = 0x4000;
        }
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndlnsertAfter, int X, int Y, int cx, int cy, uint Flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool AttachThreadInput(int idAttach, int idAttachTo, int fAttach);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int RegisterWindowMessage(string message);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessageTimeout(IntPtr hWnd, int uMsg, IntPtr wParam, IntPtr lParam, int fuFlags, int uTimeout, out UIntPtr lpdwResult);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "CloseWindow", SetLastError = true)]
        public static extern bool CloseWindow(IntPtr hWnd);

        #region ShowWindow 方法窗体状态的参数枚举
        /// <summary>
        /// 隐藏窗口并激活其他窗口
        /// </summary>
        public const int SW_HIDE = 0;

        /// <summary>
        /// 激活并显示一个窗口。如果窗口被最小化或最大化，系统将其恢复到原来的尺寸和大小。应用程序在第一次显示窗口的时候应该指定此标志
        /// </summary>
        public const uint SW_SHOWNORMAL = 1;

        /// <summary>
        /// 激活窗口并将其最小化
        /// </summary>
        public const uint SW_SHOWMINIMIZED = 2;
        /// <summary>
        /// 激活窗口并将其最大化
        /// </summary>
        public const uint SW_SHOWMAXIMIZED = 3;

        /// <summary>
        /// 以窗口最近一次的大小和状态显示窗口。此值与SW_SHOWNORMAL相似，只是窗口没有被激活
        /// </summary>
        public const uint SW_SHOWNOACTIVATE = 4;

        /// <summary>
        /// 在窗口原来的位置以原来的尺寸激活和显示窗口
        /// </summary>
        public const uint SW_SHOW = 5;

        /// <summary>
        /// 最小化指定的窗口并且激活在Z序中的下一个顶层窗口
        /// </summary>
        public const uint SW_MINIMIZE = 6;

        /// <summary>
        /// 最小化的方式显示窗口，此值与SW_SHOWMINIMIZED相似，只是窗口没有被激活
        /// </summary>
        public const uint SW_SHOWMINNOACTIVE = 7;

        /// <summary>
        /// 以窗口原来的状态显示窗口。此值与SW_SHOW相似，只是窗口没有被激活
        /// </summary>
        public const uint SW_SHOWNA = 8;

        /// <summary>
        /// 激活并显示窗口。如果窗口最小化或最大化，则系统将窗口恢复到原来的尺寸和位置。在恢复最小化窗口时，应用程序应该指定这个标志
        /// </summary>
        public const uint SW_RESTORE = 9;

        /// <summary>
        /// 依据在STARTUPINFO结构中指定的SW_FLAG标志设定显示状态，STARTUPINFO 结构是由启动应用程序的程序传递给CreateProcess函数的
        /// </summary>
        public const uint SW_SHOWDEFAULT = 10;

        /// <summary>
        /// 最小化窗口，即使拥有窗口的线程被挂起也会最小化。在从其他线程最小化窗口时才使用这个参数
        /// </summary>
        public const uint SW_FORCEMINIMIZE = 11;
        #endregion

        [DllImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]
        public static extern IntPtr ShowWindow(IntPtr hWnd, uint nCmdShow);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        public static extern long GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        public static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        public static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, long dwNewLong);

        [DllImport("user32")]
        public static extern int UpdateWindow(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetDoubleClickTime();

        [DllImport("user32.dll", EntryPoint = "GetUserObjectSecurity")]
        public static extern int GetUserObjectSecurity([System.Runtime.InteropServices.InAttribute()] IntPtr hObj,
            [In] ref uint pSIRequested, System.IntPtr pSID, uint nLength, [System.Runtime.InteropServices.OutAttribute()] out uint lpnLengthNeeded);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsImmersiveProcess(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowsHookEx(Int32 idHook, LLProc lpfn, IntPtr hMod, UInt32 dwThreadId);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, Int32 nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern Boolean UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        public static extern Boolean GetPhysicalCursorPos(ref POINT pt);

        [DllImport("user32.dll")]
        public static extern Int16 GetKeyState(Int32 nVirtKey);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "IDE1006")]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        public static extern UInt32 SendInput(UInt32 numberOfInputs, INPUT[] inputs, Int32 sizeOfInputStructure);

        [DllImport("user32.dll")]
        public static extern UInt16 VkKeyScanEx(Char ch, IntPtr dwhkl);

        [DllImport("user32.dll")]
        public static extern IntPtr GetKeyboardLayout(UInt32 idThread);

        [DllImport("user32.dll")]
        public static extern Boolean SetPhysicalCursorPos(Int32 X, Int32 Y);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowPlacement(
            IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        public static extern IntPtr GetFocus();

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex,
            int dwNewLong);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        public static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        [DllImport("User32.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern IntPtr LoadCursorFromFile(String str);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);

        [DllImport("user32")]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool EnumChildWindows(IntPtr hWndStart, EnumWindowsProc callback, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "GetWindowTextLength", SetLastError = true)]
        public static extern int GetWindowTextLength(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern bool ClientToScreen(IntPtr hwnd, ref Point lpPoint);

        [DllImport("user32.dll")]
        public static extern bool ScreenToClient(IntPtr hwnd, ref Point lpPoint);

        [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint nCmd);

        [DllImport("user32.dll", EntryPoint = "ReleaseDC")]
        public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDc);

        [DllImport("user32.dll")]
        public static extern bool SetSystemCursor(IntPtr hcur, uint id);

        [DllImport("user32.dll")]
        public static extern IntPtr LoadCursor(IntPtr hInstance, uint lpCursorName);

        [DllImport("user32.dll")]
        public static extern IntPtr CopyIcon(IntPtr pcur);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern Int32 SystemParametersInfo(UInt32 uiAction,
            UInt32 uiParam, String pvParam, UInt32 fWinIni);

        [DllImport("user32.dll")]
        public static extern IntPtr LoadCursorFromFileW(string fileName);

        [DllImport("user32.dll")]
        public static extern IntPtr SetCursor(IntPtr cursorHandle);

        [DllImport("user32.dll")]
        public static extern uint DestroyCursor(IntPtr cursorHandle);

        [DllImport("user32", EntryPoint = "SetLayeredWindowAttributes")]
        public static extern int SetLayeredWindowAttributes(
        IntPtr hwnd,
        int crKey,
        int bAlpha,
        int dwFlags
        );

        public static int GetDpiForWindow(IntPtr hwnd) => II.RPA.Compatibility.WindowsDpi.ForWindow(hwnd);

        public static int GetDpiForSystem() => II.RPA.Compatibility.WindowsDpi.ForSystem();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetActiveWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string lclassName, string windowTitle);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int ObjectFromLresult(
        int lResult,
        ref System.Guid riid,
        int wParam,
        [System.Runtime.InteropServices.MarshalAs(UnmanagedType.Interface), System.Runtime.InteropServices.In, System.Runtime.InteropServices.Out] ref System.Object ppvObject
        //注意这个函数ObjectFromLresult的声明.
        );

        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr ptr);

        [DllImport("user32.dll")]
        public static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hWnd, out Win32Types.Rect lpRect);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern int UpdateLayeredWindow(IntPtr hWnd, IntPtr hdcDst, ref Win32Types.Point pptDst,
            ref Win32Types.Size psize, IntPtr hdcSrc, ref Win32Types.Point pptSrc, uint crKey,
            ref Win32Types.BlendFunction pblend, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern void LockWorkStation();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern long SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(int uCode, int uMapType);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern short VkKeyScan(char ch);

        [DllImport("user32", EntryPoint = "IsWindow")]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowEnabled(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetWindowLong", SetLastError = true)]
        public static extern uint GetWindowLongPtr32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        public static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool IsWindowVisible(int hWnd);

        [DllImport("user32", CharSet = CharSet.Auto)]
        public static extern IntPtr GetTopWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

    }
}
