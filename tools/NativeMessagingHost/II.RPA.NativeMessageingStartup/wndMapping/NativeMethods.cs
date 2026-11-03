using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using static II.RPA.NativeMessageingStartup.HostStartup;

namespace II.RPA.NativeMessageingStartup
{
    internal class NativeMethods
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string lclassName, string windowTitle);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        public static string GetWindowText(IntPtr ptr)
        {
            StringBuilder buffer = new StringBuilder(GetWindowTextLength(ptr));
            GetWindowText(ptr, buffer, buffer.Capacity + 1);
            var result = buffer.ToString();
            return result;
        }
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        public delegate bool EnumWindowsProc(IntPtr windowHandle, IntPtr lParam);
        [DllImport("user32.dll")]
        public static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc callback, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool IsWindowEnabled(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetClientRect(IntPtr hwnd, out RECT lpRect);
        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();
        [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr GetWindow(IntPtr hWnd, GET_WINDOW_CMD nCmd);
        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);
        [DllImport("user32", EntryPoint = "GetWindowLong")]
        public static extern uint GetWindowLong(IntPtr hwnd, int nIndex);
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
        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool QueryFullProcessImageName(IntPtr hProcess, uint flags, StringBuilder exeName, ref uint nameLen);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean CloseHandle(IntPtr hObject);

        public static readonly int GWL_WNDPROC = -4; // Retrieves the address of the window procedure, or a handle representing the address of the window procedure. You must use the CallWindowProc function to call the window procedure.
        public static readonly int GWL_HINSTANCE = -6;
        public static readonly int GWL_HWNDPARENT = -8;
        public static readonly int GWL_STYLE = -16; // Retrieves the window styles.
        public static readonly int GWL_EXSTYLE = -20; // Retrieves the extended window styles.
        public static readonly int GWL_USERDATA = -21; // Gets the 32-bit value associated with the window. Each window has a corresponding 32-bit value intended for use by the application that created the window. Windows CE versions 2.0 and later support this value, but Windows CE versions 1.0 and 1.01 do not.
        public static readonly int GWL_ID = -12; // Not supported.
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public static IEnumerable<T> MatchDesktopWindowInZorder<T>(Func<IntPtr, T> predicate)
        {
            var desktopWnd = NativeMethods.GetDesktopWindow();
            IntPtr hWnd = NativeMethods.GetWindow(desktopWnd, GET_WINDOW_CMD.GW_CHILD);
            while (hWnd != IntPtr.Zero)
            {
                if (NativeMethods.IsWindow(hWnd)
                    && NativeMethods.IsWindowVisible(hWnd)
                    && (NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_EXSTYLE) & NativeMethods.WS_EX_TOOLWINDOW) == 0
                )
                {
                    var ret = predicate.Invoke(hWnd);
                    if (!EqualityComparer<T>.Default.Equals(ret, default(T)))
                        yield return ret;
                }
                hWnd = NativeMethods.GetWindow(hWnd, GET_WINDOW_CMD.GW_HWNDNEXT);
            }
        }
        public static Rectangle GetWindowRect(IntPtr wndHandle)
        {
            if (NativeMethods.GetWindowRect(wndHandle, out var wndRect))
            {
                return new Rectangle(
                    wndRect.Left,
                    wndRect.Top,
                    wndRect.Right - wndRect.Left,
                    wndRect.Bottom - wndRect.Top
                    );
            }
            return Rectangle.Empty;
        }
        //public static string GetProcessNameByCache(IntPtr hWnd)
        //{
        //    if (!GlobleConfigurations.ElementAPPDic.TryGetValue(hWnd, out var processName))//首次获取窗口进程信息，保存进程名
        //    {
        //        GetWindowThreadProcessId(hWnd, out var processId);
        //        processName = GetProcessNameById((int)processId);
        //    }
        //    return processName.EndsWith(".exe") ? processName.Replace(".exe", "") : processName;
        //}
        public static Process GetProcessByHandle(IntPtr hWnd)
        {
            try
            {
                GetWindowThreadProcessId(hWnd, out var processId);
                return Process.GetProcessById((int)processId);
            }
            catch { return null; }
        }
        //public static string GetProcessNameById(int processId)
        //{
        //    IntPtr intPtr = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.QueryLimitedInformation, bInheritHandle: false, processId);
        //    if (intPtr != IntPtr.Zero)
        //    {
        //        StringBuilder stringBuilder = new StringBuilder(1024);
        //        uint nameLen = (uint)stringBuilder.Capacity;
        //        if (NativeMethods.QueryFullProcessImageName(intPtr, 0u, stringBuilder, ref nameLen))
        //        {
        //            NativeMethods.CloseHandle(intPtr);
        //            return Path.GetFileNameWithoutExtension(stringBuilder.ToString());
        //        }
        //        NativeMethods.CloseHandle(intPtr);
        //    }
        //    try
        //    {
        //        foreach (ManagementObject item in new ManagementObjectSearcher("Select * From Win32_Process Where ProcessID = " + processId).Get())
        //        {
        //            if (item["Name"] == null)
        //            {
        //                continue;
        //            }
        //            try
        //            {
        //                string text = item["Name"].ToString();
        //                if (!string.IsNullOrEmpty(text))
        //                {
        //                    return text.Substring(0, text.LastIndexOf('.'));
        //                }
        //            }
        //            catch
        //            {
        //            }
        //        }
        //    }
        //    catch (Exception)
        //    {
        //    }
        //    return Process.GetProcessById(processId).ProcessName;
        //}
    }
}
