
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

    public class Setupapi
    {
		[DllImport("SetupApi.dll", CharSet = CharSet.Auto)]
		public         static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, int Flags);

		[DllImport("Setupapi.dll", CharSet = CharSet.Auto)]
		public static extern Boolean SetupDiEnumDeviceInterfaces(IntPtr hDevInfo, IntPtr devInfo, ref Guid interfaceClassGuid, uint memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

		[DllImport(@"setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern Boolean SetupDiGetDeviceInterfaceDetail(
            IntPtr hDevInfo,
            SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
            IntPtr deviceInterfaceDetailData,
            uint deviceInterfaceDetailDataSize,
            out uint requiredSize,
            SP_DEVINFO_DATA deviceInfoData);


    }
}
