using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.Win32;

namespace II.RPA.Screenshot.Plugin.Core {
	/// <summary>
	/// Description of IEHelper.
	/// </summary>
	public static class IEHelper {
		// Internet explorer Registry key
		private const string IE_KEY = @"Software\Microsoft\Internet Explorer";
		/// <summary>
		/// Helper method to get the IE version
		/// </summary>
		/// <returns></returns>
		public static int IEVersion() {
			int version = 7;
			// Seeing if IE 9 is used, here we need another offset!
			using (RegistryKey ieKey = Registry.LocalMachine.OpenSubKey(IE_KEY, false)) {
				object versionKey = ieKey?.GetValue("Version");
				if (versionKey != null) {
					int.TryParse(versionKey.ToString().Substring(0,1), out version);
				}
			}
			return version;
		}
		
		/// <summary>
		/// Find the DirectUI window for MSAA (Accessible)
		/// </summary>
		/// <param name="browserWindowDetails">The browser WindowDetails</param>
		/// <returns>WindowDetails for the DirectUI window</returns>
		public static WindowDetails GetDirectUI(WindowDetails browserWindowDetails) {
			if (browserWindowDetails == null) {
				return null;
			}
			WindowDetails tmpWd = browserWindowDetails;
			// Since IE 9 the TabBandClass is less deep!
			if (IEVersion() < 9) {
				tmpWd = tmpWd.GetChild("CommandBarClass");
				tmpWd = tmpWd?.GetChild("ReBarWindow32");
			}
			tmpWd = tmpWd?.GetChild("TabBandClass");
			tmpWd = tmpWd?.GetChild("DirectUIHWND");
			return tmpWd;
		}
		
		/// <summary>
		/// Return an IEnumerable with the currently opened IE urls
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<string> GetIEUrls() {
			// Find the IE window
			foreach (WindowDetails ieWindow in WindowDetails.GetAllWindows("IEFrame")) {
				WindowDetails directUIWD = GetDirectUI(ieWindow);
				if (directUIWD != null) {
					Accessible ieAccessible = new Accessible(directUIWD.Handle);
					foreach(string url in ieAccessible.IETabUrls)
					{
						yield return url;
					}
				}
			}
		}
	}
}
