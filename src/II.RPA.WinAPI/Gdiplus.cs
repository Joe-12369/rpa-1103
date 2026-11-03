
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

    public class Gdiplus
    {
		[DllImport("gdiplus.dll", SetLastError = true)]
		public static extern int GdipCreateBitmapFromGdiDib(IntPtr bmpInfo, IntPtr pixData, out IntPtr image);


    }
}
