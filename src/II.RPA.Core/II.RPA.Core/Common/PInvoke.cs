using System;
using System.Runtime.InteropServices;

namespace II.RPA.Core.Common
{
    public static class PInvoke
    {
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenThread(
            ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll")]
        public static extern uint ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        public static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            PROCESS_QUERY_LIMITED_INFORMATION = 0x00001000,
        }
        public static bool HasExited(int processId)
        {
            var hProcess = OpenProcess(ProcessAccessFlags.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);

            GetExitCodeProcess(hProcess, out uint ExitCode);
            if (ExitCode != 259) // ExitCode of 259 is STILL_ACTIVE
            {
                CloseHandle(hProcess);
                return true;
            }
            CloseHandle(hProcess);
            return false;
        }
    }
    [Flags]
    public enum ThreadAccess : int
    {
        TERMINATE = (0x0001),
        SUSPEND_RESUME = (0x0002),
        GET_CONTEXT = (0x0008),
        SET_CONTEXT = (0x0010),
        SET_INFORMATION = (0x0020),
        QUERY_INFORMATION = (0x0040),
        SET_THREAD_TOKEN = (0x0080),
        IMPERSONATE = (0x0100),
        DIRECT_IMPERSONATION = (0x0200)
    }
}
