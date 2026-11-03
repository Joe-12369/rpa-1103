using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows;

namespace II.Framework.UI.Common
{
    public class WindowBrushHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        private enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19,
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }
        private enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
            ACCENT_INVALID_STATE = 5,
        }
        [Flags]
        public enum DWMWINDOWATTRIBUTE
        {
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
            DWMWA_SYSTEMBACKDROP_TYPE = 38
        }
        public enum BlurMode
        {
            None = 1,
            Acrylic = 3,
            Mica = 2,
            Tabbed = 4
        }
        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, ref int pvAttribute, int cbAttribute);
        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
        public static int SetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE attribute, int parameter)
           => DwmSetWindowAttribute(hwnd, attribute, ref parameter, Marshal.SizeOf<int>());

        public static void SetWindowBlur(IntPtr handle, int color, BlurMode mode)
        {
            SetWindowAttribute(
                handle,
                DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                color);
            SetWindowAttribute(
                handle,
                DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
                (int)mode);
        }
        public static void SetAcrylicBrush(Window window, Color color)
        {
            if (window.ResizeMode != (ResizeMode.CanMinimize | ResizeMode.NoResize))
            {
                window.ResizeMode = ResizeMode.CanMinimize;
            }
            var _blurColor = color.R << 0 | color.G << 8 | color.B << 16 | color.A << 24;
            var handle = new WindowInteropHelper(window).EnsureHandle();
            var accent = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                GradientColor = _blurColor
            };
            var accentPolicySize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentPolicySize);
            Marshal.StructureToPtr(accent, accentPtr, false);
            try
            {
                var data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    SizeOfData = accentPolicySize,
                    Data = accentPtr,
                };
                SetWindowCompositionAttribute(handle, ref data);
            }
            finally
            {
                Marshal.FreeHGlobal(accentPtr);
            }
        }
        public static void SetAcrylicBrush(Window window)
        {
            SetAcrylicBrush(window, Color.FromArgb(180, 255, 255, 255));
        }
        public static void SetAcrylicBrushWin11(Window window, bool DarkMode = false)
        {
            var handle = new WindowInteropHelper(window).EnsureHandle();
            var wc = WindowChrome.GetWindowChrome(window);
            if (wc != null)
            {
                wc.GlassFrameThickness = new Thickness(-1);
            }
            else
            {
                WindowChrome.SetWindowChrome(window, new WindowChrome()
                {
                    GlassFrameThickness = new Thickness(-1),
                    CaptionHeight = 1
                });
            }
            SetWindowBlur(handle, DarkMode ? 1 : 0, BlurMode.Acrylic);
        }
    }
}
