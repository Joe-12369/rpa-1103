
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

    public class Shell32
    {
		[DllImport("shell32.dll", EntryPoint = "CommandLineToArgvW", CharSet = CharSet.Unicode)]        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "IDE1006")]
		public static extern IntPtr _CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string cmdLine, out int numArgs);


    }
}
