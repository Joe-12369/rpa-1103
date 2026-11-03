using II.RPA.Logging;
using Microsoft.Extensions.Logging;

using System;

namespace II.RPA.Screenshot.Plugin {
	/// <summary>
	/// Description of IProcessor.
	/// </summary>
	public interface IProcessor : IDisposable, IComparable {
		/// <summary>
		/// Simple "designation" like "FixTitle"
		/// </summary>
		string Designation {
			get;
		}

		/// <summary>
		/// Description which will be shown in the settings form, destination picker etc
		/// </summary>
		string Description {
			get;
		}

		/// <summary>
		/// Priority, used for sorting
		/// </summary>
		int Priority {
			get;
		}

		/// <summary>
		/// Returns if the destination is active
		/// </summary>
		bool isActive {
			get;
		}

		/// <summary>
		/// If a capture is made, and the destination is enabled, this method is called.
		/// </summary>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		/// <returns>true if the processor has "processed" the capture</returns>
		bool ProcessCapture(ISurface surface, ICaptureDetails captureDetails);
	}
}
