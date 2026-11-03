
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

    public class Gdi32
    {
		[DllImport("gdi32.dll", CharSet = CharSet.Auto)]
		public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

		[DllImport("gdi32.dll")]
		public static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int
    wDest, int hDest, IntPtr hdcSource, int xSrc, int ySrc, CopyPixelOperation rop);

		[DllImport("gdi32.dll")]
		public static extern IntPtr DeleteDC(IntPtr hDc);

		[DllImport("gdi32.dll")]
		public static extern IntPtr DeleteObject(IntPtr hDc);

		[DllImport("gdi32.dll")]
		public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

		[DllImport("gdi32.dll")]
		public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

		[DllImport("gdi32.dll")]
		public static extern IntPtr SelectObject(IntPtr hdc, IntPtr bmp);

		[DllImport("gdi32.dll")]
		public static extern IntPtr CreateDIBSection(IntPtr hdc, ref Win32Types.BitmapInfo bmi,
            uint usage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);


    }
}
