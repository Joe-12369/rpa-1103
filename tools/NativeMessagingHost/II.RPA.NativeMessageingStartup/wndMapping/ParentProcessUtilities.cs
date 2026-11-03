using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.NativeMessageingStartup
{
    internal static class ParentProcessUtilities
    {
        internal static Process GetParent(Process process)
        {
            return GetParent(process.Handle);
        }
        internal static Process GetParent(IntPtr processHandle)
        {
            var pbi = new PROCESS_BASIC_INFORMATION();
            int size = 0;
            NtQueryInformationProcess(processHandle, 0, ref pbi, pbi.Size, ref size);
            try { return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32()); }
            catch { return null; }
        }
        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(
            IntPtr ProcessHandle, int ProcessInformationClass,
            ref PROCESS_BASIC_INFORMATION ProcessInformation,
            int ProcessInformationLength, ref int ReturnLength);

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr Reserved1;
            public IntPtr PebBaseAddress;
            public IntPtr Reserved2_0, Reserved2_1;
            public IntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;
            public int Size => Marshal.SizeOf(typeof(PROCESS_BASIC_INFORMATION));
        }

        // 获取父进程
        internal static ParentInfo GetParentInfo(Process proc)
        {
            ParentInfo parentInfo = null;
            try
            {
                using (var parent = ParentProcessUtilities.GetParent(proc))
                {
                    if (parent.ProcessName == "cmd")
                    {
                        using (var pParent = ParentProcessUtilities.GetParent(parent))
                        {
                            parentInfo = new ParentInfo(pParent.ProcessName, pParent.Id, pParent.StartTime);
                        }
                    }
                    else
                    {
                        parentInfo = new ParentInfo(parent.ProcessName, parent.Id, parent.StartTime);
                    }
                }
                return parentInfo;
            }
            catch
            {
                return new ParentInfo("unknown", -1, DateTime.MinValue);
            }

        }
    }
}
