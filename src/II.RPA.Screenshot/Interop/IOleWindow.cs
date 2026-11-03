using System;
using System.Runtime.InteropServices;

namespace II.RPA.Screenshot.Interop
{
    // Needed to get the Window handle from the IDocument2
    // See: http://msdn.microsoft.com/en-us/library/ms680102%28v=vs.85%29.aspx
    [ComImport, Guid("00000114-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IOleWindow {
		void GetWindow(out IntPtr phwnd);
		void ContextSensitiveHelp([In, MarshalAs(UnmanagedType.Bool)] bool fEnterMode);
	}
}
