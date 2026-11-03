
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

    public class IeElement
    {
		[DllImport(@"IE\Element.dll", EntryPoint = "DoFindElement", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]        [return: MarshalAs(UnmanagedType.Bool)]
		public extern static bool DoFindElement(string json, bool flag);


    }
}
