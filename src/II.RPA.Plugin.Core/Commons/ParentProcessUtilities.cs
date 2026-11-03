using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Core.Commons
{
    public struct ParentProcessUtilities
    {
        internal IntPtr Reserved1;

        internal IntPtr PebBaseAddress;

        internal IntPtr Reserved2_0;

        internal IntPtr Reserved2_1;

        internal IntPtr UniqueProcessId;

        internal IntPtr InheritedFromUniqueProcessId;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        // PROCESS_QUERY_LIMITED_INFORMATION = 0x1000，权限要求更低
        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref ParentProcessUtilities processInformation, int processInformationLength, out int returnLength);

        public static string GetParentProcessName(Process process)
        {
            using (var parentProcess = GetParentProcess(process, "cmd"))
                return parentProcess?.ProcessName.ToLower() ?? string.Empty;
        }

        public static Process GetParentProcess(Process process, string ignoreParent)
        {
            var parentProcess = GetParentProcess(process.Handle);
            if (parentProcess != null && parentProcess.ProcessName == ignoreParent)
            {
                var parentProcess2 = ParentProcessUtilities.GetParentProcess(parentProcess);
                parentProcess.Dispose();
                parentProcess = parentProcess2;
            }
            return parentProcess;
        }

        public static Process GetParentProcess(Process process)
        {
            return GetParentProcess(process.Id);
        }

        public static Process GetParentProcess(int id)
        {
            try
            {
                IntPtr processHandle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, id);
                if (processHandle == IntPtr.Zero)
                {
                    // 如果 OpenProcess 失败，尝试使用 Process.GetProcessById（可能也会失败，但保持向后兼容）
                    try
                    {
                        return GetParentProcess(Process.GetProcessById(id).Handle);
                    }
                    catch (Win32Exception)
                    {
                        // 权限不足，返回 null
                        return null;
                    }
                    catch (ArgumentException)
                    {
                        // 进程不存在，返回 null
                        return null;
                    }
                }

                try
                {
                    return GetParentProcess(processHandle);
                }
                finally
                {
                    CloseHandle(processHandle);
                }
            }
            catch (Win32Exception)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public static Process GetParentProcess(IntPtr handle)
        {
            ParentProcessUtilities processInformation = default(ParentProcessUtilities);
            int returnLength;
            int num = NtQueryInformationProcess(handle, 0, ref processInformation, Marshal.SizeOf(processInformation), out returnLength);
            if (num != 0)
            {
                //throw new Win32Exception(num);
                return null;
            }
            try
            {
                int parentProcessId = processInformation.InheritedFromUniqueProcessId.ToInt32();
                if (parentProcessId == 0)
                {
                    return null;
                }

                IntPtr parentHandle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, parentProcessId);
                if (parentHandle != IntPtr.Zero)
                {
                    try
                    {
                        CloseHandle(parentHandle);
                        return Process.GetProcessById(parentProcessId);
                    }
                    catch (Win32Exception)
                    {
                        return null;
                    }
                    catch (ArgumentException)
                    {
                        return null;
                    }
                }
                else
                {
                    try
                    {
                        return Process.GetProcessById(parentProcessId);
                    }
                    catch (Win32Exception)
                    {
                        return null;
                    }
                    catch (ArgumentException)
                    {
                        return null;
                    }
                }
            }
            catch (ArgumentException)
            {
                //throw new Exception("BrowserTypeHelper.GetParentProcess Error");
                return null;
            }
            catch (Win32Exception)
            {
                return null;
            }
        }

    }
}
