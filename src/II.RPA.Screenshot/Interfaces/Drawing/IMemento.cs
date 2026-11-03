using II.RPA.Logging;
using Microsoft.Extensions.Logging;

using System;

namespace II.RPA.Screenshot.Memento {
	/// <summary>
	/// Description of IMemento.
	/// </summary>
	public interface IMemento : IDisposable {
	    /// <summary>
	    /// Restores target to the state memorized by this memento.
		/// </summary>
		/// <returns>
		/// A memento of the state before restoring
		/// </returns>
		IMemento Restore();
		
		/// <summary>
		/// Try to merge the current memento with another, preventing loads of items on the stack
		/// </summary>
		/// <param name="other">The memento to try to merge with</param>
		/// <returns></returns>
		bool Merge(IMemento other);
	}
}
