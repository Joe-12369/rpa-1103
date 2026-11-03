using II.RPA.Logging;
using Microsoft.Extensions.Logging;

using System.Drawing;

namespace II.RPA.Screenshot.Plugin.Core {
	/// <summary>
	/// This is the method signature which is used to capture a rectangle from the screen.
	/// </summary>
	/// <param name="captureBounds"></param>
	/// <returns>Captured Bitmap</returns>
	public delegate Bitmap CaptureScreenRectangleHandler(Rectangle captureBounds);

	/// <summary>
	/// This is a hack to experiment with different screen capture routines
	/// </summary>
	public static class CaptureHandler {
		/// <summary>
		/// By changing this value, null is default
		/// </summary>
		public static CaptureScreenRectangleHandler CaptureScreenRectangle {
			get;
			set;
		}
	}
}
