using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace II.RPA.Plugin.Excel.Utils
{
    public enum AssocStr
    {
        ASSOCSTR_COMMAND = 1,
        ASSOCSTR_EXECUTABLE,
        ASSOCSTR_FRIENDLYDOCNAME,
        ASSOCSTR_FRIENDLYAPPNAME,
        ASSOCSTR_NOOPEN,
        ASSOCSTR_SHELLNEWVALUE,
        ASSOCSTR_DDECOMMAND,
        ASSOCSTR_DDEIFEXEC,
        ASSOCSTR_DDEAPPLICATION,
        ASSOCSTR_DDETOPIC,
        ASSOCSTR_INFOTIP,
        ASSOCSTR_QUICKTIP,
        ASSOCSTR_TILEINFO,
        ASSOCSTR_CONTENTTYPE,
        ASSOCSTR_DEFAULTICON,
        ASSOCSTR_SHELLEXTENSION,
        ASSOCSTR_DROPTARGET,
        ASSOCSTR_DELEGATEEXECUTE,
        ASSOCSTR_SUPPORTED_URI_PROTOCOLS,
        ASSOCSTR_MAX
    }
    public enum AssocF
    {
        ASSOCF_NONE = 0x00000000,
        ASSOCF_INIT_NOREMAPCLSID = 0x00000001,
        ASSOCF_INIT_BYEXENAME = 0x00000002,
        ASSOCF_OPEN_BYEXENAME = 0x00000002,
        ASSOCF_INIT_DEFAULTTOSTAR = 0x00000004,
        ASSOCF_INIT_DEFAULTTOFOLDER = 0x00000008,
        ASSOCF_NOUSERSETTINGS = 0x00000010,
        ASSOCF_NOTRUNCATE = 0x00000020,
        ASSOCF_VERIFY = 0x00000040,
        ASSOCF_REMAPRUNDLL = 0x00000080,
        ASSOCF_NOFIXUPS = 0x00000100,
        ASSOCF_IGNOREBASECLASS = 0x00000200,
        ASSOCF_INIT_IGNOREUNKNOWN = 0x00000400,
        ASSOCF_INIT_FIXED_PROGID = 0x00000800,
        ASSOCF_IS_PROTOCOL = 0x00001000,
        ASSOCF_INIT_FOR_FILE = 0x00002000
    }
    public class NativeUtils
    {
        // 导入Windows API函数      

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("ole32.dll")]
        public static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(int hWnd, out int lpdwProcessId);

        [DllImport("Shlwapi.dll", CharSet = CharSet.Unicode)]
        public static extern uint AssocQueryString(AssocF flags, AssocStr str,
  string pszAssoc, string pszExtra, [Out] StringBuilder pszOut, ref uint pcchOut);

        public static string AssocQueryString(AssocStr association, string extension)
        {
            const int S_OK = 0;
            const int S_FALSE = 1;

            uint length = 0;
            uint ret = AssocQueryString(AssocF.ASSOCF_NONE, association, extension, null, null, ref length);
            if (ret != S_FALSE)
            {
                throw new InvalidOperationException("Could not determine associated string");
            }

            var sb = new StringBuilder((int)length);
            ret = AssocQueryString(AssocF.ASSOCF_NONE, association, extension, null, sb, ref length);
            if (ret != S_OK)
            {
                throw new InvalidOperationException("Could not determine associated string");
            }

            return sb.ToString();
        }

        public struct OSVERSIONINFOEX
        {
            public int dwOSVersionInfoSize;

            public int dwMajorVersion;

            public int dwMinorVersion;

            public int dwBuildNumber;

            public int dwPlatformId;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;

            public short wServicePackMajor;

            public short wServicePackMinor;

            public short wSuiteMask;

            public byte wProductType;

            public byte wReserved;
        }

        public struct Processentry32
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
        }

        public struct MSLLHOOKSTRUCT
        {
            public POINT Point;

            public int MouseData;

            public int Flags;

            public int Time;

            public int ExtraInfo;
        }

        public class HookEventArgs : EventArgs
        {
            private readonly int _code;

            private readonly IntPtr _wParam;

            private readonly IntPtr _lParam;

            public int Code => _code;

            public IntPtr wParam => _wParam;

            public IntPtr lParam => _lParam;

            public HookEventArgs(int code, IntPtr wParam, IntPtr lParam)
            {
                _code = code;
                _wParam = wParam;
                _lParam = lParam;
            }
        }

        public delegate void HookEventHandler(object sender, HookEventArgs e);

        public enum HookType
        {
            WH_JOURNALRECORD,
            WH_JOURNALPLAYBACK,
            WH_KEYBOARD,
            WH_GETMESSAGE,
            WH_CALLWNDPROC,
            WH_CBT,
            WH_SYSMSGFILTER,
            WH_MOUSE,
            WH_HARDWARE,
            WH_DEBUG,
            WH_SHELL,
            WH_FOREGROUNDIDLE,
            WH_CALLWNDPROCRET,
            WH_KEYBOARD_LL,
            WH_MOUSE_LL
        }

        [Flags]
        public enum MouseEventFlags : uint
        {
            Move = 0x1u,
            LeftDown = 0x2u,
            LeftUp = 0x4u,
            RightDown = 0x8u,
            RightUp = 0x10u,
            MiddleDown = 0x20u,
            MiddleUp = 0x40u,
            XDown = 0x80u,
            XUp = 0x100u,
            Wheel = 0x800u,
            VirtualDesk = 0x4000u,
            Absolute = 0x8000u
        }

        [Flags]
        public enum KeyboardFlags : uint
        {
            KeyDown = 0x0u,
            ExtendedKey = 0x1u,
            KeyUp = 0x2u,
            Unicode = 0x4u,
            ScanCode = 0x8u
        }

        [Flags]
        public enum InputType : uint
        {
            Mouse = 0x0u,
            Keyboard = 0x1u,
            Hardware = 0x2u
        }

        public enum GetAncestor_Flags
        {
            GetParent = 1,
            GetRoot,
            GetRootOwner
        }

        public enum ShellHookMessages
        {
            HSHELL_WINDOWCREATED = 1,
            HSHELL_WINDOWDESTROYED = 2,
            HSHELL_ACTIVATESHELLWINDOW = 3,
            HSHELL_WINDOWACTIVATED = 4,
            HSHELL_GETMINRECT = 5,
            HSHELL_REDRAW = 6,
            HSHELL_TASKMAN = 7,
            HSHELL_LANGUAGE = 8,
            HSHELL_ACCESSIBILITYSTATE = 11
        }

        public enum ShowWindowCmds
        {
            SW_HIDE = 0,
            SW_SHOWNORMAL = 1,
            SW_NORMAL = 1,
            SW_SHOWMINIMIZED = 2,
            SW_SHOWMAXIMIZED = 3,
            SW_MAXIMIZE = 3,
            SW_SHOWNOACTIVATE = 4,
            SW_SHOW = 5,
            SW_MINIMIZE = 6,
            SW_SHOWMINNOACTIVE = 7,
            SW_SHOWNA = 8,
            SW_RESTORE = 9,
            SW_SHOWDEFAULT = 10,
            SW_FORCEMINIMIZE = 11,
            SW_MAX = 12
        }

        public enum GetWindowCmds
        {
            GW_HWNDFIRST,
            GW_HWNDLAST,
            GW_HWNDNEXT,
            GW_HWNDPREV,
            GW_OWNER,
            GW_CHILD,
            GW_ENABLEDPOPUP
        }

        public enum PeekMessageFlags
        {
            PM_NOREMOVE,
            PM_REMOVE,
            PM_NOYIELD
        }

        public enum WindowMessages
        {
            WM_NULL = 0,
            WM_CREATE = 1,
            WM_DESTROY = 2,
            WM_MOVE = 3,
            WM_SIZE = 5,
            WM_ACTIVATE = 6,
            WM_SETFOCUS = 7,
            WM_KILLFOCUS = 8,
            WM_ENABLE = 10,
            WM_SETREDRAW = 11,
            WM_SETTEXT = 12,
            WM_GETTEXT = 13,
            WM_GETTEXTLENGTH = 14,
            WM_PAINT = 0xF,
            WM_CLOSE = 0x10,
            WM_QUERYENDSESSION = 17,
            WM_QUIT = 18,
            WM_QUERYOPEN = 19,
            WM_ERASEBKGND = 20,
            WM_SYSCOLORCHANGE = 21,
            WM_ENDSESSION = 22,
            WM_SHOWWINDOW = 24,
            WM_CTLCOLOR = 25,
            WM_WININICHANGE = 26,
            WM_SETTINGCHANGE = 26,
            WM_DEVMODECHANGE = 27,
            WM_ACTIVATEAPP = 28,
            WM_FONTCHANGE = 29,
            WM_TIMECHANGE = 30,
            WM_CANCELMODE = 0x1F,
            WM_SETCURSOR = 0x20,
            WM_MOUSEACTIVATE = 33,
            WM_CHILDACTIVATE = 34,
            WM_QUEUESYNC = 35,
            WM_GETMINMAXINFO = 36,
            WM_PAINTICON = 38,
            WM_ICONERASEBKGND = 39,
            WM_NEXTDLGCTL = 40,
            WM_SPOOLERSTATUS = 42,
            WM_DRAWITEM = 43,
            WM_MEASUREITEM = 44,
            WM_DELETEITEM = 45,
            WM_VKEYTOITEM = 46,
            WM_CHARTOITEM = 47,
            WM_SETFONT = 48,
            WM_GETFONT = 49,
            WM_SETHOTKEY = 50,
            WM_GETHOTKEY = 51,
            WM_QUERYDRAGICON = 55,
            WM_COMPAREITEM = 57,
            WM_GETOBJECT = 61,
            WM_COMPACTING = 65,
            WM_COMMNOTIFY = 68,
            WM_WINDOWPOSCHANGING = 70,
            WM_WINDOWPOSCHANGED = 71,
            WM_POWER = 72,
            WM_COPYDATA = 74,
            WM_CANCELJOURNAL = 75,
            WM_NOTIFY = 78,
            WM_INPUTLANGCHANGEREQUEST = 80,
            WM_INPUTLANGCHANGE = 81,
            WM_TCARD = 82,
            WM_HELP = 83,
            WM_USERCHANGED = 84,
            WM_NOTIFYFORMAT = 85,
            WM_CONTEXTMENU = 123,
            WM_STYLECHANGING = 124,
            WM_STYLECHANGED = 125,
            WM_DISPLAYCHANGE = 126,
            WM_GETICON = 0x7F,
            WM_SETICON = 0x80,
            WM_NCCREATE = 129,
            WM_NCDESTROY = 130,
            WM_NCCALCSIZE = 131,
            WM_NCHITTEST = 132,
            WM_NCPAINT = 133,
            WM_NCACTIVATE = 134,
            WM_GETDLGCODE = 135,
            WM_SYNCPAINT = 136,
            WM_NCMOUSEMOVE = 160,
            WM_NCLBUTTONDOWN = 161,
            WM_NCLBUTTONUP = 162,
            WM_NCLBUTTONDBLCLK = 163,
            WM_NCRBUTTONDOWN = 164,
            WM_NCRBUTTONUP = 165,
            WM_NCRBUTTONDBLCLK = 166,
            WM_NCMBUTTONDOWN = 167,
            WM_NCMBUTTONUP = 168,
            WM_NCMBUTTONDBLCLK = 169,
            WM_KEYDOWN = 0x100,
            WM_KEYUP = 257,
            WM_CHAR = 258,
            WM_DEADCHAR = 259,
            WM_SYSKEYDOWN = 260,
            WM_SYSKEYUP = 261,
            WM_SYSCHAR = 262,
            WM_SYSDEADCHAR = 263,
            WM_KEYLAST = 264,
            WM_IME_STARTCOMPOSITION = 269,
            WM_IME_ENDCOMPOSITION = 270,
            WM_IME_COMPOSITION = 271,
            WM_IME_KEYLAST = 271,
            WM_INITDIALOG = 272,
            WM_COMMAND = 273,
            WM_SYSCOMMAND = 274,
            WM_TIMER = 275,
            WM_HSCROLL = 276,
            WM_VSCROLL = 277,
            WM_INITMENU = 278,
            WM_INITMENUPOPUP = 279,
            WM_MENUSELECT = 287,
            WM_MENUCHAR = 288,
            WM_ENTERIDLE = 289,
            WM_MENURBUTTONUP = 290,
            WM_MENUDRAG = 291,
            WM_MENUGETOBJECT = 292,
            WM_UNINITMENUPOPUP = 293,
            WM_MENUCOMMAND = 294,
            WM_CTLCOLORMSGBOX = 306,
            WM_CTLCOLOREDIT = 307,
            WM_CTLCOLORLISTBOX = 308,
            WM_CTLCOLORBTN = 309,
            WM_CTLCOLORDLG = 310,
            WM_CTLCOLORSCROLLBAR = 311,
            WM_CTLCOLORSTATIC = 312,
            WM_MOUSEMOVE = 0x200,
            WM_LBUTTONDOWN = 513,
            WM_LBUTTONUP = 514,
            WM_LBUTTONDBLCLK = 515,
            WM_RBUTTONDOWN = 516,
            WM_RBUTTONUP = 517,
            WM_RBUTTONDBLCLK = 518,
            WM_MBUTTONDOWN = 519,
            WM_MBUTTONUP = 520,
            WM_MBUTTONDBLCLK = 521,
            WM_MOUSEWHEEL = 522,
            WM_PARENTNOTIFY = 528,
            WM_ENTERMENULOOP = 529,
            WM_EXITMENULOOP = 530,
            WM_NEXTMENU = 531,
            WM_SIZING = 532,
            WM_CAPTURECHANGED = 533,
            WM_MOVING = 534,
            WM_DEVICECHANGE = 537,
            WM_MDICREATE = 544,
            WM_MDIDESTROY = 545,
            WM_MDIACTIVATE = 546,
            WM_MDIRESTORE = 547,
            WM_MDINEXT = 548,
            WM_MDIMAXIMIZE = 549,
            WM_MDITILE = 550,
            WM_MDICASCADE = 551,
            WM_MDIICONARRANGE = 552,
            WM_MDIGETACTIVE = 553,
            WM_MDISETMENU = 560,
            WM_ENTERSIZEMOVE = 561,
            WM_EXITSIZEMOVE = 562,
            WM_DROPFILES = 563,
            WM_MDIREFRESHMENU = 564,
            WM_IME_SETCONTEXT = 641,
            WM_IME_NOTIFY = 642,
            WM_IME_CONTROL = 643,
            WM_IME_COMPOSITIONFULL = 644,
            WM_IME_SELECT = 645,
            WM_IME_CHAR = 646,
            WM_IME_REQUEST = 648,
            WM_IME_KEYDOWN = 656,
            WM_IME_KEYUP = 657,
            WM_MOUSEHOVER = 673,
            WM_MOUSELEAVE = 675,
            WM_CUT = 768,
            WM_COPY = 769,
            WM_PASTE = 770,
            WM_CLEAR = 771,
            WM_UNDO = 772,
            WM_RENDERFORMAT = 773,
            WM_RENDERALLFORMATS = 774,
            WM_DESTROYCLIPBOARD = 775,
            WM_DRAWCLIPBOARD = 776,
            WM_PAINTCLIPBOARD = 777,
            WM_VSCROLLCLIPBOARD = 778,
            WM_SIZECLIPBOARD = 779,
            WM_ASKCBFORMATNAME = 780,
            WM_CHANGECBCHAIN = 781,
            WM_HSCROLLCLIPBOARD = 782,
            WM_QUERYNEWPALETTE = 783,
            WM_PALETTEISCHANGING = 784,
            WM_PALETTECHANGED = 785,
            WM_HOTKEY = 786,
            WM_PRINT = 791,
            WM_PRINTCLIENT = 792,
            WM_HANDHELDFIRST = 856,
            WM_HANDHELDLAST = 863,
            WM_AFXFIRST = 864,
            WM_AFXLAST = 895,
            WM_PENWINFIRST = 896,
            WM_PENWINLAST = 911,
            WM_APP = 0x8000,
            WM_USER = 0x400,
            WM_REFLECT = 0x2000,
            MIN_ALL = 419,
            MIN_ALL_UNDO = 416
        }

        public enum MouseHitTestResponses
        {
            HTBORDER = 18,
            HTBOTTOM = 0xF,
            HTBOTTOMLEFT = 0x10,
            HTBOTTOMRIGHT = 17,
            HTCAPTION = 2,
            HTCLIENT = 1,
            HTCLOSE = 20,
            HTERROR = -2,
            HTGROWBOX = 4,
            HTHELP = 21,
            HTHSCROLL = 6,
            HTLEFT = 10,
            HTMENU = 5,
            HTMAXBUTTON = 9,
            HTMINBUTTON = 8,
            HTNOWHERE = 0,
            HTREDUCE = 8,
            HTRIGHT = 11,
            HTSIZE = 4,
            HTSYSMENU = 3,
            HTTOP = 12,
            HTTOPLEFT = 13,
            HTTOPRIGHT = 14,
            HTTRANSPARENT = -1,
            HTVSCROLL = 7,
            HTZOOM = 9
        }

        public enum WindowStyles : uint
        {
            WS_BORDER = 0x800000u,
            WS_CAPTION = 12582912u,
            WS_CHILD = 0x40000000u,
            WS_CHILDWINDOW = 0x40000000u,
            WS_CLIPCHILDREN = 0x2000000u,
            WS_CLIPSIBLINGS = 0x4000000u,
            WS_DISABLED = 0x8000000u,
            WS_DLGFRAME = 0x400000u,
            WS_EX_ACCEPTFILES = 0x10u,
            WS_EX_DLGMODALFRAME = 1u,
            WS_EX_NOPARENTNOTIFY = 4u,
            WS_EX_TOPMOST = 8u,
            WS_EX_TRANSPARENT = 0x20u,
            WS_EX_TOOLWINDOW = 0x80u,
            WS_EX_APPWINDOW = 0x40000u,
            WS_EX_NOACTIVATE = 0x8000000u,
            WS_EX_LAYERED = 0x80000u,
            WS_EX_MDICHILD = 0x40u,
            WS_GROUP = 0x20000u,
            WS_HSCROLL = 0x100000u,
            WS_MAXIMIZE = 0x1000000u,
            WS_MAXIMIZEBOX = 0x10000u,
            WS_MINIMIZE = 0x20000000u,
            WS_MINIMIZEBOX = 0x20000u,
            WS_OVERLAPPED = 0u,
            WS_POPUP = 0x80000000u,
            WS_SYSMENU = 0x80000u,
            WS_TABSTOP = 0x10000u,
            WS_THICKFRAME = 0x40000u,
            WS_VISIBLE = 0x10000000u,
            WS_VSCROLL = 0x200000u,
            WS_ICONIC = 0x20000000u,
            WS_POPUPWINDOW = 2156396544u,
            WS_SIZEBOX = 0x40000u,
            WS_TILED = 0u,
            WS_OVERLAPPEDWINDOW = 13565952u
        }

        public struct WindowInfo
        {
            public int size;

            public Rectangle window;

            public Rectangle client;

            public int style;

            public int exStyle;

            public int windowStatus;

            public uint xWindowBorders;

            public uint yWindowBorders;

            public short atomWindowtype;

            public short creatorVersion;
        }

        public struct WindowPlacement
        {
            public int length;

            public int flags;

            public int showCmd;

            public Point minPosition;

            public Point maxPosition;

            public Rectangle normalPosition;
        }

        public struct Rect
        {
            public int left;

            public int top;

            public int right;

            public int bottom;

            public int Width => right - left;

            public int Height => bottom - top;
        }

        public struct POINT
        {
            public int x;

            public int y;

            public POINT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }

        public struct CURSORINFO
        {
            public int cbSize;

            public int flags;

            public IntPtr hCursor;

            public POINT ptScreenPos;
        }

        public delegate bool EnumWindowEventHandler(IntPtr hWnd, int lParam);

        public delegate bool EventHandlerForEnumWindows(IntPtr hWnd, ref object extraData);

        public delegate bool EnumChildWindowsEventHandler(IntPtr handle, ref object extraData);

        public enum DpiType
        {
            Effective,
            Angular,
            Raw
        }

        public enum BinaryRasterOperations
        {
            R2_BLACK = 1,
            R2_NOTMERGEPEN = 2,
            R2_MASKNOTPEN = 3,
            R2_NOTCOPYPEN = 4,
            R2_MASKPENNOT = 5,
            R2_NOT = 6,
            R2_XORPEN = 7,
            R2_NOTMASKPEN = 8,
            R2_MASKPEN = 9,
            R2_NOTXORPEN = 10,
            R2_NOP = 11,
            R2_MERGENOTPEN = 12,
            R2_COPYPEN = 13,
            R2_MERGEPENNOT = 14,
            R2_MERGEPEN = 0xF,
            R2_WHITE = 0x10,
            R2_LAST = 0x10
        }

        public enum TernaryRasterOperations
        {
            SRCCOPY = 13369376,
            SRCPAINT = 15597702,
            SRCAND = 8913094,
            SRCINVERT = 6684742,
            SRCERASE = 4457256,
            NOTSRCCOPY = 3342344,
            NOTSRCERASE = 1114278,
            MERGECOPY = 12583114,
            MERGEPAINT = 12255782,
            PATCOPY = 15728673,
            PATPAINT = 16452105,
            PATINVERT = 5898313,
            DSTINVERT = 5570569,
            BLACKNESS = 66,
            WHITENESS = 16711778
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct INPUT
        {
            [FieldOffset(0)]
            public int type;

            [FieldOffset(4)]
            public MOUSEINPUT mi;

            [FieldOffset(4)]
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct INPUT64
        {
            [FieldOffset(0)]
            public int type;

            [FieldOffset(8)]
            public MOUSEINPUT mi;

            [FieldOffset(8)]
            public KEYBDINPUT ki;
        }

        public struct MOUSEINPUT
        {
            public int dx;

            public int dy;

            public int mouseData;

            public int dwFlags;

            public int time;

            public IntPtr dwExtraInfo;
        }

        public struct KEYBDINPUT
        {
            public short wVk;

            public short wScan;

            public int dwFlags;

            public int time;

            public IntPtr dwExtraInfo;
        }

        public struct FILETIME
        {
            public uint dwLowDateTime;

            public uint dwHighDateTime;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WIN32_FIND_DATA
        {
            public uint dwFileAttributes;

            public FILETIME ftCreationTime;

            public FILETIME ftLastAccessTime;

            public FILETIME ftLastWriteTime;

            public uint nFileSizeHigh;

            public uint nFileSizeLow;

            public uint dwReserved0;

            public uint dwReserved1;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        [Flags]
        public enum ProcessAccessFlags
        {
            DELETE = 0x10000,
            READ_CONTROL = 0x20000,
            SYNCHRONIZE = 0x100000,
            WRITE_DAC = 0x40000,
            WRITE_OWNER = 0x80000,
            PROCESS_CREATE_PROCESS = 0x80,
            PROCESS_CREATE_THREAD = 0x2,
            PROCESS_DUP_HANDLE = 0x40,
            PROCESS_QUERY_INFORMATION = 0x400,
            PROCESS_QUERY_LIMITED_INFORMATION = 0x1000,
            PROCESS_SET_INFORMATION = 0x200,
            PROCESS_SET_QUOATA = 0x100,
            PROCESS_SUSPEND_RESUME = 0x800,
            PROCESS_TERMINATE = 0x1,
            PROCESS_VM_OPERATION = 0x8,
            PROCESS_VM_READ = 0x10,
            PROCESS__VM_WRITE = 0x20
        }

        public struct BITMAPINFO
        {
            public BITMAPINFOHEADER bmiHeader;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1, ArraySubType = UnmanagedType.Struct)]
            public RGBQUAD[] bmiColors;
        }

        public struct RGBQUAD
        {
            public byte rgbBlue;

            public byte rgbGreen;

            public byte rgbRed;

            public byte rgbReserved;
        }

        public struct BITMAPINFOHEADER
        {
            public uint biSize;

            public int biWidth;

            public int biHeight;

            public ushort biPlanes;

            public ushort biBitCount;

            public BitmapCompressionMode biCompression;

            public uint biSizeImage;

            public int biXPelsPerMeter;

            public int biYPelsPerMeter;

            public uint biClrUsed;

            public uint biClrImportant;

            public void Init()
            {
                biSize = (uint)Marshal.SizeOf(this);
            }
        }

        public enum BitmapCompressionMode : uint
        {
            BI_RGB,
            BI_RLE8,
            BI_RLE4,
            BI_BITFIELDS,
            BI_JPEG,
            BI_PNG
        }

        public enum DIBColorMode : uint
        {
            DIB_RGB_COLORS,
            DIB_PAL_COLORS
        }

        public enum DeviceCap
        {
            DRIVERVERSION = 0,
            TECHNOLOGY = 2,
            HORZSIZE = 4,
            VERTSIZE = 6,
            HORZRES = 8,
            VERTRES = 10,
            BITSPIXEL = 12,
            PLANES = 14,
            NUMBRUSHES = 0x10,
            NUMPENS = 18,
            NUMMARKERS = 20,
            NUMFONTS = 22,
            NUMCOLORS = 24,
            PDEVICESIZE = 26,
            CURVECAPS = 28,
            LINECAPS = 30,
            POLYGONALCAPS = 0x20,
            TEXTCAPS = 34,
            CLIPCAPS = 36,
            RASTERCAPS = 38,
            ASPECTX = 40,
            ASPECTY = 42,
            ASPECTXY = 44,
            SHADEBLENDCAPS = 45,
            LOGPIXELSX = 88,
            LOGPIXELSY = 90,
            SIZEPALETTE = 104,
            NUMRESERVED = 106,
            COLORRES = 108,
            PHYSICALWIDTH = 110,
            PHYSICALHEIGHT = 111,
            PHYSICALOFFSETX = 112,
            PHYSICALOFFSETY = 113,
            SCALINGFACTORX = 114,
            SCALINGFACTORY = 115,
            VREFRESH = 116,
            DESKTOPVERTRES = 117,
            DESKTOPHORZRES = 118,
            BLTALIGNMENT = 119
        }

        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;

            public IntPtr hThread;

            public uint dwProcessId;

            public uint dwThreadId;
        }

        public enum SecurityImpersonationLevel
        {
            SecurityAnonymous,
            SecurityIdentification,
            SecurityImpersonation,
            SecurityDelegation
        }

        public struct STARTUPINFO
        {
            public int cb;

            public string lpReserved;

            public string lpDesktop;

            public string lpTitle;

            public uint dwX;

            public uint dwY;

            public uint dwXSize;

            public uint dwYSize;

            public uint dwXCountChars;

            public uint dwYCountChars;

            public uint dwFillAttribute;

            public uint dwFlags;

            public short wShowWindow;

            public short cbReserved2;

            public IntPtr lpReserved2;

            public IntPtr hStdInput;

            public IntPtr hStdOutput;

            public IntPtr hStdError;
        }

        [Flags]
        public enum StartInfoFlags : uint
        {
            STARTF_FORCEONFEEDBACK = 0x40u,
            STARTF_FORCEOFFFEEDBACK = 0x80u,
            STARTF_PREVENTPINNING = 0x2000u,
            STARTF_RUNFULLSCREEN = 0x20u,
            STARTF_TITLEISAPPID = 0x1000u,
            STARTF_TITLEISLINKNAME = 0x800u,
            STARTF_UNTRUSTEDSOURCE = 0x8000u,
            STARTF_USECOUNTCHARS = 0x8u,
            STARTF_USEFILLATTRIBUTE = 0x10u,
            STARTF_USEHOTKEY = 0x200u,
            STARTF_USEPOSITION = 0x4u,
            STARTF_USESHOWWINDOW = 0x1u,
            STARTF_USESIZE = 0x2u,
            STARTF_USESTDHANDLES = 0x100u
        }

        public enum TokenType
        {
            TokenPrimary = 1,
            TokenImpersonation
        }

        [Flags]
        public enum ProcessCreationFlags : uint
        {
            CREATE_BREAKAWAY_FROM_JOB = 0x1000000u,
            CREATE_DEFAULT_ERROR_MODE = 0x4000000u,
            CREATE_NEW_CONSOLE = 0x10u,
            CREATE_NEW_PROCESS_GROUP = 0x200u,
            CREATE_NO_WINDOW = 0x8000000u,
            CREATE_PROTECTED_PROCESS = 0x40000u,
            CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x2000000u,
            CREATE_SEPARATE_WOW_VDM = 0x800u,
            CREATE_SHARED_WOW_VDM = 0x1000u,
            CREATE_SUSPENDED = 0x4u,
            CREATE_UNICODE_ENVIRONMENT = 0x400u,
            DEBUG_ONLY_THIS_PROCESS = 0x2u,
            DEBUG_PROCESS = 0x1u,
            DETACHED_PROCESS = 0x8u,
            EXTENDED_STARTUPINFO_PRESENT = 0x80000u,
            INHERIT_PARENT_AFFINITY = 0x10000u
        }

        [Flags]
        public enum DesktopAccessRightsFlags : long
        {
            DESKTOP_CREATEMENU = 0x4L,
            DESKTOP_CREATEWINDOW = 0x2L,
            DESKTOP_ENUMERATE = 0x40L,
            DESKTOP_HOOKCONTROL = 0x8L,
            DESKTOP_JOURNALPLAYBACK = 0x20L,
            DESKTOP_JOURNALRECORD = 0x10L,
            DESKTOP_READOBJECTS = 0x1L,
            DESKTOP_SWITCHDESKTOP = 0x100L,
            DESKTOP_WRITEOBJECTS = 0x80L
        }

        [Flags]
        public enum ExitWindows : uint
        {
            LogOff = 0x0u,
            ShutDown = 0x1u,
            Reboot = 0x2u,
            PowerOff = 0x8u,
            RestartApps = 0x40u,
            Force = 0x4u,
            ForceIfHung = 0x10u
        }

        [Flags]
        public enum ShutdownReason : uint
        {
            MajorApplication = 0x40000u,
            MajorHardware = 0x10000u,
            MajorLegacyApi = 0x70000u,
            MajorOperatingSystem = 0x20000u,
            MajorOther = 0x0u,
            MajorPower = 0x60000u,
            MajorSoftware = 0x30000u,
            MajorSystem = 0x50000u,
            MinorBlueScreen = 0xFu,
            MinorCordUnplugged = 0xBu,
            MinorDisk = 0x7u,
            MinorEnvironment = 0xCu,
            MinorHardwareDriver = 0xDu,
            MinorHotfix = 0x11u,
            MinorHung = 0x5u,
            MinorInstallation = 0x2u,
            MinorMaintenance = 0x1u,
            MinorMMC = 0x19u,
            MinorNetworkConnectivity = 0x14u,
            MinorNetworkCard = 0x9u,
            MinorOther = 0x0u,
            MinorOtherDriver = 0xEu,
            MinorPowerSupply = 0xAu,
            MinorProcessor = 0x8u,
            MinorReconfig = 0x4u,
            MinorSecurity = 0x13u,
            MinorSecurityFix = 0x12u,
            MinorSecurityFixUninstall = 0x18u,
            MinorServicePack = 0x10u,
            MinorServicePackUninstall = 0x16u,
            MinorTermSrv = 0x20u,
            MinorUnstable = 0x6u,
            MinorUpgrade = 0x3u,
            MinorWMI = 0x15u,
            FlagUserDefined = 0x40000000u,
            FlagPlanned = 0x80000000u
        }

        public enum LogonType
        {
            LOGON32_LOGON_INTERACTIVE = 2,
            LOGON32_LOGON_NETWORK = 3,
            LOGON32_LOGON_BATCH = 4,
            LOGON32_LOGON_SERVICE = 5,
            LOGON32_LOGON_UNLOCK = 7,
            LOGON32_LOGON_NETWORK_CLEARTEXT = 8,
            LOGON32_LOGON_NEW_CREDENTIALS = 9
        }

        public enum LogonProvider
        {
            LOGON32_PROVIDER_DEFAULT,
            LOGON32_PROVIDER_WINNT35,
            LOGON32_PROVIDER_WINNT40,
            LOGON32_PROVIDER_WINNT50
        }

        public enum SECURITY_IMPERSONATION_LEVEL
        {
            SecurityAnonymous,
            SecurityIdentification,
            SecurityImpersonation,
            SecurityDelegation
        }

        [Flags]
        public enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x40u,
            ES_CONTINUOUS = 0x80000000u,
            ES_DISPLAY_REQUIRED = 0x2u,
            ES_SYSTEM_REQUIRED = 0x1u
        }

        public struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;

            [MarshalAs(UnmanagedType.U2)]
            public ushort dmSpecVersion;

            [MarshalAs(UnmanagedType.U2)]
            public ushort dmDriverVersion;

            [MarshalAs(UnmanagedType.U2)]
            public ushort dmSize;

            [MarshalAs(UnmanagedType.U2)]
            public ushort dmDriverExtra;

            [MarshalAs(UnmanagedType.U4)]
            public uint dmFields;

            public POINTL dmPosition;

            [MarshalAs(UnmanagedType.U4)]
            public uint dmDisplayOrientation;

            [MarshalAs(UnmanagedType.U4)]
            public uint dmDisplayFixedOutput;

            [MarshalAs(UnmanagedType.I2)]
            public short dmColor;

            [MarshalAs(UnmanagedType.I2)]
            public short dmDuplex;

            [MarshalAs(UnmanagedType.I2)]
            public short dmYResolution;

            [MarshalAs(UnmanagedType.I2)]
            public short dmTTOption;

            [MarshalAs(UnmanagedType.I2)]
            public short dmCollate;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;

            [MarshalAs(UnmanagedType.U2)]
            public ushort dmLogPixels;

            [MarshalAs(UnmanagedType.U4)]
            public uint dmBitsPerPel;

            [MarshalAs(UnmanagedType.U4)]
            public uint dmPelsWidth;

            [MarshalAs(UnmanagedType.U4)]
            public uint dmPelsHeight;

            [MarshalAs(UnmanagedType.U4)]
            public uint dmDisplayFlags;

            [MarshalAs(UnmanagedType.U4)]
            public uint dmDisplayFrequency;

            [MarshalAs(UnmanagedType.U4)]
            public uint dmICMMethod;

            [MarshalAs(UnmanagedType.U4)]
            public uint dmICMIntent;

            [MarshalAs(UnmanagedType.U4)]
            public uint dmMediaType;

            [MarshalAs(UnmanagedType.U4)]
            public uint dmDitherType;

            [MarshalAs(UnmanagedType.U4)]
            public uint dmReserved1;

            [MarshalAs(UnmanagedType.U4)]
            public uint dmReserved2;

            [MarshalAs(UnmanagedType.U4)]
            public uint dmPanningWidth;

            [MarshalAs(UnmanagedType.U4)]
            public uint dmPanningHeight;

            public void Initialize()
            {
                dmDeviceName = new string(new char[32]);
                dmFormName = new string(new char[32]);
                dmSize = (ushort)Marshal.SizeOf(this);
            }
        }

        public struct POINTL
        {
            public int x;

            public int y;
        }

        public enum DisplayChangeResult
        {
            BadDualView = -6,
            BadParam,
            BadFlags,
            NotUpdated,
            BadMode,
            Failed,
            Successful,
            Restart
        }

        public const int DISABLE_NAVIGATION_SOUNDS = 21;

        public const int SET_FEATURE_ON_PROCESS = 2;

        private const uint TH32CS_SNAPPROCESS = 2u;

        public const uint MAX_PATH = 260u;

        public const int TVM_SETEXTENDEDSTYLE = 4396;

        public const int TVS_EX_DOUBLEBUFFER = 4;

        public const int GWL_HWNDPARENT = -8;

        public const int GWL_EXSTYLE = -20;

        public const int GWL_STYLE = -16;

        public const int GCL_HICON = -14;

        public const int GCL_HICONSM = -34;

        public const int WM_QUERYDRAGICON = 55;

        public const int WM_HOTKEY = 786;

        public const int WM_GETICON = 127;

        public const int WM_SETICON = 128;

        public const int WM_CLOSE = 16;

        public const int SC_CLOSE = 61536;

        public const int ICON_SMALL = 0;

        public const int ICON_BIG = 1;

        public const int SMTO_ABORTIFHUNG = 2;

        public const int SMTO_NOTIMEOUTIFNOTHUNG = 8;

        public const int TRUE = 1;

        public const int FALSE = 0;

        public const int WHITE_BRUSH = 0;

        public const int LTGRAY_BRUSH = 1;

        public const int GRAY_BRUSH = 2;

        public const int DKGRAY_BRUSH = 3;

        public const int BLACK_BRUSH = 4;

        public const int NULL_BRUSH = 5;

        public const int HOLLOW_BRUSH = 5;

        public const int WHITE_PEN = 6;

        public const int BLACK_PEN = 7;

        public const int NULL_PEN = 8;

        public const int OEM_FIXED_FONT = 10;

        public const int ANSI_FIXED_FONT = 11;

        public const int ANSI_VAR_FONT = 12;

        public const int SYSTEM_FONT = 13;

        public const int DEVICE_DEFAULT_FONT = 14;

        public const int DEFAULT_PALETTE = 15;

        public const int SYSTEM_FIXED_FONT = 16;

        public const int RDW_INVALIDATE = 1;

        public const int RDW_INTERNALPAINT = 2;

        public const int RDW_ERASE = 4;

        public const int RDW_VALIDATE = 8;

        public const int RDW_NOINTERNALPAINT = 16;

        public const int RDW_NOERASE = 32;

        public const int RDW_NOCHILDREN = 64;

        public const int RDW_ALLCHILDREN = 128;

        public const int RDW_UPDATENOW = 256;

        public const int RDW_ERASENOW = 512;

        public const int RDW_FRAME = 1024;

        public const int RDW_NOFRAME = 2048;

        public const int IDC_ARROW = 32512;

        public const int IDC_IBEAM = 32513;

        public const int IDC_WAIT = 32514;

        public const int IDC_CROSS = 32515;

        public const int IDC_UPARROW = 32516;

        public const int IDC_SIZE = 32640;

        public const int IDC_ICON = 32641;

        public const int IDC_SIZENWSE = 32642;

        public const int IDC_SIZENESW = 32643;

        public const int IDC_SIZEWE = 32644;

        public const int IDC_SIZENS = 32645;

        public const int IDC_SIZEALL = 32646;

        public const int IDC_NO = 32648;

        public const int IDC_HAND = 32649;

        public const int IDC_APPSTARTING = 32650;

        public const int IDC_HELP = 32651;

        public const int HIDE_WINDOW = 0;

        public const int SHOW_OPENWINDOW = 1;

        public const int SHOW_ICONWINDOW = 2;

        public const int SHOW_FULLSCREEN = 3;

        public const int SHOW_OPENNOACTIVATE = 4;

        public const int SW_PARENTCLOSING = 1;

        public const int SW_OTHERZOOM = 2;

        public const int SW_PARENTOPENING = 3;

        public const int SW_OTHERUNZOOM = 4;

        public const int SWP_NOSIZE = 1;

        public const int SWP_NOMOVE = 2;

        public const int SWP_NOZORDER = 4;

        public const int SWP_NOREDRAW = 8;

        public const int SWP_NOACTIVATE = 16;

        public const int SWP_FRAMECHANGED = 32;

        public const int SWP_SHOWWINDOW = 64;

        public const int SWP_HIDEWINDOW = 128;

        public const int SWP_NOCOPYBITS = 256;

        public const int SWP_NOOWNERZORDER = 512;

        public const int SWP_NOSENDCHANGING = 1024;

        public const int SWP_DRAWFRAME = 32;

        public const int SWP_NOREPOSITION = 512;

        public const int SWP_DEFERERASE = 8192;

        public const int SWP_ASYNCWINDOWPOS = 16384;

        public const int HWND_TOP = 0;

        public const int HWND_BOTTOM = 1;

        public const int HWND_TOPMOST = -1;

        public const int HWND_NOTOPMOST = -2;

        public const int MA_NOACTIVATE = 3;

        public const int SPI_SETSCREENSAVERACTIVE = 17;

        public const int SPIF_SENDWININICHANGE = 2;

        public const int AW_HOR_POSITIVE = 1;

        public const int AW_HOR_NEGATIVE = 2;

        public const int AW_VER_POSITIVE = 4;

        public const int AW_VER_NEGATIVE = 8;

        public const int AW_CENTER = 16;

        public const int AW_HIDE = 65536;

        public const int AW_ACTIVATE = 131072;

        public const int AW_SLIDE = 262144;

        public const int AW_BLEND = 524288;

        public static uint MOD_ALT = 1u;

        public static uint MOD_CONTROL = 2u;

        public static uint MOD_SHIFT = 4u;

        public static uint MOD_WIN = 8u;

        public static uint OBJID_NATIVEOM = 4294967280u;

        public static Guid IID_IDispatch = new Guid("{00020400-0000-0000-C000-000000000046}");

        public const int MAXIMUM_ALLOWED = 33554432;

        public const uint INVALID_SESSION_ID = uint.MaxValue;

        public const int ENUM_CURRENT_SETTINGS = -1;

        public const int DMDO_DEFAULT = 0;

        public const int DMDO_90 = 1;

        public const int DMDO_180 = 2;

        public const int DMDO_270 = 3;

        [DllImport("kernel32.dll")]
        public static extern bool GetVersionEx(ref OSVERSIONINFOEX osVersionInfo);

        [DllImport("urlmon.dll")]
        [return: MarshalAs(UnmanagedType.Error)]
        public static extern int CoInternetSetFeatureEnabled(int FeatureEntry, [MarshalAs(UnmanagedType.U4)] int dwFlags, bool fEnable);

        [DllImport("shell32.dll", EntryPoint = "FindExecutable")]
        public static extern long FindExecutableA(string lpFile, string lpDirectory, StringBuilder lpResult);

        public static string FindExecutable(string pv_strFilename)
        {
            StringBuilder stringBuilder = new StringBuilder(1024);
            long num = FindExecutableA(pv_strFilename, string.Empty, stringBuilder);
            if (num >= 32)
            {
                return stringBuilder.ToString();
            }

            return $"Error: ({num})";
        }

        public static uint GetParentProcessId()
        {
            return GetParentProcessId((uint)Process.GetCurrentProcess().Id);
        }

        public static uint GetParentProcessId(uint currentProcessId)
        {
            uint result = 0u;
            IntPtr intPtr = CreateToolhelp32Snapshot(2u, 0u);
            if (intPtr == IntPtr.Zero)
            {
                return 0u;
            }

            Processentry32 processentry = default;
            processentry.dwSize = (uint)Marshal.SizeOf(typeof(Processentry32));
            Processentry32 lppe = processentry;
            while (Process32Next(intPtr, ref lppe))
            {
                if (currentProcessId == lppe.th32ProcessID)
                {
                    result = lppe.th32ParentProcessID;
                    break;
                }
            }

            return result;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

        [DllImport("kernel32.dll")]
        private static extern bool Process32Next(IntPtr hSnapshot, ref Processentry32 lppe);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string name);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern bool ClientToScreen(IntPtr hwnd, ref Point lpPoint);

        [DllImport("user32.dll")]
        public static extern bool ScreenToClient(IntPtr hwnd, ref Point lpPoint);

        [DllImport("WinSpool.drv", SetLastError = true)]
        public static extern bool SetDefaultPrinterW([MarshalAs(UnmanagedType.LPWStr)] string pPrinterName);

        [DllImport("user32")]
        public static extern int SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32")]
        public static extern int ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll")]
        public static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

        [DllImport("user32.dll")]
        public static extern int GetWindowModuleFileName(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool BlockInput(bool fBlockIt);

        [DllImport("user32.dll")]
        public static extern bool GetWindowPlacement(IntPtr window, ref WindowPlacement position);

        [DllImport("User32.dll")]
        public static extern int RegisterWindowMessage(string message);

        [DllImport("User32.dll")]
        public static extern void EnumWindows(EnumWindowEventHandler callback, int lParam);

        [DllImport("User32.dll")]
        public static extern void EnumWindows(EventHandlerForEnumWindows callback, ref object extraData);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr parentHandle, EnumChildWindowsEventHandler callback, ref object extraData);

        [DllImport("user32.dll", EntryPoint = "SystemParametersInfo", SetLastError = true)]
        public static extern bool SystemParametersInfoGet(uint action, uint param, ref uint vparam, uint init);

        [DllImport("user32.dll", EntryPoint = "SystemParametersInfo", SetLastError = true)]
        public static extern bool SystemParametersInfoSet(uint action, uint param, uint vparam, uint init);

        [DllImport("User32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("User32.dll")]
        public static extern IntPtr GetWindow(IntPtr hWnd, int wCmd);

        [DllImport("User32.dll")]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        [DllImport("User32.dll")]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("User32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int index);

        [DllImport("user32", CharSet = CharSet.Auto)]
        public static extern int SendMessageTimeout(IntPtr hWnd, int uMsg, int wParam, StringBuilder lParam, int fuFlags, int uTimeout, IntPtr lpdwResult);

        [DllImport("user32", CharSet = CharSet.Ansi, EntryPoint = "SendMessageTimeoutA", ExactSpelling = true, SetLastError = true)]
        public static extern int SendMessageTimeout(IntPtr hWnd, int msg, int wParam, int lParam, int fuFlags, int uTimeout, ref int lpdwResult);

        [DllImport("User32.dll")]
        public static extern int GetClassLong(IntPtr hWnd, int index);

        [DllImport("User32.dll")]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        [DllImport("USER32.DLL", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hwnd, int msg, int wParam, StringBuilder sb);

        [DllImport("User32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("User32.dll")]
        public static extern int PostMessage(IntPtr hWnd, int uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("User32.dll")]
        public static extern void SwitchToThisWindow(IntPtr hWnd, int altTabActivated);

        [DllImport("User32.dll")]
        public static extern int ShowWindowAsync(IntPtr hWnd, int command);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetFocus();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern bool BringWindowToTop(IntPtr window);

        [DllImport("user32.dll")]
        public static extern bool GetWindowInfo(IntPtr hwnd, ref WindowInfo info);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hwnd, ref Rect rectangle);

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(Point pt);

        [DllImport("user32.dll")]
        public static extern IntPtr ChildWindowFromPoint(IntPtr hWnd, Point pt);

        [DllImport("user32.dll")]
        public static extern IntPtr RealChildWindowFromPoint(IntPtr hWnd, Point pt);

        [DllImport("user32.dll")]
        public static extern IntPtr SetCapture(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int ReleaseCapture();

        [DllImport("gdi32.dll")]
        public static extern IntPtr GetStockObject(int nObject);

        [DllImport("user32.dll")]
        public static extern int InvalidateRect(IntPtr hWnd, IntPtr lpRect, int bErase);

        [DllImport("user32.dll")]
        public static extern int UpdateWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool SystemParametersInfo(int uAction, int uParam, ref int lpvParam, int flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int AnimateWindow(IntPtr hwand, int dwTime, int dwFlags);

        [DllImport("shcore.dll")]
        public static extern IntPtr GetDpiForMonitor(IntPtr hmonitor, DpiType dpiType, out uint dpiX, out uint dpiY);

        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(IntPtr hdcDst, int xDst, int yDst, int cx, int cy, IntPtr hdcSrc, int xSrc, int ySrc, uint ulRop);

        [DllImport("gdi32.dll")]
        public static extern bool StretchBlt(IntPtr hdcDst, int xDst, int yDst, int cx, int cy, IntPtr hdcSrc, int xSrc, int ySrc, int cxSrc, int cySrc, uint ulRop);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateDC(IntPtr lpszDriver, string lpszDevice, IntPtr lpszOutput, IntPtr lpInitData);

        [DllImport("gdi32.dll")]
        public static extern IntPtr DeleteDC(IntPtr hdc);

        [DllImport("shell32.dll")]
        public static extern int SHGetFolderPath(IntPtr hwndOwner, int nFolder, IntPtr hToken, uint dwFlags, [Out] StringBuilder pszPath);

        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        public static extern uint SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, int dwFlags);

        [DllImport("Shell32.dll", CharSet = CharSet.Auto)]
        public static extern int ExtractIconEx(string lpszFile, int nIconIndex, IntPtr[] phIconLarge, IntPtr[] phIconSmall, int nIcons);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll", EntryPoint = "SendInput", SetLastError = true)]
        public static extern uint SendInput64(uint nInputs, INPUT64[] pInputs, int cbSize);

        [DllImport("Kernel32")]
        public static extern short GlobalAddAtom(string atomName);

        [DllImport("Kernel32")]
        public static extern short GlobalDeleteAtom(short atom);

        [DllImport("User32")]
        public static extern int LockWindowUpdate(IntPtr windowHandle);

        public static short MAKEWORD(byte a, byte b)
        {
            return (short)((byte)(a & 0xFF) | ((byte)(b & 0xFF) << 8));
        }

        public static byte LOBYTE(short a)
        {
            return (byte)((uint)a & 0xFFu);
        }

        public static byte HIBYTE(short a)
        {
            return (byte)(a >> 8);
        }

        public static int MAKELONG(short a, short b)
        {
            return (a & 0xFFFF) | ((b & 0xFFFF) << 16);
        }

        public static short HIWORD(int a)
        {
            return (short)(a >> 16);
        }

        public static short LOWORD(int a)
        {
            return (short)(a & 0xFFFF);
        }

        [DllImport("Kernel32")]
        public static extern int CopyFile(string source, string destination, int failIfExists);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWow64Process([In] IntPtr hProcess, out bool lpSystemInfo);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool AllowSetForegroundWindow(int dwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        public static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        public static extern int GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        public static extern int GetKeyState(int vKey);

        [DllImport("user32.dll")]
        public static extern bool SetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int DestroyIcon(IntPtr hIcon);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("shlwapi.dll")]
        public static extern bool PathIsNetworkPath(string path);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern int FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll")]
        public static extern bool FindClose(IntPtr hFindFile);

        [DllImport("oleacc.dll")]
        public static extern int AccessibleObjectFromWindow(IntPtr hwnd, uint idObject, ref Guid iid, [In][Out][MarshalAs(UnmanagedType.IUnknown)] ref object ppvObject);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int GetCurrentProcessId();

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC([In] IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleBitmap([In] IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);

        [DllImport("dwmapi.dll")]
        public static extern int DwmIsCompositionEnabled(out bool enabled);

        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hDc, IntPtr hObject);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateDIBSection(IntPtr hdc, [In] ref BITMAPINFO pbmi, uint iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

        [DllImport("gdi32.dll")]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport("gdi32.dll")]
        public static extern int GetBitmapBits(IntPtr hbmp, int cbBuffer, [Out] byte[] lpvBits);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateBitmap(int nWidth, int nHeight, uint cPlanes, uint cBitsPerPel, IntPtr lpvBits);

        [DllImport("advapi32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool CreateProcessAsUser(IntPtr hToken, string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandle, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool DuplicateTokenEx(IntPtr existingTokenHandle, uint dwDesiredAccess, IntPtr lpThreadAttributes, int tokenType, int impersonationLevel, ref IntPtr duplicateTokenHandle);

        [DllImport("userenv.dll", SetLastError = true)]
        public static extern bool CreateEnvironmentBlock(ref IntPtr environment, IntPtr token, bool inherit);

        [DllImport("userenv.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport("Wtsapi32.dll")]
        public static extern uint WTSQueryUserToken(uint SessionId, ref IntPtr phToken);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr OpenInputDesktop(uint dwFlags, bool fInherit, uint dwDesiredAccess);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool CloseDesktop(IntPtr hDesktop);

        [DllImport("Sas.dll")]
        public static extern void SendSAS(bool asUser);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool LockWorkStation();

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ExitWindowsEx(ExitWindows uFlags, ShutdownReason dwReason);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, out IntPtr phToken);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool DuplicateToken(IntPtr existingTokenHandle, SECURITY_IMPERSONATION_LEVEL securityImpersonationLevel, out IntPtr duplicateTokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool RevertToSelf();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern DisplayChangeResult ChangeDisplaySettingsEx(string lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd, uint dwflags, IntPtr lParam);

        [DllImport("psapi.dll")]
        public static extern uint GetProcessImageFileName(IntPtr hProcess, [Out] StringBuilder lpImageFileName, [In][MarshalAs(UnmanagedType.U4)] int nSize);

        public const int WH_MOUSE_LL = 14;
        public const int WH_KEYBOARD_LL = 13;
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_LBUTTONUP = 0x0202;
        public const int WM_KEYDOWN = 0x0100;

        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr WindowFromPoint(POINT Point);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }

}
