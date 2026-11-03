using System;
using System.Runtime.InteropServices;

namespace II.RPA.Compatibility
{
    // Linked into the assemblies that expose DPI helpers, including the standalone desktop tool.
    internal static class WindowsDpi
    {
        internal static int ForWindow(IntPtr window)
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 14393))
            {
                int dpi = GetDpiForWindow(window);
                if (dpi > 0) return dpi;
            }
            return LegacySystemDpi();
        }

        internal static int ForSystem()
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 14393))
            {
                int dpi = GetDpiForSystem();
                if (dpi > 0) return dpi;
            }
            return LegacySystemDpi();
        }

        internal static int LegacySystemDpi()
        {
            // Server 2012 has system-wide DPI. Always release the screen DC.
            IntPtr dc = GetDC(IntPtr.Zero);
            if (dc == IntPtr.Zero) return 96;
            try
            {
                int dpi = GetDeviceCaps(dc, 88); // LOGPIXELSX
                return dpi > 0 ? dpi : 96;
            }
            finally
            {
                ReleaseDC(IntPtr.Zero, dc);
            }
        }

        [DllImport("user32.dll")] private static extern int GetDpiForWindow(IntPtr window);
        [DllImport("user32.dll")] private static extern int GetDpiForSystem();
        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr window);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr window, IntPtr dc);
        [DllImport("gdi32.dll")] private static extern int GetDeviceCaps(IntPtr dc, int index);
    }
}
