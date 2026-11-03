using II.RPA.WinAPI;
using II.RPA.WinAPI.Struct;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace II.RPA.Plugin.Window.Core
{
    internal static partial class User32X
    {
        public enum GET_ANCESTOR_FLAGS
        {
            GA_PARENT = 1,
            GA_ROOT = 2,
            GA_ROOTOWNER = 3,
        }

        public enum WINDOW_LONG_PTR_INDEX
        {
            GWLP_USERDATA = -21, // 0xFFFFFFEB
            GWL_USERDATA = -21, // 0xFFFFFFEB
            GWL_EXSTYLE = -20, // 0xFFFFFFEC
            GWL_STYLE = -16, // 0xFFFFFFF0
            GWLP_ID = -12, // 0xFFFFFFF4
            GWL_ID = -12, // 0xFFFFFFF4
            GWLP_HWNDPARENT = -8, // 0xFFFFFFF8
            GWL_HWNDPARENT = -8, // 0xFFFFFFF8
            GWLP_HINSTANCE = -6, // 0xFFFFFFFA
            GWL_HINSTANCE = -6, // 0xFFFFFFFA
            GWLP_WNDPROC = -4, // 0xFFFFFFFC
            GWL_WNDPROC = -4, // 0xFFFFFFFC
        }

        public enum GET_WINDOW_CMD
        {
            GW_HWNDFIRST,
            GW_HWNDLAST,
            GW_HWNDNEXT,
            GW_HWNDPREV,
            GW_OWNER,
            GW_CHILD,
            GW_ENABLEDPOPUP,
        }

        public enum DWMWINDOWATTRIBUTE
        {
            DWMWA_NCRENDERING_ENABLED = 1,
            DWMWA_NCRENDERING_POLICY = 2,
            DWMWA_TRANSITIONS_FORCEDISABLED = 3,
            DWMWA_ALLOW_NCPAINT = 4,
            DWMWA_CAPTION_BUTTON_BOUNDS = 5,
            DWMWA_NONCLIENT_RTL_LAYOUT = 6,
            DWMWA_FORCE_ICONIC_REPRESENTATION = 7,
            DWMWA_FLIP3D_POLICY = 8,
            DWMWA_EXTENDED_FRAME_BOUNDS = 9,
            DWMWA_HAS_ICONIC_BITMAP = 10, // 0x0000000A
            DWMWA_DISALLOW_PEEK = 11, // 0x0000000B
            DWMWA_EXCLUDED_FROM_PEEK = 12, // 0x0000000C
            DWMWA_CLOAK = 13, // 0x0000000D
            DWMWA_CLOAKED = 14, // 0x0000000E
            DWMWA_FREEZE_REPRESENTATION = 15, // 0x0000000F
            DWMWA_PASSIVE_UPDATE_MODE = 16, // 0x00000010
            DWMWA_USE_HOSTBACKDROPBRUSH = 17, // 0x00000011
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20, // 0x00000014
            DWMWA_WINDOW_CORNER_PREFERENCE = 33, // 0x00000021
            DWMWA_BORDER_COLOR = 34, // 0x00000022
            DWMWA_CAPTION_COLOR = 35, // 0x00000023
            DWMWA_TEXT_COLOR = 36, // 0x00000024
            DWMWA_VISIBLE_FRAME_BORDER_THICKNESS = 37, // 0x00000025
            DWMWA_LAST = 38, // 0x00000026
        }

        [DllImport("dwmapi")]
        public static extern unsafe int DwmGetWindowAttribute(
            IntPtr hwnd,
            DWMWINDOWATTRIBUTE dwAttribute,
            void* pvAttribute,
            uint cbAttribute);

        [DllImport("user32")]
        public static extern IntPtr GetAncestor(IntPtr hwnd, GET_ANCESTOR_FLAGS gaFlags);

        public static unsafe bool IsInvisibleWin10Background(IntPtr _hWnd)
        {
            if (Environment.OSVersion.Version.Major < 6)
            {
                return false;
            }

            int structure;

            if (
                User32X.DwmGetWindowAttribute(
                    _hWnd,
                    DWMWINDOWATTRIBUTE.DWMWA_CLOAKED,
                    (void*)&structure,
                    (uint)Marshal.SizeOf<int>(structure))
                < 0)
            {
                structure = 0;
            }

            return structure != 0;
        }

        //public static IntPtr[] GetWindowsOnTaskbar()
        //{
        //    List<IntPtr> hWnds = new List<IntPtr>();

        //    User32.EnumWindows(new EnumWindowsProc((hWnd, lparam) =>
        //    {
        //        if (!User32.IsWindowVisible((int)hWnd))
        //        {
        //            return true;
        //        }

        //        int windowLong = User32.GetWindowLong(hWnd, (int)WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);

        //        if ((windowLong & 128) != 0)
        //        {
        //            return true;
        //        }

        //        if ((windowLong & 134217728) != 0)
        //        {
        //            return true;
        //        }

        //        if (User32X.IsInvisibleWin10Background(hWnd))
        //        {
        //            return true;
        //        }

        //        if (User32.GetWindow(hWnd, (uint)GET_WINDOW_CMD.GW_OWNER) != IntPtr.Zero)
        //        {
        //            return true;
        //        }

        //        hWnds.Add(hWnd);
        //        return true;
        //    }), User32.GetDesktopWindow());
        //    return hWnds.ToArray();
        //}

        //public static List<IntPtr> QueryAllChildWindows(
        //  IntPtr hwndParent,
        //  Func<IntPtr, bool> predicate)
        //{
        //    var enumWindowsProcData = new User32X.EnumWindowsProcData()
        //    {
        //        hWndList = new List<IntPtr>(),
        //        Predicate = predicate
        //    };
        //    User32.EnumChildWindows(hwndParent, new EnumWindowsProc((hWnd, lParam) =>
        //    {
        //        if (enumWindowsProcData.Predicate == null || enumWindowsProcData.Predicate(hWnd))
        //        {
        //            enumWindowsProcData.hWndList.Add(hWnd);
        //        }
        //        return true;
        //    }), IntPtr.Zero);

        //    return enumWindowsProcData.hWndList;
        //}

        //private class EnumWindowsProcData
        //{
        //    public List<IntPtr> hWndList { get; set; }

        //    public Func<IntPtr, bool> Predicate { get; set; }
        //}
    }
}
