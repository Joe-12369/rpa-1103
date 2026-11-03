using II.RPA.Logging;
using Microsoft.Extensions.Logging;

using System.Drawing;
using System.Windows.Forms;

namespace II.RPA.Screenshot.Plugin {
	/// <summary>
	/// The IImageEditor is the Interface that the Screenshot ImageEditor has to implement
	/// </summary>
	public interface IImageEditor {
		/// <summary>
		/// Return the IWin32Window, this way Plugins have access to the HWND handles wich can be used with Win32 API calls.
		/// </summary>
		IWin32Window WindowHandle {
			get;
		}

		/// <summary>
		/// Get the current Image from the Editor for Exporting (save/upload etc)
		/// This is actually a wrapper which calls Surface.GetImageForExport().
		/// Don't forget to call image.Dispose() when finished!!!
		/// </summary>
		/// <returns>Bitmap</returns>
		Image GetImageForExport();
		
		/// <summary>
		/// Get the ToolStripMenuItem where plugins can place their Menu entrys
		/// </summary>
		/// <returns>ToolStripMenuItem</returns>
		ToolStripMenuItem GetPluginMenuItem();

		/// <summary>
		/// Get the File ToolStripMenuItem
		/// </summary>
		/// <returns>ToolStripMenuItem</returns>
		ToolStripMenuItem GetFileMenuItem();
		
		/// <summary>
		/// Make the ICaptureDetails from the current Surface in the EditorForm available.
		/// </summary>
		ICaptureDetails CaptureDetails {
			get;
		}

		ISurface Surface {
			get;
			set;
		}
	}
}
