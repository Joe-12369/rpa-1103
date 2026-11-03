
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

    public class Crypt32
    {
		[DllImport("Crypt32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]            [return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CryptProtectData(ref DATA_BLOB pDataIn, String szDataDescr, ref DATA_BLOB pOptionalEntropy,
                IntPtr pvReserved, ref CRYPTPROTECT_PROMPTSTRUCT pPromptStruct, CryptProtectFlags dwFlags, ref DATA_BLOB pDataOut);

		[DllImport("Crypt32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]            [return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CryptUnprotectData(ref DATA_BLOB pDataIn, StringBuilder szDataDescr, ref DATA_BLOB pOptionalEntropy, IntPtr pvReserved,
                ref CRYPTPROTECT_PROMPTSTRUCT pPromptStruct, CryptProtectFlags dwFlags, ref DATA_BLOB pDataOut);


    }
}
