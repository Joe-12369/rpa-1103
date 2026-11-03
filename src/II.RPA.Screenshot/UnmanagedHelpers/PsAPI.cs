using II.RPA.Logging;
using Microsoft.Extensions.Logging;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace II.RPA.Screenshot.Plugin.UnmanagedHelpers {
	/// <summary>
	/// Description of PsAPI.
	/// </summary>
	public class PsAPI {
		private static ILogger logger = Logger.OfContext();
		[DllImport("psapi", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, StringBuilder lpFilename, uint nSize);
		[DllImport("psapi", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern uint GetProcessImageFileName(IntPtr hProcess, StringBuilder lpImageFileName, uint nSize);
		[DllImport("psapi")]
		private static extern int EmptyWorkingSet(IntPtr hwProc);

		/// <summary>
		/// Make the process use less memory by emptying the working set
		/// </summary>
		public static void EmptyWorkingSet() {
			logger.LogInformation("Calling EmptyWorkingSet");
			using (Process currentProcess = Process.GetCurrentProcess()) {
				EmptyWorkingSet(currentProcess.Handle);
			}
		}
	}
}
