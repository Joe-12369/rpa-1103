
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
    using Accessibility;
    #endregion

    public class Oleacc
    {
		[DllImport("oleacc.dll", CharSet = CharSet.Auto, PreserveSig = false)]        [return: MarshalAs(UnmanagedType.Interface)]
		public static extern object ObjectFromLresult(UIntPtr lResult, [MarshalAs(UnmanagedType.LPStruct)] Guid refiid, IntPtr wParam);

		[DllImport("oleacc.dll")]
		public static extern int AccessibleObjectFromWindow(IntPtr hwnd, uint id, ref Guid iid,
                                                             [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object
                                                                 ppvObject);

		[DllImport("oleacc.dll")]
		public static extern int AccessibleChildren(IAccessible paccContainer, int iChildStart, int cChildren,
                                                     [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] object[] rgvarChildren, out int pcObtained);


    }
}
