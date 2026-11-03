
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

    public class Userenv
    {
		[DllImport("userenv.dll", SetLastError = true)]
		public static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

		[DllImport("userenv.dll", SetLastError = true)]        [return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);


    }
}
