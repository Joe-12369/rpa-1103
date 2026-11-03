using II.RPA.Logging;
using Microsoft.Extensions.Logging;

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace II.RPA.Screenshot.Plugin.Drawing.Adorners
{
	public interface IAdorner
	{
		/// <summary>
		/// Returns if this adorner is active
		/// </summary>
		bool IsActive { get; }

		/// <summary>
		/// The current edit status, this is needed to locate the adorner to send events to
		/// </summary>
		EditStatus EditStatus { get; }

		/// <summary>
		/// The owner of this adorner
		/// </summary>
		IDrawableContainer Owner { get; }

		/// <summary>
		/// Is the current point "over" the Adorner?
		/// If this is the case, the 
		/// </summary>
		/// <param name="point">Point to test</param>
		/// <returns>true if so</returns>
		bool HitTest(Point point);

		/// <summary>
		/// Handle the MouseDown event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="mouseEventArgs">MouseEventArgs</param>
		void MouseDown(object sender, MouseEventArgs mouseEventArgs);

		/// <summary>
		/// Handle the MouseUp event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="mouseEventArgs">MouseEventArgs</param>
		void MouseUp(object sender, MouseEventArgs mouseEventArgs);

		/// <summary>
		/// Handle the MouseMove event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="mouseEventArgs">MouseEventArgs</param>
		void MouseMove(object sender, MouseEventArgs mouseEventArgs);

		/// <summary>
		/// Gets the cursor that should be displayed for this behavior.
		/// </summary>
		Cursor Cursor { get; }

		/// <summary>
		/// Draw the adorner
		/// </summary>
		/// <param name="paintEventArgs">PaintEventArgs</param>
		void Paint(PaintEventArgs paintEventArgs);

		/// <summary>
		/// Called if the owner is transformed
		/// </summary>
		/// <param name="matrix">Matrix</param>
		void Transform(Matrix matrix);
	}
}
