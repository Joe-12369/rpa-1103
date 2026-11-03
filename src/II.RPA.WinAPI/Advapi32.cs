
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

    public class Advapi32
    {
		[DllImport("advapi32.dll", SetLastError = true)]        [return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out IntPtr TokenHandle);

		[DllImport("advapi32.dll", SetLastError = true)]
		public static extern bool GetTokenInformation(IntPtr TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, IntPtr TokenInformation, uint TokenInformationLength, out uint ReturnLength);

		[DllImport("advapi32", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern bool ConvertSidToStringSid(IntPtr pSID, out IntPtr ptrSid);

		[DllImport("advapi32.dll", SetLastError = true)]
		public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint Bufferlength, IntPtr PreviousState, IntPtr ReturnLength);

		[DllImport("advapi32.dll", SetLastError = true)]
		public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

		[DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx", SetLastError = true)]
		public static extern bool DuplicateTokenEx(IntPtr hExistingToken, uint dwDesiredAccess, ref SECURITY_ATTRIBUTES lpThreadAttributes, Int32 ImpersonationLevel,
            Int32 dwTokenType, ref IntPtr phNewToken);

		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public         static extern bool CreateProcessAsUser(IntPtr hToken, string lpApplicationName, string lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes,
    ref SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory,
    ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

		[DllImport("advapi32.dll", SetLastError = true, PreserveSig = true)]
		public static extern uint LsaRetrievePrivateData(IntPtr PolicyHandle, ref LSA_UNICODE_STRING KeyName, out IntPtr PrivateData);

		[DllImport("advapi32.dll", SetLastError = true, PreserveSig = true)]
		public static extern uint LsaStorePrivateData(IntPtr policyHandle, ref LSA_UNICODE_STRING KeyName, ref LSA_UNICODE_STRING PrivateData);

		[DllImport("advapi32.dll", SetLastError = true, PreserveSig = true)]
		public static extern uint LsaOpenPolicy(ref LSA_UNICODE_STRING SystemName, ref LSA_OBJECT_ATTRIBUTES ObjectAttributes, uint DesiredAccess, out IntPtr PolicyHandle);

		[DllImport("advapi32.dll", SetLastError = true, PreserveSig = true)]
		public static extern int LsaNtStatusToWinError(uint status);

		[DllImport("advapi32.dll", SetLastError = true, PreserveSig = true)]
		public static extern uint LsaClose(IntPtr policyHandle);

		[DllImport("advapi32.dll", SetLastError = true, PreserveSig = true)]
		public static extern uint LsaFreeMemory(IntPtr buffer);


    }
}
