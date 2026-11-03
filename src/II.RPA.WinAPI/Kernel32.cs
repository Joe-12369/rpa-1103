
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
    #endregion

    public class Kernel32
    {
        public const int OF_READWRITE = 2;
        public const int OF_SHARE_DENY_NONE = 0x40;
        public static readonly IntPtr HFILE_ERROR = new IntPtr(-1);
 
        [DllImport("kernel32.dll")]
        public static extern IntPtr _lopen(string lpPathName, int iReadWrite); 
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int LoadLibrary(string lpLibFileName);

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern SafeFileHandle CreateFile(
            string fileName,
            [MarshalAs(UnmanagedType.U4)] FileAccess fileAccess,
            //UInt32 fileAccess,
            [MarshalAs(UnmanagedType.U4)] FileShare fileShare,
            IntPtr securityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
            //[MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
            uint flagsAndAttributes,
            IntPtr template);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool ReadFileEx(
            SafeFileHandle hFile,
            [Out] byte[] lpbuffer,
            [In] uint nNumberOfBytesToRead,
            [In, Out] ref NativeOverlapped lpOverlapped,
            IntPtr lpCompletionRoutine);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll")]
        public static extern IntPtr LocalFree(IntPtr hMem);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean CloseHandle(IntPtr hObject);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern Boolean AllocConsole();

        [DllImport("kernel32.dll", EntryPoint = "LocalFree", SetLastError = true)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "IDE1006")]
        public static extern IntPtr _LocalFree(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint GetCurrentThreadId();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetBinaryType(
            string lpApplicationName,
            out BinaryType dwBinType
            );

        [DllImport("kernel32.dll")]
        public static extern int GetApplicationUserModelId(
            IntPtr hProcess,
            ref uint AppModelIDLength,
            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder sbAppUserModelID);

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool QueryFullProcessImageName(IntPtr hProcess, uint flags, StringBuilder exeName, ref uint nameLen);

        [DllImport("Kernel32.dll")]
        public static extern uint GetLastError();

        [DllImport("kernel32.dll")]
        public static extern void SetLastError(uint dwErrCode);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        public static extern IntPtr GlobalLock(IntPtr handle);

        [DllImport("Kernel32.dll", CharSet = CharSet.Ansi)]
        public static extern bool CreateProcess(
          StringBuilder lpApplicationName, StringBuilder lpCommandLine,
          SECURITY_ATTRIBUTES lpProcessAttributes,
          SECURITY_ATTRIBUTES lpThreadAttributes,
          bool bInheritHandles,
          int dwCreationFlags,
          StringBuilder lpEnvironment,
          StringBuilder lpCurrentDirectory,
          ref STARTUPINFO lpStartupInfo,
          ref PROCESS_INFORMATION lpProcessInformation
          );

        [DllImport("Kernel32.dll")]
        public static extern uint WaitForSingleObject(System.IntPtr hHandle, uint dwMilliseconds);

        [DllImport("Kernel32.dll")]
        public static extern bool GetExitCodeProcess(System.IntPtr hProcess, ref uint lpExitCode);

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern uint WTSGetActiveConsoleSessionId();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, IntPtr dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] buffer, uint size, int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttribute, IntPtr dwStackSize, IntPtr lpStartAddress,
            IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);


    }
}
