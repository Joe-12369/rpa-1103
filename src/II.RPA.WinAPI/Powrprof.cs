
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

    public class Powrprof
    {
		[DllImport("PowrProf.dll", CharSet = CharSet.Auto)]
		public static extern bool SetSuspendState(bool hiberate, bool forceCritical, bool disableWakeEvent);


    }
}
