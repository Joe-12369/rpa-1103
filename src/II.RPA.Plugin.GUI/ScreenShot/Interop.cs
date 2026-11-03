using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace II.RPA.Plugin.GUI.ScreenShot
{
    internal class InteropValues
    {
        internal static class ExternDll
        {
            public const string
                User32 = "user32.dll",
                Gdi32 = "gdi32.dll",
                GdiPlus = "gdiplus.dll",
                Kernel32 = "kernel32.dll",
                Shell32 = "shell32.dll",
                MsImg = "msimg32.dll",
                NTdll = "ntdll.dll",
                DwmApi = "dwmapi.dll";
        }

        internal delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

        internal delegate IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [return: MarshalAs(UnmanagedType.Bool)]
        internal delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        internal const int
            BITSPIXEL = 12,
            PLANES = 14,
            BI_RGB = 0,
            DIB_RGB_COLORS = 0,
            E_FAIL = unchecked((int)0x80004005),
            NIF_MESSAGE = 0x00000001,
            NIF_ICON = 0x00000002,
            NIF_TIP = 0x00000004,
            NIF_INFO = 0x00000010,
            NIM_ADD = 0x00000000,
            NIM_MODIFY = 0x00000001,
            NIM_DELETE = 0x00000002,
            NIIF_NONE = 0x00000000,
            NIIF_INFO = 0x00000001,
            NIIF_WARNING = 0x00000002,
            NIIF_ERROR = 0x00000003,
            WM_ACTIVATE = 0x0006,
            WM_QUIT = 0x0012,
            WM_GETMINMAXINFO = 0x0024,
            WM_WINDOWPOSCHANGING = 0x0046,
            WM_WINDOWPOSCHANGED = 0x0047,
            WM_SETICON = 0x0080,
            WM_NCCREATE = 0x0081,
            WM_NCDESTROY = 0x0082,
            WM_NCHITTEST = 0x0084,
            WM_NCACTIVATE = 0x0086,
            WM_NCRBUTTONDOWN = 0x00A4,
            WM_NCRBUTTONUP = 0x00A5,
            WM_NCRBUTTONDBLCLK = 0x00A6,
            WM_NCUAHDRAWCAPTION = 0x00AE,
            WM_NCUAHDRAWFRAME = 0x00AF,
            WM_KEYDOWN = 0x0100,
            WM_KEYUP = 0x0101,
            WM_SYSKEYDOWN = 0x0104,
            WM_SYSKEYUP = 0x0105,
            WM_SYSCOMMAND = 0x112,
            WM_MOUSEMOVE = 0x0200,
            WM_LBUTTONUP = 0x0202,
            WM_LBUTTONDBLCLK = 0x0203,
            WM_RBUTTONUP = 0x0205,
            WM_ENTERSIZEMOVE = 0x0231,
            WM_EXITSIZEMOVE = 0x0232,
            WM_CLIPBOARDUPDATE = 0x031D,
            WM_USER = 0x0400,
            WS_VISIBLE = 0x10000000,
            MF_BYCOMMAND = 0x00000000,
            MF_BYPOSITION = 0x400,
            MF_GRAYED = 0x00000001,
            MF_SEPARATOR = 0x800,
            TB_GETBUTTON = WM_USER + 23,
            TB_BUTTONCOUNT = WM_USER + 24,
            TB_GETITEMRECT = WM_USER + 29,
            VERTRES = 10,
            DESKTOPVERTRES = 117,
            LOGPIXELSX = 88,
            LOGPIXELSY = 90,
            SC_CLOSE = 0xF060,
            SC_SIZE = 0xF000,
            SC_MOVE = 0xF010,
            SC_MINIMIZE = 0xF020,
            SC_MAXIMIZE = 0xF030,
            SC_RESTORE = 0xF120,
            SRCCOPY = 0x00CC0020,
            MONITOR_DEFAULTTONEAREST = 0x00000002;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal class NOTIFYICONDATA
        {
            public int cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA));
            public IntPtr hWnd;
            public int uID;
            public int uFlags;
            public int uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip = string.Empty;
            public int dwState = 0x01;
            public int dwStateMask = 0x01;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo = string.Empty;
            public int uTimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle = string.Empty;
            public int dwInfoFlags;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct TBBUTTON
        {
            public int iBitmap;
            public int idCommand;
            public IntPtr fsStateStylePadding;
            public IntPtr dwData;
            public IntPtr iString;
        }

        [Flags]
        internal enum AllocationType
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000
        }

        [Flags]
        internal enum MemoryProtection
        {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct TRAYDATA
        {
            public IntPtr hwnd;
            public uint uID;
            public uint uCallbackMessage;
            public uint bReserved0;
            public uint bReserved1;
            public IntPtr hIcon;
        }

        [Flags]
        internal enum FreeType
        {
            Decommit = 0x4000,
            Release = 0x8000,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        internal enum HookType
        {
            WH_KEYBOARD_LL = 13,
            WH_MOUSE_LL = 14
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MOUSEHOOKSTRUCT
        {
            public POINT pt;
            public IntPtr hwnd;
            public uint wHitTestCode;
            public IntPtr dwExtraInfo;
        }

        [Flags]
        internal enum ProcessAccess
        {
            AllAccess = CreateThread | DuplicateHandle | QueryInformation | SetInformation | Terminate | VMOperation | VMRead | VMWrite | Synchronize,
            CreateThread = 0x2,
            DuplicateHandle = 0x40,
            QueryInformation = 0x400,
            SetInformation = 0x200,
            Terminate = 0x1,
            VMOperation = 0x8,
            VMRead = 0x10,
            VMWrite = 0x20,
            Synchronize = 0x100000
        }

        [Serializable, StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public RECT(Rect rect)
            {
                Left = (int)rect.Left;
                Top = (int)rect.Top;
                Right = (int)rect.Right;
                Bottom = (int)rect.Bottom;
            }

            public Point Position => new Point(Left, Top);
            public Size Size => new Size(Width, Height);

            public int Height
            {
                get => Bottom - Top;
                set => Bottom = Top + value;
            }

            public int Width
            {
                get => Right - Left;
                set => Right = Left + value;
            }
        }

        internal struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        internal enum GWL
        {
            STYLE = -16,
            EXSTYLE = -20
        }

        internal enum GWLP
        {
            WNDPROC = -4,
            HINSTANCE = -6,
            HWNDPARENT = -8,
            USERDATA = -21,
            ID = -12
        }

        internal struct BITMAPINFOHEADER
        {
            internal uint biSize;
            internal int biWidth;
            internal int biHeight;
            internal ushort biPlanes;
            internal ushort biBitCount;
            internal uint biCompression;
            internal uint biSizeImage;
            internal int biXPelsPerMeter;
            internal int biYPelsPerMeter;
            internal uint biClrUsed;
            internal uint biClrImportant;
        }

        [Flags]
        internal enum RedrawWindowFlags : uint
        {
            Invalidate = 1u,
            InternalPaint = 2u,
            Erase = 4u,
            Validate = 8u,
            NoInternalPaint = 16u,
            NoErase = 32u,
            NoChildren = 64u,
            AllChildren = 128u,
            UpdateNow = 256u,
            EraseNow = 512u,
            Frame = 1024u,
            NoFrame = 2048u
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public uint flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class WINDOWPLACEMENT
        {
            public int length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
            public int flags;
            public int showCmd;
            public POINT ptMinPosition;
            public POINT ptMaxPosition;
            public RECT rcNormalPosition;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct SIZE
        {
            [ComAliasName("Microsoft.VisualStudio.OLE.Interop.LONG")]
            public int cx;
            [ComAliasName("Microsoft.VisualStudio.OLE.Interop.LONG")]
            public int cy;
        }

        internal struct MONITORINFO
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        internal enum SM
        {
            CXSCREEN = 0,
            CYSCREEN = 1,
            CXVSCROLL = 2,
            CYHSCROLL = 3,
            CYCAPTION = 4,
            CXBORDER = 5,
            CYBORDER = 6,
            CXFIXEDFRAME = 7,
            CYFIXEDFRAME = 8,
            CYVTHUMB = 9,
            CXHTHUMB = 10,
            CXICON = 11,
            CYICON = 12,
            CXCURSOR = 13,
            CYCURSOR = 14,
            CYMENU = 15,
            CXFULLSCREEN = 16,
            CYFULLSCREEN = 17,
            CYKANJIWINDOW = 18,
            MOUSEPRESENT = 19,
            CYVSCROLL = 20,
            CXHSCROLL = 21,
            DEBUG = 22,
            SWAPBUTTON = 23,
            CXMIN = 28,
            CYMIN = 29,
            CXSIZE = 30,
            CYSIZE = 31,
            CXFRAME = 32,
            CXSIZEFRAME = CXFRAME,
            CYFRAME = 33,
            CYSIZEFRAME = CYFRAME,
            CXMINTRACK = 34,
            CYMINTRACK = 35,
            CXDOUBLECLK = 36,
            CYDOUBLECLK = 37,
            CXICONSPACING = 38,
            CYICONSPACING = 39,
            MENUDROPALIGNMENT = 40,
            PENWINDOWS = 41,
            DBCSENABLED = 42,
            CMOUSEBUTTONS = 43,
            SECURE = 44,
            CXEDGE = 45,
            CYEDGE = 46,
            CXMINSPACING = 47,
            CYMINSPACING = 48,
            CXSMICON = 49,
            CYSMICON = 50,
            CYSMCAPTION = 51,
            CXSMSIZE = 52,
            CYSMSIZE = 53,
            CXMENUSIZE = 54,
            CYMENUSIZE = 55,
            ARRANGE = 56,
            CXMINIMIZED = 57,
            CYMINIMIZED = 58,
            CXMAXTRACK = 59,
            CYMAXTRACK = 60,
            CXMAXIMIZED = 61,
            CYMAXIMIZED = 62,
            NETWORK = 63,
            CLEANBOOT = 67,
            CXDRAG = 68,
            CYDRAG = 69,
            SHOWSOUNDS = 70,
            CXMENUCHECK = 71,
            CYMENUCHECK = 72,
            SLOWMACHINE = 73,
            MIDEASTENABLED = 74,
            MOUSEWHEELPRESENT = 75,
            XVIRTUALSCREEN = 76,
            YVIRTUALSCREEN = 77,
            CXVIRTUALSCREEN = 78,
            CYVIRTUALSCREEN = 79,
            CMONITORS = 80,
            SAMEDISPLAYFORMAT = 81,
            IMMENABLED = 82,
            CXFOCUSBORDER = 83,
            CYFOCUSBORDER = 84,
            TABLETPC = 86,
            MEDIACENTER = 87,
            REMOTESESSION = 0x1000,
            REMOTECONTROL = 0x2001
        }

        internal enum CacheSlot
        {
            DpiX,

            FocusBorderWidth,
            FocusBorderHeight,
            HighContrast,
            MouseVanish,

            DropShadow,
            FlatMenu,
            WorkAreaInternal,
            WorkArea,

            IconMetrics,

            KeyboardCues,
            KeyboardDelay,
            KeyboardPreference,
            KeyboardSpeed,
            SnapToDefaultButton,
            WheelScrollLines,
            MouseHoverTime,
            MouseHoverHeight,
            MouseHoverWidth,

            MenuDropAlignment,
            MenuFade,
            MenuShowDelay,

            ComboBoxAnimation,
            ClientAreaAnimation,
            CursorShadow,
            GradientCaptions,
            HotTracking,
            ListBoxSmoothScrolling,
            MenuAnimation,
            SelectionFade,
            StylusHotTracking,
            ToolTipAnimation,
            ToolTipFade,
            UIEffects,

            MinimizeAnimation,
            Border,
            CaretWidth,
            ForegroundFlashCount,
            DragFullWindows,
            NonClientMetrics,

            ThinHorizontalBorderHeight,
            ThinVerticalBorderWidth,
            CursorWidth,
            CursorHeight,
            ThickHorizontalBorderHeight,
            ThickVerticalBorderWidth,
            MinimumHorizontalDragDistance,
            MinimumVerticalDragDistance,
            FixedFrameHorizontalBorderHeight,
            FixedFrameVerticalBorderWidth,
            FocusHorizontalBorderHeight,
            FocusVerticalBorderWidth,
            FullPrimaryScreenWidth,
            FullPrimaryScreenHeight,
            HorizontalScrollBarButtonWidth,
            HorizontalScrollBarHeight,
            HorizontalScrollBarThumbWidth,
            IconWidth,
            IconHeight,
            IconGridWidth,
            IconGridHeight,
            MaximizedPrimaryScreenWidth,
            MaximizedPrimaryScreenHeight,
            MaximumWindowTrackWidth,
            MaximumWindowTrackHeight,
            MenuCheckmarkWidth,
            MenuCheckmarkHeight,
            MenuButtonWidth,
            MenuButtonHeight,
            MinimumWindowWidth,
            MinimumWindowHeight,
            MinimizedWindowWidth,
            MinimizedWindowHeight,
            MinimizedGridWidth,
            MinimizedGridHeight,
            MinimumWindowTrackWidth,
            MinimumWindowTrackHeight,
            PrimaryScreenWidth,
            PrimaryScreenHeight,
            WindowCaptionButtonWidth,
            WindowCaptionButtonHeight,
            ResizeFrameHorizontalBorderHeight,
            ResizeFrameVerticalBorderWidth,
            SmallIconWidth,
            SmallIconHeight,
            SmallWindowCaptionButtonWidth,
            SmallWindowCaptionButtonHeight,
            VirtualScreenWidth,
            VirtualScreenHeight,
            VerticalScrollBarWidth,
            VerticalScrollBarButtonHeight,
            WindowCaptionHeight,
            KanjiWindowHeight,
            MenuBarHeight,
            VerticalScrollBarThumbHeight,
            IsImmEnabled,
            IsMediaCenter,
            IsMenuDropRightAligned,
            IsMiddleEastEnabled,
            IsMousePresent,
            IsMouseWheelPresent,
            IsPenWindows,
            IsRemotelyControlled,
            IsRemoteSession,
            ShowSounds,
            IsSlowMachine,
            SwapButtons,
            IsTabletPC,
            VirtualScreenLeft,
            VirtualScreenTop,

            PowerLineStatus,

            IsGlassEnabled,
            UxThemeName,
            UxThemeColor,
            WindowCornerRadius,
            WindowGlassColor,
            WindowGlassBrush,
            WindowNonClientFrameThickness,
            WindowResizeBorderThickness,

            NumSlots
        }

        internal static class Win32Constant
        {
            internal const int MAX_PATH = 260;
            internal const int INFOTIPSIZE = 1024;
            internal const int TRUE = 1;
            internal const int FALSE = 0;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct WNDCLASS
        {
            public uint style;
            public Delegate lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszMenuName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszClassName;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal class WNDCLASS4ICON
        {
            public int style;
            public WndProc lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        internal struct BITMAPINFO
        {
            public int biSize;

            public int biWidth;

            public int biHeight;

            public short biPlanes;

            public short biBitCount;

            public int biCompression;

            public int biSizeImage;

            public int biXPelsPerMeter;

            public int biYPelsPerMeter;

            public int biClrUsed;

            public int biClrImportant;

            public BITMAPINFO(int width, int height, short bpp)
            {
                biSize = SizeOf();
                biWidth = width;
                biHeight = height;
                biPlanes = 1;
                biBitCount = bpp;
                biCompression = 0;
                biSizeImage = 0;
                biXPelsPerMeter = 0;
                biYPelsPerMeter = 0;
                biClrUsed = 0;
                biClrImportant = 0;
            }

            [SecuritySafeCritical]
            private static int SizeOf()
            {
                return Marshal.SizeOf(typeof(BITMAPINFO));
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class ICONINFO
        {
            public bool fIcon = false;
            public int xHotspot = 0;
            public int yHotspot = 0;
            public BitmapHandle hbmMask = null;
            public BitmapHandle hbmColor = null;
        }

        internal enum WINDOWCOMPOSITIONATTRIB
        {
            WCA_ACCENT_POLICY = 19
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WINCOMPATTRDATA
        {
            public WINDOWCOMPOSITIONATTRIB Attribute;
            public IntPtr Data;
            public int DataSize;
        }

        internal enum ACCENTSTATE
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
            ACCENT_INVALID_STATE = 5
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ACCENTPOLICY
        {
            public ACCENTSTATE AccentState;
            public int AccentFlags;
            public uint GradientColor;
            public int AnimationId;
        }

        [ComImport, Guid("0000000C-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IStream
        {
            int Read([In] IntPtr buf, [In] int len);

            int Write([In] IntPtr buf, [In] int len);

            [return: MarshalAs(UnmanagedType.I8)]
            long Seek([In, MarshalAs(UnmanagedType.I8)] long dlibMove, [In] int dwOrigin);

            void SetSize([In, MarshalAs(UnmanagedType.I8)] long libNewSize);

            [return: MarshalAs(UnmanagedType.I8)]
            long CopyTo([In, MarshalAs(UnmanagedType.Interface)] IStream pstm, [In, MarshalAs(UnmanagedType.I8)] long cb, [Out, MarshalAs(UnmanagedType.LPArray)] long[] pcbRead);

            void Commit([In] int grfCommitFlags);

            void Revert();

            void LockRegion([In, MarshalAs(UnmanagedType.I8)] long libOffset, [In, MarshalAs(UnmanagedType.I8)] long cb, [In] int dwLockType);

            void UnlockRegion([In, MarshalAs(UnmanagedType.I8)] long libOffset, [In, MarshalAs(UnmanagedType.I8)] long cb, [In] int dwLockType);

            void Stat([In] IntPtr pStatstg, [In] int grfStatFlag);

            [return: MarshalAs(UnmanagedType.Interface)]
            IStream Clone();
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        internal class StreamConsts
        {
            public const int LOCK_WRITE = 0x1;
            public const int LOCK_EXCLUSIVE = 0x2;
            public const int LOCK_ONLYONCE = 0x4;
            public const int STATFLAG_DEFAULT = 0x0;
            public const int STATFLAG_NONAME = 0x1;
            public const int STATFLAG_NOOPEN = 0x2;
            public const int STGC_DEFAULT = 0x0;
            public const int STGC_OVERWRITE = 0x1;
            public const int STGC_ONLYIFCURRENT = 0x2;
            public const int STGC_DANGEROUSLYCOMMITMERELYTODISKCACHE = 0x4;
            public const int STREAM_SEEK_SET = 0x0;
            public const int STREAM_SEEK_CUR = 0x1;
            public const int STREAM_SEEK_END = 0x2;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal class ImageCodecInfoPrivate
        {
            [MarshalAs(UnmanagedType.Struct)]
            public Guid Clsid;
            [MarshalAs(UnmanagedType.Struct)]
            public Guid FormatID;

            public IntPtr CodecName = IntPtr.Zero;
            public IntPtr DllName = IntPtr.Zero;
            public IntPtr FormatDescription = IntPtr.Zero;
            public IntPtr FilenameExtension = IntPtr.Zero;
            public IntPtr MimeType = IntPtr.Zero;

            public int Flags;
            public int Version;
            public int SigCount;
            public int SigSize;

            public IntPtr SigPattern = IntPtr.Zero;
            public IntPtr SigMask = IntPtr.Zero;
        }

        internal class ComStreamFromDataStream : IStream
        {
            protected Stream DataStream;

            // to support seeking ahead of the stream length...
            private long _virtualPosition = -1;

            internal ComStreamFromDataStream(Stream dataStream)
            {
                this.DataStream = dataStream ?? throw new ArgumentNullException(nameof(dataStream));
            }

            private void ActualizeVirtualPosition()
            {
                if (_virtualPosition == -1) return;

                if (_virtualPosition > DataStream.Length)
                    DataStream.SetLength(_virtualPosition);

                DataStream.Position = _virtualPosition;

                _virtualPosition = -1;
            }

            public virtual IStream Clone()
            {
                NotImplemented();
                return null;
            }

            public virtual void Commit(int grfCommitFlags)
            {
                DataStream.Flush();
                ActualizeVirtualPosition();
            }

            public virtual long CopyTo(IStream pstm, long cb, long[] pcbRead)
            {
                const int bufsize = 4096; // one page
                var buffer = Marshal.AllocHGlobal(bufsize);
                if (buffer == IntPtr.Zero) throw new OutOfMemoryException();
                long written = 0;

                try
                {
                    while (written < cb)
                    {
                        var toRead = bufsize;
                        if (written + toRead > cb) toRead = (int)(cb - written);
                        var read = Read(buffer, toRead);
                        if (read == 0) break;
                        if (pstm.Write(buffer, read) != read)
                        {
                            throw EFail("Wrote an incorrect number of bytes");
                        }
                        written += read;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
                if (pcbRead != null && pcbRead.Length > 0)
                {
                    pcbRead[0] = written;
                }

                return written;
            }

            public virtual Stream GetDataStream() => DataStream;

            public virtual void LockRegion(long libOffset, long cb, int dwLockType)
            {
            }

            protected static ExternalException EFail(string msg) => throw new ExternalException(msg, E_FAIL);

            protected static void NotImplemented() => throw new NotImplementedException();

            public virtual int Read(IntPtr buf, int length)
            {
                var buffer = new byte[length];
                var count = Read(buffer, length);
                Marshal.Copy(buffer, 0, buf, length);
                return count;
            }

            public virtual int Read(byte[] buffer, int length)
            {
                ActualizeVirtualPosition();
                return DataStream.Read(buffer, 0, length);
            }

            public virtual void Revert() => NotImplemented();

            public virtual long Seek(long offset, int origin)
            {
                var pos = _virtualPosition;
                if (_virtualPosition == -1)
                {
                    pos = DataStream.Position;
                }
                var len = DataStream.Length;

                switch (origin)
                {
                    case StreamConsts.STREAM_SEEK_SET:
                        if (offset <= len)
                        {
                            DataStream.Position = offset;
                            _virtualPosition = -1;
                        }
                        else
                        {
                            _virtualPosition = offset;
                        }
                        break;
                    case StreamConsts.STREAM_SEEK_END:
                        if (offset <= 0)
                        {
                            DataStream.Position = len + offset;
                            _virtualPosition = -1;
                        }
                        else
                        {
                            _virtualPosition = len + offset;
                        }
                        break;
                    case StreamConsts.STREAM_SEEK_CUR:
                        if (offset + pos <= len)
                        {
                            DataStream.Position = pos + offset;
                            _virtualPosition = -1;
                        }
                        else
                        {
                            _virtualPosition = offset + pos;
                        }
                        break;
                }

                return _virtualPosition != -1 ? _virtualPosition : DataStream.Position;
            }

            public virtual void SetSize(long value) => DataStream.SetLength(value);

            public virtual void Stat(IntPtr pstatstg, int grfStatFlag) => NotImplemented();

            public virtual void UnlockRegion(long libOffset, long cb, int dwLockType)
            {
            }

            public virtual int Write(IntPtr buf, int length)
            {
                var buffer = new byte[length];
                Marshal.Copy(buf, buffer, 0, length);
                return Write(buffer, length);
            }

            public virtual int Write(byte[] buffer, int length)
            {
                ActualizeVirtualPosition();
                DataStream.Write(buffer, 0, length);
                return length;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public RECT rc;
            public int lParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RTL_OSVERSIONINFOEX
        {
            internal uint dwOSVersionInfoSize;
            internal uint dwMajorVersion;
            internal uint dwMinorVersion;
            internal uint dwBuildNumber;
            internal uint dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal string szCSDVersion;
        }

        [Flags]
        public enum WindowPositionFlags
        {
            /// <summary>
            ///     If the calling thread and the thread that owns the window are attached to different input queues, the system posts
            ///     the request to the thread that owns the window. This prevents the calling thread from blocking its execution while
            ///     other threads process the request.
            /// </summary>
            SWP_ASYNCWINDOWPOS = 0x4000,

            /// <summary>
            ///     Prevents generation of the WM_SYNCPAINT message.
            /// </summary>
            SWP_DEFERERASE = 0x2000,

            /// <summary>
            ///     Draws a frame (defined in the window's class description) around the window.
            /// </summary>
            SWP_DRAWFRAME = 0x0020,

            /// <summary>
            ///     Applies new frame styles set using the SetWindowLong function. Sends a WM_NCCALCSIZE message to the window, even if
            ///     the window's size is not being changed. If this flag is not specified, WM_NCCALCSIZE is sent only when the window's
            ///     size is being changed.
            /// </summary>
            SWP_FRAMECHANGED = 0x0020,

            /// <summary>
            ///     Hides the window.
            /// </summary>
            SWP_HIDEWINDOW = 0x0080,

            /// <summary>
            ///     Does not activate the window. If this flag is not set, the window is activated and moved to the top of either the
            ///     topmost or non-topmost group (depending on the setting of the hWndInsertAfter parameter).
            /// </summary>
            SWP_NOACTIVATE = 0x0010,

            /// <summary>
            ///     Discards the entire contents of the client area. If this flag is not specified, the valid contents of the client
            ///     area are saved and copied back into the client area after the window is sized or repositioned.
            /// </summary>
            SWP_NOCOPYBITS = 0x0100,

            /// <summary>
            ///     Retains the current position (ignores X and Y parameters).
            /// </summary>
            SWP_NOMOVE = 0x0002,

            /// <summary>
            ///     Does not change the owner window's position in the Z order.
            /// </summary>
            SWP_NOOWNERZORDER = 0x0200,

            /// <summary>
            ///     Does not redraw changes. If this flag is set, no repainting of any kind occurs. This applies to the client area,
            ///     the nonclient area (including the title bar and scroll bars), and any part of the parent window uncovered as a
            ///     result of the window being moved. When this flag is set, the application must explicitly invalidate or redraw any
            ///     parts of the window and parent window that need redrawing.
            /// </summary>
            SWP_NOREDRAW = 0x0008,

            /// <summary>
            ///     Same as the SWP_NOOWNERZORDER flag.
            /// </summary>
            SWP_NOREPOSITION = 0x0200,

            /// <summary>
            ///     Prevents the window from receiving the WM_WINDOWPOSCHANGING message.
            /// </summary>
            SWP_NOSENDCHANGING = 0x0400,

            /// <summary>
            ///     Retains the current size (ignores the cx and cy parameters).
            /// </summary>
            SWP_NOSIZE = 0x0001,

            /// <summary>
            ///     Retains the current Z order (ignores the hWndInsertAfter parameter).
            /// </summary>
            SWP_NOZORDER = 0x0004,

            /// <summary>
            ///     Displays the window.
            /// </summary>
            SWP_SHOWWINDOW = 0x0040
        }
    }

    internal class InteropMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.SysInt)]
        public static extern IntPtr WindowFromPoint(InteropValues.POINT Point);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetParent(IntPtr hWnd);
        #region common

        internal const int E_FAIL = unchecked((int)0x80004005);

        internal static readonly IntPtr HRGN_NONE = new IntPtr(-1);

        [DllImport(InteropValues.ExternDll.User32, CharSet = CharSet.Auto)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern int RegisterWindowMessage(string msg);

        [DllImport(InteropValues.ExternDll.Kernel32, SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, out InteropValues.TBBUTTON lpBuffer,
            int dwSize, out int lpNumberOfBytesRead);

        [DllImport(InteropValues.ExternDll.Kernel32, SetLastError = true)]
        internal static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, out InteropValues.RECT lpBuffer,
            int dwSize, out int lpNumberOfBytesRead);

        [DllImport(InteropValues.ExternDll.Kernel32, SetLastError = true)]
        internal static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, out InteropValues.TRAYDATA lpBuffer,
            int dwSize, out int lpNumberOfBytesRead);

        [DllImport(InteropValues.ExternDll.User32, CharSet = CharSet.Auto)]
        internal static extern uint SendMessage(IntPtr hWnd, uint Msg, uint wParam, IntPtr lParam);

        [DllImport(InteropValues.ExternDll.User32, SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport(InteropValues.ExternDll.User32, SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool AttachThreadInput(in uint currentForegroundWindowThreadId,
            in uint thisWindowThreadId, bool isAttach);

        [DllImport(InteropValues.ExternDll.User32, SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport(InteropValues.ExternDll.Kernel32, SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern IntPtr OpenProcess(InteropValues.ProcessAccess dwDesiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

        [DllImport(InteropValues.ExternDll.Kernel32, SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, int dwSize,
            InteropValues.AllocationType flAllocationType, InteropValues.MemoryProtection flProtect);

        [DllImport(InteropValues.ExternDll.Kernel32, SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int CloseHandle(IntPtr hObject);

        [DllImport(InteropValues.ExternDll.Kernel32, SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, InteropValues.FreeType dwFreeType);

        [DllImport(InteropValues.ExternDll.User32, SetLastError = true)]
        internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport(InteropValues.ExternDll.User32, SetLastError = true)]
        internal static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass,
            string lpszWindow);

        [DllImport(InteropValues.ExternDll.User32)]
        internal static extern int GetWindowRect(IntPtr hwnd, out InteropValues.RECT lpRect);

        [DllImport(InteropValues.ExternDll.User32, CharSet = CharSet.Auto)]
        internal static extern bool GetCursorPos(out InteropValues.POINT pt);

        [DllImport(InteropValues.ExternDll.User32)]
        internal static extern IntPtr GetDesktopWindow();

        [DllImport(InteropValues.ExternDll.User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport(InteropValues.ExternDll.User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        [DllImport(InteropValues.ExternDll.User32)]
        internal static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport(InteropValues.ExternDll.User32)]
        internal static extern bool EnableMenuItem(IntPtr hMenu, int UIDEnabledItem, int uEnable);

        [DllImport(InteropValues.ExternDll.User32)]
        internal static extern bool InsertMenu(IntPtr hMenu, int wPosition, int wFlags, int wIDNewItem, string lpNewItem);

        [DllImport(InteropValues.ExternDll.User32, ExactSpelling = true, EntryPoint = "DestroyMenu", CharSet = CharSet.Auto)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool IntDestroyMenu(HandleRef hMenu);

        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport(InteropValues.ExternDll.User32, SetLastError = true, ExactSpelling = true, EntryPoint = nameof(GetDC),
            CharSet = CharSet.Auto)]
        internal static extern IntPtr IntGetDC(HandleRef hWnd);

        [SecurityCritical]
        internal static IntPtr GetDC(HandleRef hWnd)
        {
            var hDc = IntGetDC(hWnd);
            if (hDc == IntPtr.Zero) throw new Win32Exception();

            return HandleCollector.Add(hDc, CommonHandles.HDC);
        }

        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport(InteropValues.ExternDll.User32, ExactSpelling = true, EntryPoint = nameof(ReleaseDC), CharSet = CharSet.Auto)]
        internal static extern int IntReleaseDC(HandleRef hWnd, HandleRef hDC);

        [SecurityCritical]
        internal static int ReleaseDC(HandleRef hWnd, HandleRef hDC)
        {
            HandleCollector.Remove((IntPtr)hDC, CommonHandles.HDC);
            return IntReleaseDC(hWnd, hDC);
        }

        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport(InteropValues.ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        internal static extern int GetDeviceCaps(HandleRef hDC, int nIndex);

        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport(InteropValues.ExternDll.User32)]
        internal static extern int GetSystemMetrics(InteropValues.SM nIndex);

        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport(InteropValues.ExternDll.User32, EntryPoint = nameof(DestroyIcon), CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool IntDestroyIcon(IntPtr hIcon);

        [SecurityCritical]
        internal static bool DestroyIcon(IntPtr hIcon)
        {
            var result = IntDestroyIcon(hIcon);
            return result;
        }

        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport(InteropValues.ExternDll.Gdi32, EntryPoint = nameof(DeleteObject), CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool IntDeleteObject(IntPtr hObject);

        [SecurityCritical]
        internal static bool DeleteObject(IntPtr hObject)
        {
            var result = IntDeleteObject(hObject);
            return result;
        }



        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport(InteropValues.ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto,
            EntryPoint = nameof(CreateBitmap))]
        private static extern BitmapHandle PrivateCreateBitmap(int width, int height, int planes, int bitsPerPixel,
            byte[] lpvBits);

        [SecurityCritical]
        internal static BitmapHandle CreateBitmap(int width, int height, int planes, int bitsPerPixel, byte[] lpvBits)
        {
            var hBitmap = PrivateCreateBitmap(width, height, planes, bitsPerPixel, lpvBits);
            return hBitmap;
        }

        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport(InteropValues.ExternDll.Kernel32, EntryPoint = "GetModuleFileName", CharSet = CharSet.Unicode,
            SetLastError = true)]
        private static extern int IntGetModuleFileName(HandleRef hModule, StringBuilder buffer, int length);

        [SecurityCritical]
        internal static string GetModuleFileName(HandleRef hModule)
        {
            var buffer = new StringBuilder(InteropValues.Win32Constant.MAX_PATH);
            while (true)
            {
                var size = IntGetModuleFileName(hModule, buffer, buffer.Capacity);
                if (size == 0) throw new Win32Exception();

                if (size == buffer.Capacity)
                {
                    buffer.EnsureCapacity(buffer.Capacity * 2);
                    continue;
                }

                return buffer.ToString();
            }
        }


        [DllImport(InteropValues.ExternDll.Shell32, CharSet = CharSet.Auto)]
        internal static extern int Shell_NotifyIcon(int message, InteropValues.NOTIFYICONDATA pnid);

        [SecurityCritical]
        [DllImport(InteropValues.ExternDll.User32, SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CreateWindowExW")]
        internal static extern IntPtr CreateWindowEx(
            int dwExStyle,
            [MarshalAs(UnmanagedType.LPWStr)] string lpClassName,
            [MarshalAs(UnmanagedType.LPWStr)] string lpWindowName,
            int dwStyle,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport(InteropValues.ExternDll.User32, CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        internal static extern short RegisterClass(InteropValues.WNDCLASS4ICON wc);

        [DllImport(InteropValues.ExternDll.User32, CharSet = CharSet.Auto)]
        internal static extern IntPtr DefWindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport(InteropValues.ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto)]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport(InteropValues.ExternDll.User32, CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport(InteropValues.ExternDll.Kernel32, CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport(InteropValues.ExternDll.User32, CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport(InteropValues.ExternDll.User32, CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr SetWindowsHookEx(int idHook, InteropValues.HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport(InteropValues.ExternDll.User32, SetLastError = true)]
        internal static extern IntPtr GetWindowDC(IntPtr window);

        [DllImport(InteropValues.ExternDll.Gdi32, SetLastError = true)]
        internal static extern uint GetPixel(IntPtr dc, int x, int y);

        [DllImport(InteropValues.ExternDll.User32, SetLastError = true)]
        internal static extern int ReleaseDC(IntPtr window, IntPtr dc);

        [DllImport(InteropValues.ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        internal static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport(InteropValues.ExternDll.User32, CharSet = CharSet.Auto)]
        internal static extern IntPtr GetDC(IntPtr ptr);

        [DllImport(InteropValues.ExternDll.User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowPlacement(IntPtr hwnd, InteropValues.WINDOWPLACEMENT lpwndpl);

        internal static InteropValues.WINDOWPLACEMENT GetWindowPlacement(IntPtr hwnd)
        {
            InteropValues.WINDOWPLACEMENT wINDOWPLACEMENT = new InteropValues.WINDOWPLACEMENT();
            if (GetWindowPlacement(hwnd, wINDOWPLACEMENT))
            {
                return wINDOWPLACEMENT;
            }
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        internal static int GetXLParam(int lParam) => LoWord(lParam);

        internal static int GetYLParam(int lParam) => HiWord(lParam);

        internal static int HiWord(int value) => (short)(value >> 16);

        internal static int LoWord(int value) => (short)(value & 65535);

        [DllImport(InteropValues.ExternDll.User32)]
        internal static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

        [DllImport(InteropValues.ExternDll.User32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EnumThreadWindows(uint dwThreadId, InteropValues.EnumWindowsProc lpfn, IntPtr lParam);

        [DllImport(InteropValues.ExternDll.Gdi32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteDC(IntPtr hdc);

        [DllImport(InteropValues.ExternDll.Gdi32, SetLastError = true)]
        internal static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport(InteropValues.ExternDll.Gdi32, ExactSpelling = true, SetLastError = true)]
        internal static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport(InteropValues.ExternDll.User32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr SendMessage(IntPtr hWnd, int nMsg, IntPtr wParam, IntPtr lParam);

        [DllImport(InteropValues.ExternDll.User32)]
        internal static extern IntPtr MonitorFromPoint(InteropValues.POINT pt, int flags);

        [DllImport(InteropValues.ExternDll.User32)]
        internal static extern IntPtr GetWindow(IntPtr hwnd, int nCmd);

        [DllImport(InteropValues.ExternDll.User32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWindowVisible(IntPtr hwnd);

        [DllImport(InteropValues.ExternDll.User32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsIconic(IntPtr hwnd);

        [DllImport(InteropValues.ExternDll.User32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsZoomed(IntPtr hwnd);

        [DllImport(InteropValues.ExternDll.User32, CharSet = CharSet.Auto, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int flags);

        internal static System.Windows.Point GetCursorPos()
        {
            var result = default(System.Windows.Point);
            if (GetCursorPos(out var point))
            {
                result.X = point.X;
                result.Y = point.Y;
            }
            return result;
        }

        [DllImport(InteropValues.ExternDll.User32)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        internal static int GetWindowLong(IntPtr hWnd, InteropValues.GWL nIndex) => GetWindowLong(hWnd, (int)nIndex);

        internal static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 4)
            {
                return SetWindowLongPtr32(hWnd, nIndex, dwNewLong);
            }
            return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
        }

        [DllImport(InteropValues.ExternDll.User32, CharSet = CharSet.Auto, EntryPoint = "SetWindowLong")]
        internal static extern IntPtr SetWindowLongPtr32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport(InteropValues.ExternDll.User32, CharSet = CharSet.Auto, EntryPoint = "SetWindowLongPtr")]
        internal static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport(InteropValues.ExternDll.User32, CharSet = CharSet.Unicode)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport(InteropValues.ExternDll.User32, CharSet = CharSet.Unicode)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        internal static IntPtr SetWindowLongPtr(IntPtr hWnd, InteropValues.GWLP nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
            {
                return SetWindowLongPtr(hWnd, (int)nIndex, dwNewLong);
            }
            return new IntPtr(SetWindowLong(hWnd, (int)nIndex, dwNewLong.ToInt32()));
        }

        internal static int SetWindowLong(IntPtr hWnd, InteropValues.GWL nIndex, int dwNewLong) => SetWindowLong(hWnd, (int)nIndex, dwNewLong);

        [DllImport(InteropValues.ExternDll.User32, CharSet = CharSet.Unicode)]
        internal static extern ushort RegisterClass(ref InteropValues.WNDCLASS lpWndClass);

        [DllImport(InteropValues.ExternDll.Kernel32)]
        internal static extern uint GetCurrentThreadId();

        [DllImport(InteropValues.ExternDll.User32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr CreateWindowEx(int dwExStyle, IntPtr classAtom, string lpWindowName, int dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport(InteropValues.ExternDll.User32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DestroyWindow(IntPtr hwnd);

        [DllImport(InteropValues.ExternDll.User32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnregisterClass(IntPtr classAtom, IntPtr hInstance);

        [DllImport(InteropValues.ExternDll.User32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDest, ref InteropValues.POINT pptDest, ref InteropValues.SIZE psize, IntPtr hdcSrc, ref InteropValues.POINT pptSrc, uint crKey, [In] ref InteropValues.BLENDFUNCTION pblend, uint dwFlags);

        [DllImport(InteropValues.ExternDll.User32)]
        internal static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, InteropValues.RedrawWindowFlags flags);

        [DllImport(InteropValues.ExternDll.User32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, InteropValues.EnumMonitorsDelegate lpfnEnum, IntPtr dwData);

        [DllImport(InteropValues.ExternDll.User32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IntersectRect(out InteropValues.RECT lprcDst, [In] ref InteropValues.RECT lprcSrc1, [In] ref InteropValues.RECT lprcSrc2);

        [DllImport(InteropValues.ExternDll.User32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, ref InteropValues.MONITORINFO monitorInfo);

        [DllImport(InteropValues.ExternDll.Gdi32, SetLastError = true)]
        internal static extern IntPtr CreateDIBSection(IntPtr hdc, ref InteropValues.BITMAPINFO pbmi, uint iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

        [DllImport(InteropValues.ExternDll.MsImg)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AlphaBlend(IntPtr hdcDest, int xoriginDest, int yoriginDest, int wDest, int hDest, IntPtr hdcSrc, int xoriginSrc, int yoriginSrc, int wSrc, int hSrc, InteropValues.BLENDFUNCTION pfn);

        internal static int GET_SC_WPARAM(IntPtr wParam) => (int)wParam & 65520;

        [DllImport(InteropValues.ExternDll.User32)]
        internal static extern IntPtr ChildWindowFromPointEx(IntPtr hwndParent, InteropValues.POINT pt, int uFlags);

        [DllImport(InteropValues.ExternDll.Gdi32)]
        internal static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int width, int height);

        [DllImport(InteropValues.ExternDll.Gdi32)]
        internal static extern bool BitBlt(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);

        [DllImport(InteropValues.ExternDll.User32)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool EnableWindow(IntPtr hWnd, bool enable);

        [ReflectionPermission(SecurityAction.Assert, Unrestricted = true), SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.UnmanagedCode)]
        internal static object PtrToStructure(IntPtr lparam, Type cls) => Marshal.PtrToStructure(lparam, cls);

        [ReflectionPermission(SecurityAction.Assert, Unrestricted = true),
         SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.UnmanagedCode)]
        internal static void PtrToStructure(IntPtr lparam, object data) => Marshal.PtrToStructure(lparam, data);

        [DllImport(InteropValues.ExternDll.Shell32, CallingConvention = CallingConvention.StdCall)]
        internal static extern uint SHAppBarMessage(int dwMessage, ref InteropValues.APPBARDATA pData);

        [SecurityCritical]
        [DllImport(InteropValues.ExternDll.DwmApi, EntryPoint = "DwmGetColorizationColor", PreserveSig = true)]
        internal static extern int DwmGetColorizationColor(out uint pcrColorization, out bool pfOpaqueBlend);

        #endregion

        internal class Gdip
        {
            private const string ThreadDataSlotName = "system.drawing.threaddata";

            private static IntPtr InitToken;

            private static bool Initialized => InitToken != IntPtr.Zero;

            internal const int
                Ok = 0,
                GenericError = 1,
                InvalidParameter = 2,
                OutOfMemory = 3,
                ObjectBusy = 4,
                InsufficientBuffer = 5,
                NotImplemented = 6,
                Win32Error = 7,
                WrongState = 8,
                Aborted = 9,
                FileNotFound = 10,
                ValueOverflow = 11,
                AccessDenied = 12,
                UnknownImageFormat = 13,
                FontFamilyNotFound = 14,
                FontStyleNotFound = 15,
                NotTrueTypeFont = 16,
                UnsupportedGdiplusVersion = 17,
                GdiplusNotInitialized = 18,
                PropertyNotFound = 19,
                PropertyNotSupported = 20,
                E_UNEXPECTED = unchecked((int)0x8000FFFF);

            static Gdip()
            {
                Initialize();
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct StartupInput
            {
                private int GdiplusVersion;

                private readonly IntPtr DebugEventCallback;

                private bool SuppressBackgroundThread;

                private bool SuppressExternalCodecs;

                public static StartupInput GetDefault()
                {
                    var result = new StartupInput
                    {
                        GdiplusVersion = 1,
                        SuppressBackgroundThread = false,
                        SuppressExternalCodecs = false
                    };
                    return result;
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            private readonly struct StartupOutput
            {
                private readonly IntPtr hook;

                private readonly IntPtr unhook;
            }

            [ResourceExposure(ResourceScope.None)]
            [ResourceConsumption(ResourceScope.AppDomain, ResourceScope.AppDomain)]
            private static void Initialize()
            {
                var input = StartupInput.GetDefault();

                var status = GdiplusStartup(out InitToken, ref input, out _);

                if (status != Ok)
                {
                    throw StatusException(status);
                }

                var currentDomain = AppDomain.CurrentDomain;
                currentDomain.ProcessExit += OnProcessExit;

                if (!currentDomain.IsDefaultAppDomain())
                {
                    currentDomain.DomainUnload += OnProcessExit;
                }
            }

            [PrePrepareMethod]
            [ResourceExposure(ResourceScope.AppDomain)]
            [ResourceConsumption(ResourceScope.AppDomain)]
            private static void OnProcessExit(object sender, EventArgs e) => Shutdown();

            [ResourceExposure(ResourceScope.AppDomain)]
            [ResourceConsumption(ResourceScope.AppDomain)]
            private static void Shutdown()
            {
                if (Initialized)
                {
                    ClearThreadData();
                    // unhook our shutdown handlers as we do not need to shut down more than once
                    var currentDomain = AppDomain.CurrentDomain;
                    currentDomain.ProcessExit -= OnProcessExit;
                    if (!currentDomain.IsDefaultAppDomain())
                    {
                        currentDomain.DomainUnload -= OnProcessExit;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void ClearThreadData()
            {
                var slot = Thread.GetNamedDataSlot(ThreadDataSlotName);
                Thread.SetData(slot, null);
            }

            [DllImport(InteropValues.ExternDll.GdiPlus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.None)]
            internal static extern int GdipImageGetFrameDimensionsCount(HandleRef image, out int count);

            internal static Exception StatusException(int status)
            {
                return new ExternalException("GdiplusUnknown");
                //    status switch
                //{
                //    GenericError => new ExternalException("GdiplusGenericError"),
                //    InvalidParameter => new ArgumentException("GdiplusInvalidParameter"),
                //    OutOfMemory => new OutOfMemoryException("GdiplusOutOfMemory"),
                //    ObjectBusy => new InvalidOperationException("GdiplusObjectBusy"),
                //    InsufficientBuffer => new OutOfMemoryException("GdiplusInsufficientBuffer"),
                //    NotImplemented => new NotImplementedException("GdiplusNotImplemented"),
                //    Win32Error => new ExternalException("GdiplusGenericError"),
                //    WrongState => new InvalidOperationException("GdiplusWrongState"),
                //    Aborted => new ExternalException("GdiplusAborted"),
                //    FileNotFound => new FileNotFoundException("GdiplusFileNotFound"),
                //    ValueOverflow => new OverflowException("GdiplusOverflow"),
                //    AccessDenied => new ExternalException("GdiplusAccessDenied"),
                //    UnknownImageFormat => new ArgumentException("GdiplusUnknownImageFormat"),
                //    PropertyNotFound => new ArgumentException("GdiplusPropertyNotFoundError"),
                //    PropertyNotSupported => new ArgumentException("GdiplusPropertyNotSupportedError"),
                //    FontFamilyNotFound => new ArgumentException("GdiplusFontFamilyNotFound"),
                //    FontStyleNotFound => new ArgumentException("GdiplusFontStyleNotFound"),
                //    NotTrueTypeFont => new ArgumentException("GdiplusNotTrueTypeFont_NoName"),
                //    UnsupportedGdiplusVersion => new ExternalException("GdiplusUnsupportedGdiplusVersion"),
                //    GdiplusNotInitialized => new ExternalException("GdiplusNotInitialized"),
                //    _ => new ExternalException("GdiplusUnknown")
                //};
            }

            [DllImport(InteropValues.ExternDll.GdiPlus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.None)]
            internal static extern int GdipImageGetFrameDimensionsList(HandleRef image, IntPtr buffer, int count);

            [DllImport(InteropValues.ExternDll.GdiPlus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.None)]
            internal static extern int GdipImageGetFrameCount(HandleRef image, ref Guid dimensionId, int[] count);

            [DllImport(InteropValues.ExternDll.GdiPlus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.None)]
            internal static extern int GdipGetPropertyItemSize(HandleRef image, int propid, out int size);

            [DllImport(InteropValues.ExternDll.GdiPlus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.None)]
            internal static extern int GdipGetPropertyItem(HandleRef image, int propid, int size, IntPtr buffer);

            [DllImport(InteropValues.ExternDll.GdiPlus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.Machine)]
            internal static extern int GdipCreateHBITMAPFromBitmap(HandleRef nativeBitmap, out IntPtr hbitmap, int argbBackground);

            [DllImport(InteropValues.ExternDll.GdiPlus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.None)]
            internal static extern int GdipImageSelectActiveFrame(HandleRef image, ref Guid dimensionId, int frameIndex);

            [DllImport(InteropValues.ExternDll.GdiPlus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.Machine)]
            internal static extern int GdipCreateBitmapFromFile(string filename, out IntPtr bitmap);

            [DllImport(InteropValues.ExternDll.GdiPlus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.None)]
            internal static extern int GdipImageForceValidation(HandleRef image);

            [DllImport(InteropValues.ExternDll.GdiPlus, SetLastError = true, ExactSpelling = true, EntryPoint = "GdipDisposeImage", CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.None)]
            private static extern int IntGdipDisposeImage(HandleRef image);

            internal static int GdipDisposeImage(HandleRef image)
            {
                if (!Initialized) return Ok;
                var result = IntGdipDisposeImage(image);
                return result;
            }

            [DllImport(InteropValues.ExternDll.GdiPlus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.Process)]
            private static extern int GdiplusStartup(out IntPtr token, ref StartupInput input, out StartupOutput output);

            [DllImport(InteropValues.ExternDll.GdiPlus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.None)]
            internal static extern int GdipGetImageRawFormat(HandleRef image, ref Guid format);

            [DllImport(InteropValues.ExternDll.User32)]
            internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref InteropValues.WINCOMPATTRDATA data);

            [DllImport(InteropValues.ExternDll.GdiPlus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.Machine)]
            internal static extern int GdipCreateBitmapFromStream(InteropValues.IStream stream, out IntPtr bitmap);

            [DllImport(InteropValues.ExternDll.GdiPlus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.Machine)]
            internal static extern int GdipCreateBitmapFromHBITMAP(HandleRef hbitmap, HandleRef hpalette, out IntPtr bitmap);

            [DllImport(InteropValues.ExternDll.GdiPlus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.None)]
            internal static extern int GdipGetImageEncodersSize(out int numEncoders, out int size);

            [DllImport(InteropValues.ExternDll.GdiPlus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.None)]
            internal static extern int GdipGetImageDecodersSize(out int numDecoders, out int size);

            [DllImport(InteropValues.ExternDll.GdiPlus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.None)]
            internal static extern int GdipGetImageDecoders(int numDecoders, int size, IntPtr decoders);

            [DllImport(InteropValues.ExternDll.GdiPlus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.None)]
            internal static extern int GdipGetImageEncoders(int numEncoders, int size, IntPtr encoders);

            [DllImport(InteropValues.ExternDll.GdiPlus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.None)]
            internal static extern int GdipSaveImageToStream(HandleRef image, InteropValues.IStream stream, ref Guid classId, HandleRef encoderParams);

            [DllImport(InteropValues.ExternDll.NTdll)]
            internal static extern int RtlGetVersion(out InteropValues.RTL_OSVERSIONINFOEX lpVersionInformation);

        }
    }

    internal static class ValueBoxes
    {
        internal static object TrueBox = true;

        internal static object FalseBox = false;

        internal static object Double0Box = .0;

        internal static object Double01Box = .1;

        internal static object Double1Box = 1.0;

        internal static object Double10Box = 10.0;

        internal static object Double20Box = 20.0;

        internal static object Double100Box = 100.0;

        internal static object Double200Box = 200.0;

        internal static object Double300Box = 300.0;

        internal static object DoubleNeg1Box = -1.0;

        internal static object Int0Box = 0;

        internal static object Int1Box = 1;

        internal static object Int2Box = 2;

        internal static object Int5Box = 5;

        internal static object Int99Box = 99;

        internal static object BooleanBox(bool value) => value ? TrueBox : FalseBox;
    }
    internal sealed class BitmapHandle : WpfSafeHandle
    {
        [SecurityCritical]
        private BitmapHandle() : this(true)
        {
            //请不要删除此构造函数，否则当使用自定义ico文件时会报错
        }

        [SecurityCritical]
        private BitmapHandle(bool ownsHandle) : base(ownsHandle, CommonHandles.GDI)
        {
        }

        [SecurityCritical]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected override bool ReleaseHandle()
        {
            return InteropMethods.DeleteObject(handle);
        }

        [SecurityCritical]
        internal HandleRef MakeHandleRef(object obj)
        {
            return new HandleRef(obj, handle);
        }

        [SecurityCritical]
        internal static BitmapHandle CreateFromHandle(IntPtr hbitmap, bool ownsHandle = true)
        {
            return new BitmapHandle(ownsHandle)
            {
                handle = hbitmap,
            };
        }
    }
    internal abstract class WpfSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private readonly int _collectorId;

        [SecurityCritical]
        protected WpfSafeHandle(bool ownsHandle, int collectorId) : base(ownsHandle)
        {
            HandleCollector.Add(collectorId);
            _collectorId = collectorId;
        }

        [SecurityCritical, SecuritySafeCritical]
        protected override void Dispose(bool disposing)
        {
            HandleCollector.Remove(_collectorId);
            base.Dispose(disposing);
        }
    }
    internal static class CommonHandles
    {
        public static readonly int Icon = HandleCollector.RegisterType(nameof(Icon), 20, 500);

        public static readonly int HDC = HandleCollector.RegisterType(nameof(HDC), 100, 2);

        public static readonly int GDI = HandleCollector.RegisterType(nameof(GDI), 50, 500);

        public static readonly int Kernel = HandleCollector.RegisterType(nameof(Kernel), 0, 1000);
    }

    internal static class HandleCollector
    {
        private static HandleType[] HandleTypes;
        private static int HandleTypeCount;

        private static readonly object HandleMutex = new object();

        internal static IntPtr Add(IntPtr handle, int type)
        {
            HandleTypes[type - 1].Add();
            return handle;
        }

        [System.Security.SecuritySafeCritical]
        internal static SafeHandle Add(SafeHandle handle, int type)
        {
            HandleTypes[type - 1].Add();
            return handle;
        }

        internal static void Add(int type)
        {
            HandleTypes[type - 1].Add();
        }

        internal static int RegisterType(string typeName, int expense, int initialThreshold)
        {
            lock (HandleMutex)
            {
                if (HandleTypeCount == 0 || HandleTypeCount == HandleTypes.Length)
                {
                    HandleType[] newTypes = new HandleType[HandleTypeCount + 10];
                    if (HandleTypes != null)
                    {
                        Array.Copy(HandleTypes, 0, newTypes, 0, HandleTypeCount);
                    }
                    HandleTypes = newTypes;
                }

                HandleTypes[HandleTypeCount++] = new HandleType(expense, initialThreshold);
                return HandleTypeCount;
            }
        }

        internal static IntPtr Remove(IntPtr handle, int type)
        {
            HandleTypes[type - 1].Remove();
            return handle;
        }

        [System.Security.SecuritySafeCritical]
        internal static SafeHandle Remove(SafeHandle handle, int type)
        {
            HandleTypes[type - 1].Remove();
            return handle;
        }

        internal static void Remove(int type)
        {
            HandleTypes[type - 1].Remove();
        }

        private class HandleType
        {
            private readonly int _initialThreshHold;
            private int _threshHold;
            private int _handleCount;
            private readonly int _deltaPercent;

            internal HandleType(int expense, int initialThreshHold)
            {
                _initialThreshHold = initialThreshHold;
                _threshHold = initialThreshHold;
                _deltaPercent = 100 - expense;
            }

            internal void Add()
            {
                lock (this)
                {
                    _handleCount++;
                    var performCollect = NeedCollection();

                    if (!performCollect)
                    {
                        return;
                    }
                }

                GC.Collect();

                var sleep = (100 - _deltaPercent) / 4;
                System.Threading.Thread.Sleep(sleep);
            }

            private bool NeedCollection()
            {

                if (_handleCount > _threshHold)
                {
                    _threshHold = _handleCount + _handleCount * _deltaPercent / 100;
                    return true;
                }

                var oldThreshHold = 100 * _threshHold / (100 + _deltaPercent);
                if (oldThreshHold >= _initialThreshHold && _handleCount < (int)(oldThreshHold * .9F))
                {
                    _threshHold = oldThreshHold;
                }

                return false;
            }

            internal void Remove()
            {
                lock (this)
                {
                    _handleCount--;

                    _handleCount = Math.Max(0, _handleCount);
                }
            }
        }
    }
    internal sealed class ImageCodecInfo
    {
        private string _dllName;

        public Guid Clsid { get; set; }

        public Guid FormatID { get; set; }

        public string CodecName { get; set; }

        public string DllName
        {
            [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity")]
            get
            {
                if (_dllName != null)
                {
                    new System.Security.Permissions.FileIOPermission(System.Security.Permissions.FileIOPermissionAccess.PathDiscovery, _dllName).Demand();
                }
                return _dllName;
            }
            [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity")]
            set
            {
                if (value != null)
                {
                    new System.Security.Permissions.FileIOPermission(System.Security.Permissions.FileIOPermissionAccess.PathDiscovery, value).Demand();
                }
                _dllName = value;
            }
        }

        public string FormatDescription { get; set; }

        public string FilenameExtension { get; set; }

        public string MimeType { get; set; }

        public ImageCodecFlags Flags { get; set; }

        public int Version { get; set; }

        public byte[][] SignaturePatterns { get; set; }

        public byte[][] SignatureMasks { get; set; }

        public static ImageCodecInfo[] GetImageDecoders()
        {
            ImageCodecInfo[] imageCodecs;

            var status = InteropMethods.Gdip.GdipGetImageDecodersSize(out var numDecoders, out var size);

            if (status != InteropMethods.Gdip.Ok)
            {
                throw InteropMethods.Gdip.StatusException(status);
            }

            var memory = Marshal.AllocHGlobal(size);

            try
            {
                status = InteropMethods.Gdip.GdipGetImageDecoders(numDecoders, size, memory);

                if (status != InteropMethods.Gdip.Ok)
                {
                    throw InteropMethods.Gdip.StatusException(status);
                }

                imageCodecs = ConvertFromMemory(memory, numDecoders);
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }

            return imageCodecs;
        }

        public static ImageCodecInfo[] GetImageEncoders()
        {
            ImageCodecInfo[] imageCodecs;

            var status = InteropMethods.Gdip.GdipGetImageEncodersSize(out var numEncoders, out var size);

            if (status != InteropMethods.Gdip.Ok)
            {
                throw InteropMethods.Gdip.StatusException(status);
            }

            var memory = Marshal.AllocHGlobal(size);

            try
            {
                status = InteropMethods.Gdip.GdipGetImageEncoders(numEncoders, size, memory);

                if (status != InteropMethods.Gdip.Ok)
                {
                    throw InteropMethods.Gdip.StatusException(status);
                }

                imageCodecs = ConvertFromMemory(memory, numEncoders);
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }

            return imageCodecs;
        }

        public static ImageCodecInfo[] ConvertFromMemory(IntPtr memoryStart, int numCodecs)
        {
            var codecs = new ImageCodecInfo[numCodecs];

            int index;

            for (index = 0; index < numCodecs; index++)
            {
                var curcodec = (IntPtr)((long)memoryStart + Marshal.SizeOf(typeof(InteropValues.ImageCodecInfoPrivate)) * index);
                var codecp = new InteropValues.ImageCodecInfoPrivate();
                InteropMethods.PtrToStructure(curcodec, codecp);

                codecs[index] = new ImageCodecInfo
                {
                    Clsid = codecp.Clsid,
                    FormatID = codecp.FormatID,
                    CodecName = Marshal.PtrToStringUni(codecp.CodecName),
                    DllName = Marshal.PtrToStringUni(codecp.DllName),
                    FormatDescription = Marshal.PtrToStringUni(codecp.FormatDescription),
                    FilenameExtension = Marshal.PtrToStringUni(codecp.FilenameExtension),
                    MimeType = Marshal.PtrToStringUni(codecp.MimeType),
                    Flags = (ImageCodecFlags)codecp.Flags,
                    Version = codecp.Version,
                    SignaturePatterns = new byte[codecp.SigCount][],
                    SignatureMasks = new byte[codecp.SigCount][]
                };

                for (var j = 0; j < codecp.SigCount; j++)
                {
                    codecs[index].SignaturePatterns[j] = new byte[codecp.SigSize];
                    codecs[index].SignatureMasks[j] = new byte[codecp.SigSize];

                    Marshal.Copy((IntPtr)((long)codecp.SigMask + j * codecp.SigSize), codecs[index].SignatureMasks[j], 0, codecp.SigSize);
                    Marshal.Copy((IntPtr)((long)codecp.SigPattern + j * codecp.SigSize), codecs[index].SignaturePatterns[j], 0, codecp.SigSize);
                }
            }

            return codecs;
        }
    }
    [Flags]
    internal enum ImageCodecFlags
    {
        Encoder = 0x00000001,
        Decoder = 0x00000002,
        SupportBitmap = 0x00000004,
        SupportVector = 0x00000008,
        SeekableEncode = 0x00000010,
        BlockingDecode = 0x00000020,
        Builtin = 0x00010000,
        System = 0x00020000,
        User = 0x00040000
    }
    internal class MouseHookEventArgs : EventArgs
    {
        public MouseHookMessageType MessageType { get; set; }

        public InteropValues.POINT Point { get; set; }
    }
    public enum MouseHookMessageType
    {
        LeftButtonDown = 0x0201,
        LeftButtonUp = 0x0202,
        MouseMove = 0x0200,
        MouseWheel = 0x020A,
        RightButtonDown = 0x0204,
        RightButtonUp = 0x0205
    }
    internal class MouseHook
    {
        public static event EventHandler<MouseHookEventArgs> StatusChanged;

        private static IntPtr HookId = IntPtr.Zero;

        private static readonly InteropValues.HookProc Proc = HookCallback;

        private static int Count;

        public static void Start()
        {
            if (HookId == IntPtr.Zero)
            {
                HookId = SetHook(Proc);
            }

            if (HookId != IntPtr.Zero)
            {
                Count++;
            }
        }

        public static void Stop()
        {
            Count--;
            if (Count < 1)
            {
                InteropMethods.UnhookWindowsHookEx(HookId);
                HookId = IntPtr.Zero;
            }
        }

        private static IntPtr SetHook(InteropValues.HookProc proc)
        {
            using (var curProcess = Process.GetCurrentProcess())
            {
                using (var curModule = curProcess.MainModule)
                {
                    if (curModule != null)
                    {
                        return InteropMethods.SetWindowsHookEx((int)InteropValues.HookType.WH_MOUSE_LL, proc,
                            InteropMethods.GetModuleHandle(curModule.ModuleName), 0);
                    }
                    return IntPtr.Zero;
                }
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0) return InteropMethods.CallNextHookEx(HookId, nCode, wParam, lParam);
            var hookStruct = (InteropValues.MOUSEHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(InteropValues.MOUSEHOOKSTRUCT));
            StatusChanged?.Invoke(null, new MouseHookEventArgs
            {
                MessageType = (MouseHookMessageType)wParam,
                Point = new InteropValues.POINT(hookStruct.pt.X, hookStruct.pt.Y)
            });

            return InteropMethods.CallNextHookEx(HookId, nCode, wParam, lParam);
        }
    }
}
