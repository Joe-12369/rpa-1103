using II.RPA.Logging;
using Microsoft.Extensions.Logging;

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using II.RPA.Screenshot.Plugin.Interfaces.Drawing;
using II.RPA.Screenshot.Plugin.Drawing.Adorners;

namespace II.RPA.Screenshot.Plugin.Drawing
{
	public enum RenderMode { EDIT, EXPORT };
	public enum EditStatus { UNDRAWN, DRAWING, MOVING, RESIZING, IDLE };

	public interface IDrawableContainer : INotifyPropertyChanged, IDisposable
	{
		ISurface Parent
		{
			get;
			set;
		}
		bool Selected
		{
			get;
			set;
		}

		int Left
		{
			get;
			set;
		}

		int Top
		{
			get;
			set;
		}

		int Width
		{
			get;
			set;
		}

		int Height
		{
			get;
			set;
		}

		Point Location
		{
			get;
		}

		Size Size
		{
			get;
		}

		Rectangle Bounds
		{
			get;
		}

		Rectangle DrawingBounds
		{
			get;
		}

		void ApplyBounds(RectangleF newBounds);

		bool hasFilters
		{
			get;
		}

		EditStatus Status
		{
			get;
			set;
		}
		void AlignToParent(HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment);
		void Invalidate();
		bool ClickableAt(int x, int y);
		void MoveBy(int x, int y);
		void Transform(Matrix matrix);
		bool HandleMouseDown(int x, int y);
		void HandleMouseUp(int x, int y);
		bool HandleMouseMove(int x, int y);
		bool InitContent();
		void MakeBoundsChangeUndoable(bool allowMerge);
		EditStatus DefaultEditMode
		{
			get;
		}

		/// <summary>
		/// Available adorners for the DrawableContainer
		/// </summary>
		IList<IAdorner> Adorners { get; }
	}

	public interface IDrawableContainerList : IList<IDrawableContainer>, IDisposable
	{
		Guid ParentID
		{
			get;
		}

		bool Selected
		{
			get;
			set;
		}

		ISurface Parent
		{
			get;
			set;
		}
		EditStatus Status
		{
			get;
			set;
		}
		void MakeBoundsChangeUndoable(bool allowMerge);
		void Transform(Matrix matrix);
		void MoveBy(int dx, int dy);
		bool ClickableAt(int x, int y);
		IDrawableContainer ClickableElementAt(int x, int y);
		void OnDoubleClick();
		bool HasIntersectingFilters(Rectangle clipRectangle);
		bool IntersectsWith(Rectangle clipRectangle);
		void Draw(Graphics g, Bitmap bitmap, RenderMode renderMode, Rectangle clipRectangle);
		void Invalidate();
		void PullElementsToTop(IDrawableContainerList elements);
		bool CanPushDown(IDrawableContainerList elements);
		void PullElementsUp(IDrawableContainerList elements);
		bool CanPullUp(IDrawableContainerList elements);
		void PushElementsDown(IDrawableContainerList elements);
		void PushElementsToBottom(IDrawableContainerList elements);
		void ShowContextMenu(MouseEventArgs e, ISurface surface);
		void HandleFieldChangedEvent(object sender, FieldChangedEventArgs e);
	}

	public interface ITextContainer : IDrawableContainer
	{
		string Text
		{
			get;
			set;
		}
		void FitToText();
	}

	public interface IImageContainer : IDrawableContainer
	{
		Image Image
		{
			get;
			set;
		}
		void Load(string filename);
	}
	public interface ICursorContainer : IDrawableContainer
	{
		Cursor Cursor
		{
			get;
			set;
		}
		void Load(string filename);
	}
	public interface IIconContainer : IDrawableContainer
	{
		Icon Icon
		{
			get;
			set;
		}
		void Load(string filename);
	}

	public interface IMetafileContainer : IDrawableContainer
	{
		Metafile Metafile
		{
			get;
			set;
		}
		void Load(string filename);
	}
}
