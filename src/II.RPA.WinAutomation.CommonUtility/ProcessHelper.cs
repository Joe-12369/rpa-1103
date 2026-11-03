using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WinAutomation.CommonUtility
{
    public class ProcessHelper
    {
        public static bool ExistProcess(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length > 0)
                return true;
            else
                return false;
        }

        #region 根据进程Id获取所有子进程id
        [DllImport("KERNEL32.DLL ")]
        public static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint processid);
        [DllImport("KERNEL32.DLL ")]
        public static extern int CloseHandle(IntPtr handle);
        [DllImport("KERNEL32.DLL ")]
        public static extern int Process32First(IntPtr handle, ref ProcessEntry32 pe);
        [DllImport("KERNEL32.DLL ")]
        public static extern int Process32Next(IntPtr handle, ref ProcessEntry32 pe);
        [StructLayout(LayoutKind.Sequential)]
        public struct ProcessEntry32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExeFile;
        };
        public static List<int> GetListChildProcessesId(int parentProcessId)
        {
            List<int> processesIds = new List<int>();
            var list = new List<ProcessEntry32>();
            IntPtr handle = CreateToolhelp32Snapshot(0x2, 0);
            if ((int)handle > 0)
            {
                ProcessEntry32 pe32 = new ProcessEntry32();
                pe32.dwSize = (uint)Marshal.SizeOf(pe32);

                int bMore = Process32First(handle, ref pe32);
                while (bMore == 1)
                {
                    IntPtr temp = Marshal.AllocHGlobal((int)pe32.dwSize);
                    Marshal.StructureToPtr(pe32, temp, true);
                    ProcessEntry32 pe = (ProcessEntry32)Marshal.PtrToStructure(temp, typeof(ProcessEntry32));
                    Marshal.FreeHGlobal(temp);
                    list.Add(pe);
                    bMore = Process32Next(handle, ref pe32);
                }
                CloseHandle(handle);
                foreach (ProcessEntry32 p in list)
                {
                    if (p.th32ParentProcessID == parentProcessId)
                    {
                        processesIds.Add((int)p.th32ProcessID);
                    }
                }
            }
            return processesIds;
        }
        #endregion
    }
}
