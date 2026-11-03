using II.RPA.Logging;
using Microsoft.Extensions.Logging;

using System;
using System.Runtime.InteropServices;

namespace II.RPA.Screenshot.Interop {
	// This is the COM IServiceProvider interface, not System.IServiceProvider .Net interface!
	[ComImport(), ComVisible(true), Guid("6D5140C1-7436-11CE-8034-00AA006009FA"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IServiceProvider {
		[return: MarshalAs(UnmanagedType.I4)][PreserveSig]
		int QueryService(ref Guid guidService, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppvObject);
	}
}
