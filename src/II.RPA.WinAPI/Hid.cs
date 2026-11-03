
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

    public class Hid
    {
		[DllImport("HID.dll", CharSet = CharSet.Auto)]
		public         static extern void HidD_GetHidGuid(out Guid ClassGuid);

		[DllImport("HID.dll", CharSet = CharSet.Auto)]
		public         static extern bool HidD_GetAttributes(SafeFileHandle HidDeviceObject, ref HIDD_ATTRIBUTES Attributes);

		[DllImport("HID.dll", CharSet = CharSet.Auto)]
		public static extern bool HidD_GetNumInputBuffers(SafeFileHandle HidDeviceObject, ref uint NumberBuffers);

		[DllImport("hid.dll", CharSet = CharSet.Auto)]
		public static extern bool HidD_SetNumInputBuffers(SafeFileHandle HidDeviceObject, uint BufferLength);

		[DllImport("hid.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern bool HidD_SetFeature(SafeFileHandle HidDeviceObject, byte[] Buffer, uint BufferLength);


    }
}
