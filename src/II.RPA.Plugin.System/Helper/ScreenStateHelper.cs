using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.RPASystem.Helper
{
    public class ScreenStateHelper
    {
        [DllImport("Wtsapi32.dll", CharSet = CharSet.Unicode)]
        public static extern bool WTSQuerySessionInformationW(IntPtr hServer, uint SessionId, WTS_INFO_CLASS WTSInfoClass, ref IntPtr ppBuffer, ref uint pBytesReturned);
        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern uint WTSGetActiveConsoleSessionId();
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SystemParametersInfo(uint uAction,
                                                          uint uParam,
                                                          ref bool lpvParam,
                                                          int fWinIni);

        [DllImport("user32", SetLastError = true)]
        private static extern IntPtr OpenDesktop(string lpszDesktop,
                                                 uint dwFlags,
                                                 bool fInherit,
                                                 uint dwDesiredAccess);

        [DllImport("user32", SetLastError = true)]
        private static extern IntPtr OpenInputDesktop(uint dwFlags,
                                                      bool fInherit,
                                                      uint dwDesiredAccess);

        [DllImport("user32", SetLastError = true)]
        private static extern IntPtr CloseDesktop(IntPtr hDesktop);

        [DllImport("user32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SwitchDesktop(IntPtr hDesktop);
        public static string GetScreenState()
        {
            const int DESKTOP_SWITCHDESKTOP = 256;
            IntPtr hwnd = OpenInputDesktop(0, false, DESKTOP_SWITCHDESKTOP);

            if (hwnd == IntPtr.Zero)
            {
                hwnd = OpenDesktop("Default", 0, false, DESKTOP_SWITCHDESKTOP);
            }

            if (hwnd != IntPtr.Zero)
            {
                if (SwitchDesktop(hwnd))
                {
                    CloseDesktop(hwnd);
                }
                else
                {
                    CloseDesktop(hwnd);
                    return "Locked";
                }
            }
            return "Not Locked";
        }
    }

    public enum WTS_INFO_CLASS
    {
        WTSInitialProgram,
        WTSApplicationName,
        WTSWorkingDirectory,
        WTSOEMId,
        WTSSessionId,
        WTSUserName,
        WTSWinStationName,
        WTSDomainName,
        WTSConnectState,
        WTSClientBuildNumber,
        WTSClientName,
        WTSClientDirectory,
        WTSClientProductId,
        WTSClientHardwareId,
        WTSClientAddress,
        WTSClientDisplay,
        WTSClientProtocolType,
        WTSIdleTime,
        WTSLogonTime,
        WTSIncomingBytes,
        WTSOutgoingBytes,
        WTSIncomingFrames,
        WTSOutgoingFrames,
        WTSClientInfo,
        WTSSessionInfo,
        WTSSessionInfoEx,
        WTSConfigInfo,
        WTSValidationInfo,   // Info Class value used to fetch Validation Information through the WTSQuerySessionInformation
        WTSSessionAddressV4,
        WTSIsRemoteSession
    }
    public enum WTS_CONNECTSTATE_CLASS
    {
        WTSActive,              // User logged on to WinStation
        WTSConnected,           // WinStation connected to client
        WTSConnectQuery,        // In the process of connecting to client
        WTSShadow,              // Shadowing another WinStation
        WTSDisconnected,        // WinStation logged on without client
        WTSIdle,                // Waiting for client to connect
        WTSListen,              // WinStation is listening for connection
        WTSReset,               // WinStation is being reset
        WTSDown,                // WinStation is down due to error
        WTSInit,                // WinStation in initialization
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct WTSINFOEXW
    {
        public int Level;
        public WTSINFOEX_LEVEL_W Data;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct WTSINFOEX_LEVEL_W
    {
        public WTSINFOEX_LEVEL1_W WTSInfoExLevel1;
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WTSINFOEX_LEVEL1_W
    {
        public int SessionId;
        public WTS_CONNECTSTATE_CLASS SessionState;
        public int SessionFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
        public string WinStationName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 21)]
        public string UserName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 18)]
        public string DomainName;
        public LARGE_INTEGER LogonTime;
        public LARGE_INTEGER ConnectTime;
        public LARGE_INTEGER DisconnectTime;
        public LARGE_INTEGER LastInputTime;
        public LARGE_INTEGER CurrentTime;
        public uint IncomingBytes;
        public uint OutgoingBytes;
        public uint IncomingFrames;
        public uint OutgoingFrames;
        public uint IncomingCompressedBytes;
        public uint OutgoingCompressedBytes;
    }
    [StructLayout(LayoutKind.Explicit)]
    public struct LARGE_INTEGER //此结构体在C++中使用的为union结构，在C#中需要使用FieldOffset设置相关的内存起始地址
    {
        [FieldOffset(0)]
        uint LowPart;
        [FieldOffset(4)]
        int HighPart;
        [FieldOffset(0)]
        long QuadPart;
    }
}
