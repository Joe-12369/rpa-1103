using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace II.RPA.Screenshot.Plugin.Effects
{
	/// <summary>
	/// Interface to describe an effect
	/// </summary>
	public interface IEffect {
		/// <summary>
		/// Apply this IEffect to the supplied sourceImage.
		/// In the process of applying the supplied matrix will be modified to represent the changes.
		/// </summary>
		/// <param name="sourceImage">Image to apply the effect to</param>
		/// <param name="matrix">Matrix with the modifications like rotate, translate etc. this can be used to calculate the new location of elements on a canvas</param>
		/// <returns>new image with applied effect</returns>
		Image Apply(Image sourceImage, Matrix matrix);

		/// <summary>
		/// Reset all values to their defaults
		/// </summary>
		void Reset();
	}
}
