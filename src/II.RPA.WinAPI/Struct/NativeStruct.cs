using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WinAPI.Struct
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct STARTUPINFO
    {
        public Int32 cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public Int32 dwX;
        public Int32 dwY;
        public Int32 dwXSize;
        public Int32 dwYSize;
        public Int32 dwXCountChars;
        public Int32 dwYCountChars;
        public Int32 dwFillAttribute;
        public Int32 dwFlags;
        public Int16 wShowWindow;
        public Int16 cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessId;
        public int dwThreadId;
    }
    public enum SECURITY_IMPERSONATION_LEVEL
    {
        SecurityAnonymous,
        SecurityIdentification,
        SecurityImpersonation,
        SecurityDelegation
    }
    public enum TOKEN_TYPE
    {
        TokenPrimary = 1,
        TokenImpersonation
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct SECURITY_ATTRIBUTES
    {
        public uint nLength;
        public IntPtr lpSecurityDescriptor;
        public bool bInheritHandle;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct SID_AND_ATTRIBUTES
    {
        public IntPtr Sid;
        public int Attributes;
    }
    public struct TOKEN_USER
    {
        public SID_AND_ATTRIBUTES User;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public struct PROCESS_BASIC_INFORMATION
    {
        public IntPtr ExitStatus;
        public IntPtr PebBaseAddress;
        public IntPtr AffinityMask;
        public IntPtr BasePriority;
        public UIntPtr UniqueProcessId;
        public IntPtr InheritedFromUniqueProcessId;
        public int Size
        {
            get { return (int)System.Runtime.InteropServices.Marshal.SizeOf(typeof(PROCESS_BASIC_INFORMATION)); }
            // get { return (1 * sizeof(int) + 5 * IntPtr.Size); }
        }
    }
    public enum TOKEN_INFORMATION_CLASS
    {
        TokenUser = 1,
        TokenGroups,
        TokenPrivileges,
        TokenOwner,
        TokenPrimaryGroup,
        TokenDefaultDacl,
        TokenSource,
        TokenType,
        TokenImpersonationLevel,
        TokenStatistics,
        TokenRestrictedSids,
        TokenSessionId,
        TokenGroupsAndPrivileges,
        TokenSessionReference,
        TokenSandBoxInert,
        TokenAuditPolicy,
        TokenOrigin
    }
    public enum PROCESSINFOCLASS : int
    {
        ProcessBasicInformation = 0, // 0, q: PROCESS_BASIC_INFORMATION, PROCESS_EXTENDED_BASIC_INFORMATION
        ProcessQuotaLimits, // qs: QUOTA_LIMITS, QUOTA_LIMITS_EX
        ProcessIoCounters, // q: IO_COUNTERS
        ProcessVmCounters, // q: VM_COUNTERS, VM_COUNTERS_EX
        ProcessTimes, // q: KERNEL_USER_TIMES
        ProcessBasePriority, // s: KPRIORITY
        ProcessRaisePriority, // s: ULONG
        ProcessDebugPort, // q: HANDLE
        ProcessExceptionPort, // s: HANDLE
        ProcessAccessToken, // s: PROCESS_ACCESS_TOKEN
        ProcessLdtInformation, // 10
        ProcessLdtSize,
        ProcessDefaultHardErrorMode, // qs: ULONG
        ProcessIoPortHandlers, // (kernel-mode only)
        ProcessPooledUsageAndLimits, // q: POOLED_USAGE_AND_LIMITS
        ProcessWorkingSetWatch, // q: PROCESS_WS_WATCH_INFORMATION[]; s: void
        ProcessUserModeIOPL,
        ProcessEnableAlignmentFaultFixup, // s: BOOLEAN
        ProcessPriorityClass, // qs: PROCESS_PRIORITY_CLASS
        ProcessWx86Information,
        ProcessHandleCount, // 20, q: ULONG, PROCESS_HANDLE_INFORMATION
        ProcessAffinityMask, // s: KAFFINITY
        ProcessPriorityBoost, // qs: ULONG
        ProcessDeviceMap, // qs: PROCESS_DEVICEMAP_INFORMATION, PROCESS_DEVICEMAP_INFORMATION_EX
        ProcessSessionInformation, // q: PROCESS_SESSION_INFORMATION
        ProcessForegroundInformation, // s: PROCESS_FOREGROUND_BACKGROUND
        ProcessWow64Information, // q: ULONG_PTR
        ProcessImageFileName, // q: UNICODE_STRING
        ProcessLUIDDeviceMapsEnabled, // q: ULONG
        ProcessBreakOnTermination, // qs: ULONG
        ProcessDebugObjectHandle, // 30, q: HANDLE
        ProcessDebugFlags, // qs: ULONG
        ProcessHandleTracing, // q: PROCESS_HANDLE_TRACING_QUERY; s: size 0 disables, otherwise enables
        ProcessIoPriority, // qs: ULONG
        ProcessExecuteFlags, // qs: ULONG
        ProcessResourceManagement,
        ProcessCookie, // q: ULONG
        ProcessImageInformation, // q: SECTION_IMAGE_INFORMATION
        ProcessCycleTime, // q: PROCESS_CYCLE_TIME_INFORMATION
        ProcessPagePriority, // q: ULONG
        ProcessInstrumentationCallback, // 40
        ProcessThreadStackAllocation, // s: PROCESS_STACK_ALLOCATION_INFORMATION, PROCESS_STACK_ALLOCATION_INFORMATION_EX
        ProcessWorkingSetWatchEx, // q: PROCESS_WS_WATCH_INFORMATION_EX[]
        ProcessImageFileNameWin32, // q: UNICODE_STRING
        ProcessImageFileMapping, // q: HANDLE (input)
        ProcessAffinityUpdateMode, // qs: PROCESS_AFFINITY_UPDATE_MODE
        ProcessMemoryAllocationMode, // qs: PROCESS_MEMORY_ALLOCATION_MODE
        ProcessGroupInformation, // q: USHORT[]
        ProcessTokenVirtualizationEnabled, // s: ULONG
        ProcessConsoleHostProcess, // q: ULONG_PTR
        ProcessWindowInformation, // 50, q: PROCESS_WINDOW_INFORMATION
        ProcessHandleInformation, // q: PROCESS_HANDLE_SNAPSHOT_INFORMATION // since WIN8
        ProcessMitigationPolicy, // s: PROCESS_MITIGATION_POLICY_INFORMATION
        ProcessDynamicFunctionTableInformation,
        ProcessHandleCheckingMode,
        ProcessKeepAliveCount, // q: PROCESS_KEEPALIVE_COUNT_INFORMATION
        ProcessRevokeFileHandles, // s: PROCESS_REVOKE_FILE_HANDLES_INFORMATION
        MaxProcessInfoClass
    };
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct OSVERSIONINFOEXW
    {
        public int dwOSVersionInfoSize;
        public int dwMajorVersion;
        public int dwMinorVersion;
        public int dwBuildNumber;
        public int dwPlatformId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szCSDVersion;
        public UInt16 wServicePackMajor;
        public UInt16 wServicePackMinor;
        public UInt16 wSuiteMask;
        public byte wProductType;
        public byte wReserved;
    }

    public enum BinaryType : uint
    {
        SCS_32BIT_BINARY = 0, // A 32-bit Windows-based application
        SCS_64BIT_BINARY = 6, // A 64-bit Windows-based application.
        SCS_DOS_BINARY = 1,   // An MS-DOS – based application
        SCS_OS216_BINARY = 5, // A 16-bit OS/2-based application
        SCS_PIF_BINARY = 3,   // A PIF file that executes an MS-DOS – based application
        SCS_POSIX_BINARY = 4, // A POSIX – based application
        SCS_WOW_BINARY = 2    // A 16-bit Windows-based application 
    }

    [Flags]
    public enum ProcessAccessFlags : uint
    {
        All = 0x001F0FFF,
        Terminate = 0x00000001,
        CreateThread = 0x00000002,
        VirtualMemoryOperation = 0x00000008,
        VirtualMemoryRead = 0x00000010,
        VirtualMemoryWrite = 0x00000020,
        DuplicateHandle = 0x00000040,
        CreateProcess = 0x000000080,
        SetQuota = 0x00000100,
        SetInformation = 0x00000200,
        QueryInformation = 0x00000400,
        QueryLimitedInformation = 0x00001000,
        Synchronize = 0x00100000
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct COLORREF
    {
        public byte R;
        public byte G;
        public byte B;

        public COLORREF(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        public static implicit operator Color(COLORREF c)
        {
            return Color.FromArgb(c.R, c.G, c.B);
        }

        public static implicit operator COLORREF(Color c)
        {
            return new COLORREF(c.R, c.G, c.B);
        }

#if NETFRAMEWORK
        public static implicit operator System.Windows.Media.Color(COLORREF c)
        {
            return System.Windows.Media.Color.FromRgb(c.R, c.G, c.B);
        }

        public static implicit operator COLORREF(System.Windows.Media.Color c)
        {
            return new COLORREF(c.R, c.G, c.B);
        }
#endif

        public override string ToString()
        {
            return $"R={R},G={G},B={B}";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public InputType type;
        public INPUTUNION u;

        public static int Size => Marshal.SizeOf(typeof(INPUT));

        public static INPUT MouseInput(MOUSEINPUT mouseInput)
        {
            return new INPUT { type = InputType.INPUT_MOUSE, u = new INPUTUNION { mi = mouseInput } };
        }

        public static INPUT KeyboardInput(KEYBDINPUT keyboardInput)
        {
            return new INPUT { type = InputType.INPUT_KEYBOARD, u = new INPUTUNION { ki = keyboardInput } };
        }

        public static INPUT KeyboardInput(KEYINPUTUNION keyboardInput)
        {
            return new INPUT { type = InputType.INPUT_KEYBOARD, u = new INPUTUNION { kiu = keyboardInput } };
        }

        public static INPUT HardwareInput(HARDWAREINPUT hardwareInput)
        {
            return new INPUT { type = InputType.INPUT_HARDWARE, u = new INPUTUNION { hi = hardwareInput } };
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct INPUTUNION
    {
        [FieldOffset(0)] public MOUSEINPUT mi;

        [FieldOffset(0)] public KEYBDINPUT ki;

        [FieldOffset(0)] public KEYINPUTUNION kiu;

        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public MouseEventFlags dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYINPUTUNION
    {
        public ushort wVk;
        public byte wScan;
        public KeyEventFlags dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public KeyEventFlags dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CURSORINFO
    {
        public Int32 cbSize;
        public CursorState flags;
        public IntPtr hCursor;
        public POINT ptScreenPos;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ICONINFO
    {
        public bool fIcon;
        public int xHotspot;
        public int yHotspot;
        public IntPtr hbmMask;
        public IntPtr hbmColor;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;

        public override string ToString()
        {
            return $"{{X={left},Y={top},Width={right - left},Height={bottom - top}}}";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MonitorInfo
    {
        public uint size;
        public RECT monitor;
        public RECT work;
        public uint flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINTER_TOUCH_INFO
    {
        public POINTER_INFO pointerInfo;
        public TouchFlags touchFlags;
        public TouchMask touchMask;
        public RECT rcContact;
        public RECT rcContactRaw;
        public uint orientation;
        public uint pressure;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINTER_INFO
    {
        public PointerInputType pointerType;
        public uint pointerId;
        public uint frameId;
        public PointerFlags pointerFlags;
        public IntPtr sourceDevice;
        public IntPtr hwndTarget;
        public POINT ptPixelLocation;
        public POINT ptPixelLocationRaw;
        public POINT ptHimetricLocation;
        public POINT ptHimetricLocationRaw;
        public uint dwTime;
        public uint historyCount;
        public uint inputData;
        public uint dwKeyStates;
        public ulong PerformanceCount;
        public PointerButtonChangeType ButtonChangeType;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LSA_UNICODE_STRING
    {
        public LSA_UNICODE_STRING(string value)
        {
            Buffer = Marshal.StringToHGlobalUni(value);
            Length = (ushort)(value.Length * UnicodeEncoding.CharSize);
            MaximumLength = (ushort)((value.Length + 1) * UnicodeEncoding.CharSize);
        }

        public ushort Length;
        public ushort MaximumLength;
        public IntPtr Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LSA_OBJECT_ATTRIBUTES
    {
        public int Length;
        public IntPtr RootDirectory;
        public LSA_UNICODE_STRING ObjectName;
        public uint Attributes;
        public IntPtr SecurityDescriptor;
        public IntPtr SecurityQualityOfService;
    }

    public enum LSA_AccessPolicy : long
    {
        POLICY_VIEW_LOCAL_INFORMATION = 0x00000001L,
        POLICY_VIEW_AUDIT_INFORMATION = 0x00000002L,
        POLICY_GET_PRIVATE_INFORMATION = 0x00000004L,
        POLICY_TRUST_ADMIN = 0x00000008L,
        POLICY_CREATE_ACCOUNT = 0x00000010L,
        POLICY_CREATE_SECRET = 0x00000020L,
        POLICY_CREATE_PRIVILEGE = 0x00000040L,
        POLICY_SET_DEFAULT_QUOTA_LIMITS = 0x00000080L,
        POLICY_SET_AUDIT_REQUIREMENTS = 0x00000100L,
        POLICY_AUDIT_LOG_ADMIN = 0x00000200L,
        POLICY_SERVER_ADMIN = 0x00000400L,
        POLICY_LOOKUP_NAMES = 0x00000800L,
        POLICY_NOTIFICATION = 0x00001000L,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TOKEN_PRIVILEGES
    {
        private uint prvct;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        private LUID_AND_ATTRIBUTES[] privileges;

        public uint PrivilegeCount
        {
            get { return prvct; }
            set { prvct = value; }
        }

        public LUID_AND_ATTRIBUTES[] Privileges
        {
            get { return privileges; }
            set { privileges = value; }
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct LUID
    {
        private uint lp;
        private int hp;

        public uint LowPart
        {
            get { return lp; }
            set { lp = value; }
        }

        public int HighPart
        {
            get { return hp; }
            set { hp = value; }
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct LUID_AND_ATTRIBUTES
    {
        private LUID luid;
        private uint attributes;

        public LUID LUID
        {
            get { return luid; }
            set { luid = value; }
        }

        public uint Attributes
        {
            get { return attributes; }
            set { attributes = value; }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SP_DEVICE_INTERFACE_DATA
    {
        public uint cbSize;
        public Guid interfaceClassGuid;
        public Int32 flags;
        private IntPtr reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SP_DEVINFO_DATA
    {
        public uint cbSize;
        public Guid ClassGuid;
        public uint DevInst;
        public IntPtr Reserved;
    }

    public struct HIDD_ATTRIBUTES
    {
        public Int32 Size;
        public UInt16 VendorID;
        public UInt16 ProductID;
        public UInt16 VersionNumber;
    }
    public enum ShowWindowCommands : int
    {
        Hide = 0,
        Normal = 1,
        Minimized = 2,
        Maximized = 3,
    }
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPLACEMENT
    {
        public int length;
        public int flags;
        public ShowWindowCommands showCmd;
        public System.Drawing.Point ptMinPosition;
        public System.Drawing.Point ptMaxPosition;
        public System.Drawing.Rectangle rcNormalPosition;
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
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DATA_BLOB
    {
        public int cbData;
        public IntPtr pbData;
    }
    [Flags]
    public enum CryptProtectPromptFlags
    {
        // prompt on unprotect
        CRYPTPROTECT_PROMPT_ON_UNPROTECT = 0x1,

        // prompt on protect
        CRYPTPROTECT_PROMPT_ON_PROTECT = 0x2
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct CRYPTPROTECT_PROMPTSTRUCT
    {
        public int cbSize;
        public CryptProtectPromptFlags dwPromptFlags;
        public IntPtr hwndApp;
        public String szPrompt;
    }

    [Flags]
    public enum CryptProtectFlags
    {
        // for remote-access situations where ui is not an option
        // if UI was specified on protect or unprotect operation, the call
        // will fail and GetLastError() will indicate ERROR_PASSWORD_RESTRICTION
        CRYPTPROTECT_UI_FORBIDDEN = 0x1,

        // per machine protected data -- any user on machine where CryptProtectData
        // took place may CryptUnprotectData
        CRYPTPROTECT_LOCAL_MACHINE = 0x4,

        // force credential synchronize during CryptProtectData()
        // Synchronize is only operation that occurs during this operation
        CRYPTPROTECT_CRED_SYNC = 0x8,

        // Generate an Audit on protect and unprotect operations
        CRYPTPROTECT_AUDIT = 0x10,

        // Protect data with a non-recoverable key
        CRYPTPROTECT_NO_RECOVERY = 0x20,


        // Verify the protection of a protected blob
        CRYPTPROTECT_VERIFY_PROTECTION = 0x40
    }
}
