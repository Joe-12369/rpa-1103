using System.Runtime.InteropServices;
using II.RPA.Compatibility;
using Xunit;

namespace II.RPA.Plugin.Core.Tests;

public sealed class WindowsDpiCompatibilityTests
{
    [Fact]
    public void Legacy_system_dpi_matches_screen_device_caps()
    {
        var dc = GetDC(IntPtr.Zero);
        Assert.NotEqual(IntPtr.Zero, dc);
        try
        {
            Assert.Equal(GetDeviceCaps(dc, 88), WindowsDpi.LegacySystemDpi());
        }
        finally
        {
            ReleaseDC(IntPtr.Zero, dc);
        }
    }

    [Fact]
    public void Invalid_window_uses_positive_system_dpi_instead_of_zero()
    {
        Assert.Equal(WindowsDpi.LegacySystemDpi(), WindowsDpi.ForWindow(IntPtr.Zero));
        Assert.True(WindowsDpi.ForSystem() > 0);
    }

    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr window);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr window, IntPtr dc);
    [DllImport("gdi32.dll")] private static extern int GetDeviceCaps(IntPtr dc, int index);
}
