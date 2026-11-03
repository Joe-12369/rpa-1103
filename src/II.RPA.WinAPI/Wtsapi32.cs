
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

    public class Wtsapi32
    {
		[DllImport("wtsapi32.dll", SetLastError = true)]
		public static extern bool WTSDisconnectSession(IntPtr hServer, int sessionId, bool bWait);

		[DllImport("wtsapi32.dll", SetLastError = true)]
		public static extern bool WTSLogoffSession(IntPtr hServer, int SessionId, bool bWait);

		[DllImport("wtsapi32.dll")]
		public static extern void WTSFreeMemory(IntPtr pMemory);

		[DllImport("wtsapi32.dll", SetLastError = true)]
		public static extern int WTSEnumerateSessions(
                System.IntPtr hServer,
                int Reserved,
                int Version,
                ref System.IntPtr ppSessionInfo,
                ref int pCount);

		[DllImport("wtsapi32.dll", SetLastError = true)]
		public static extern IntPtr WTSOpenServer(string pServerName);

		[DllImport("wtsapi32.dll")]
		public static extern void WTSCloseServer(IntPtr hServer);

		[DllImport("Wtsapi32.dll", CharSet = CharSet.Unicode)]
		public static extern bool WTSQuerySessionInformationW(IntPtr hServer, uint SessionId, WTS_INFO_CLASS WTSInfoClass, ref IntPtr ppBuffer, ref uint pBytesReturned);


    }
}
