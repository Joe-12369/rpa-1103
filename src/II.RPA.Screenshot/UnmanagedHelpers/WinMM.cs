using II.RPA.Logging;
using Microsoft.Extensions.Logging;

using System;
using System.Runtime.InteropServices;

namespace II.RPA.Screenshot.Plugin.UnmanagedHelpers {
	/// <summary>
	/// Windows Media 
	/// </summary>
	public class WinMM {
	    [DllImport("winmm.dll", SetLastError = true)]
	    public static extern bool PlaySound(byte[] ptrToSound, UIntPtr hmod, uint fdwSound);
	
	    [DllImport("winmm.dll", SetLastError = true)]
	    public static extern bool PlaySound(IntPtr ptrToSound, UIntPtr hmod, uint fdwSound);
	}
}
