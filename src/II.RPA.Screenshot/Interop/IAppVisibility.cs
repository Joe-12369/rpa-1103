using II.RPA.Logging;
using Microsoft.Extensions.Logging;

using System;
using System.Runtime.InteropServices;

namespace II.RPA.Screenshot.Interop {
	// This is used for Windows 8 to see if the App Launcher is active
	// See http://msdn.microsoft.com/en-us/library/windows/desktop/jj554119%28v=vs.85%29.aspx
	[ComProgId("clsid:7E5FE3D9-985F-4908-91F9-EE19F9FD1514")]
	[ComImport, Guid("2246EA2D-CAEA-4444-A3C4-6DE827E44313"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IAppVisibility {
		MONITOR_APP_VISIBILITY GetAppVisibilityOnMonitor(IntPtr hMonitor);
		bool IsLauncherVisible {
			get;
		}
	}

	public enum MONITOR_APP_VISIBILITY {
		MAV_UNKNOWN = 0,		// The mode for the monitor is unknown
		MAV_NO_APP_VISIBLE = 1,
		MAV_APP_VISIBLE = 2
	}
}
