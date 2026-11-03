
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
    using System.Diagnostics;
    #endregion

    public class Ntdll
    {
        [System.Runtime.InteropServices.DllImport("NTDLL.DLL", SetLastError = true)]
        public static extern int NtQueryInformationProcess(IntPtr hProcess, PROCESSINFOCLASS pic, ref PROCESS_BASIC_INFORMATION pbi, int cb, out int pSize);
        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, IntPtr processInformation, int processInformationLength, ref int returnLength);

        private const int ProcessBasicInformation = 0;
        private const int ProcessCommandLineInformation = 60;
        public static string GetCommandLineOfProcess(int processId)
        {
            try
            {
                Process process = Process.GetProcessById(processId);
                IntPtr processHandle = process.Handle;

                int length = 0;
                NtQueryInformationProcess(processHandle, ProcessCommandLineInformation, IntPtr.Zero, 0, ref length);

                IntPtr ptr = Marshal.AllocHGlobal(length);
                try
                {
                    int status = NtQueryInformationProcess(processHandle, ProcessCommandLineInformation, ptr, length, ref length);
                    if (status != 0)
                    {
                        return null;
                    }

                    UnicodeString unicodeString = Marshal.PtrToStructure<UnicodeString>(ptr);
                    return Marshal.PtrToStringUni(unicodeString.Buffer);
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return null;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct UnicodeString
        {
            public ushort Length;
            public ushort MaximumLength;
            public IntPtr Buffer;
        }
    }
}
