using Accessibility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace II.RPA.SetWinEventHook
{
    static class Program
    {
        public static string GetProcessOwner(int processId)
        {
            try
            {
                foreach (ManagementObject obj in new ManagementObjectSearcher("Select * From Win32_Process Where ProcessID = " + processId).Get())
                {
                    string[] argList = new string[2]
                    {
                    string.Empty,
                    string.Empty
                    };
                    object[] args = argList;
                    if (Convert.ToInt32(obj.InvokeMethod("GetOwner", args)) == 0)
                    {
                        return argList[0];
                    }
                }
            }
            catch (Exception e)
            {

                return "NO OWNER";
            }
            return "NO OWNER";
        }
        public static Process[] GetProcessesByName(string processName, bool onlyCurrentUser)
        {
            try
            {
                if (!onlyCurrentUser)
                {
                    return Process.GetProcessesByName(processName);
                }
                return (from m in Process.GetProcessesByName(processName)
                        where GetProcessOwner(m.Id) == Environment.UserName
                        select m).ToArray();
            }
            catch (Exception e)
            {

            }
            return null;
        }
        private static void KillPrevHost()
        {
            Process[] processesByName = GetProcessesByName("II.RPA.SetWinEventHook", onlyCurrentUser: true);
            int curPid = Process.GetCurrentProcess().Id;
            Process[] array = processesByName;
            foreach (Process process in array)
            {
                if (process.Id != curPid)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                    }
                }
            }
        }

        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, WinEventFlag dwFlags);
        [DllImport("oleacc.dll")]
        public static extern uint AccessibleObjectFromEvent(IntPtr hwnd, uint dwObjectID, uint dwChildID, out IAccessible ppacc, [MarshalAs(UnmanagedType.Struct)] out object pvarChild);

        [Flags]
        public enum WinEventFlag : uint
        {
            /// <summary>Events are ASYNC</summary>
            Outofcontext = 0x0000,
            /// <summary>Don't call back for events on installer's thread</summary>
            Skipownthread = 0x0001,
            /// <summary>Don't call back for events on installer's process</summary>
            Skipownprocess = 0x0002,
            /// <summary>Events are SYNC, this causes your dll to be injected into every process</summary>
            Incontext = 0x0004
        }
        static IntPtr hookHandle = IntPtr.Zero;
        static GCHandle garbageCollectorHandle;

        static void AccessibilityEventHandler(IntPtr hWinEventHook, uint eventType, IntPtr hWnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            try
            {
                object objChild = null;
                IAccessible accWindow = null;
                AccessibleObjectFromEvent(hWnd, (uint)idObject, (uint)idChild, out accWindow, out objChild);
                //Debug.WriteLine("Hook was invoked");
            }
            catch (Exception ex)
            {
                //MessageBox.Show("2");
                // Debug.WriteLine("Exception " + ex);
            }
        }
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            KillPrevHost();
            WinEventDelegate callback = new WinEventDelegate(AccessibilityEventHandler);
            // Use the member garbageCollectorHandle to keep the delegate object in memory. Might not be needed, and can't properly pin it because it's not a primitive type.
            garbageCollectorHandle = GCHandle.Alloc(callback);
            SetWinEventHook(
                0x0002, // eventMin (0x0001 = System_Sound)
                0x0002, // eventMax
                IntPtr.Zero,
                callback,
                0,
                0,
                WinEventFlag.Outofcontext | WinEventFlag.Skipownprocess
            );
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new NoShowForm());
        }
    }
}
