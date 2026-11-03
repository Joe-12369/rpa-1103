using II.RPA.Logging;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Drawing;

namespace II.RPA.Screenshot.Plugin
{
    /// <summary>
    /// The capture mode for Screenshot
    /// </summary>
    public enum CaptureMode { None, Region, FullScreen, ActiveWindow, Window, LastRegion, Clipboard, File, IE, Import }; //, Video };
    public enum ScreenCaptureMode { Auto, FullScreen, Fixed };

    /// <summary>
    /// Details for the capture, like the window title and date/time etc.
    /// </summary>
    public interface ICaptureDetails
    {
        string Filename
        {
            get;
            set;
        }
        string Title
        {
            get;
            set;
        }

        DateTime DateTime
        {
            get;
            set;
        }

        List<IDestination> CaptureDestinations
        {
            get;
            set;
        }

        Dictionary<string, string> MetaData
        {
            get;
        }

        /// <summary>
        /// Helper method to prevent complex code which needs to check every key
        /// </summary>
        /// <param name="key">The key for the meta-data</param>
        /// <param name="value">The value for the meta-data</param>
        void AddMetaData(string key, string value);

        void ClearDestinations();
        void RemoveDestination(IDestination captureDestination);
        void AddDestination(IDestination captureDestination);
        bool HasDestination(string designation);

        CaptureMode CaptureMode
        {
            get;
            set;
        }

        float DpiX
        {
            get;
            set;
        }
        float DpiY
        {
            get;
            set;
        }
    }

    public interface ICaptureElement
    {
        List<ICaptureElement> Children
        {
            get;
            set;
        }
        Rectangle Bounds
        {
            get;
            set;
        }
        string Name
        {
            get;
            set;
        }
    }

    /// <summary>
    /// The interface to the Capture object, so Plugins can use it.
    /// </summary>
    public interface ICapture : IDisposable
    {
        // The Capture Details
        ICaptureDetails CaptureDetails
        {
            get;
            set;
        }

        // The captured Image
        Image Image
        {
            get;
            set;
        }

        void NullImage();

        Rectangle ScreenBounds
        {
            get;
            set;
        }

        Icon Cursor
        {
            get;
            set;
        }

        // Boolean to specify if the cursor is available
        bool CursorVisible
        {
            get;
            set;
        }

        Point CursorLocation
        {
            get;
            set;
        }

        Point Location
        {
            get;
            set;
        }

        bool Scroll { get; set; }

        /// <summary>
        /// Crops the capture to the specified rectangle (with Bitmap coordinates!)
        /// </summary>
        /// <param name="cropRectangle">Rectangle with bitmap coordinates</param>
        bool Crop(Rectangle cropRectangle);

        /// <summary>
        /// Apply a translate to the mouse location. e.g. needed for crop
        /// </summary>
        /// <param name="x">x coordinates to move the mouse</param>
        /// <param name="y">y coordinates to move the mouse</param>
        void MoveMouseLocation(int x, int y);

        // / TODO: Enable when the elements are usable again.
        ///// <summary>
        ///// Apply a translate to the elements e.g. needed for crop
        ///// </summary>
        ///// <param name="x">x coordinates to move the elements</param>
        ///// <param name="y">y coordinates to move the elements</param>
        //void MoveElements(int x, int y);

        ///// <summary>
        ///// Add a new element to the capture
        ///// </summary>
        ///// <param name="element">Rectangle</param>
        //void AddElement(ICaptureElement element);

        ///// <summary>
        ///// Returns a list of rectangles which represent objects that are "on" the capture
        ///// </summary>
        //List<ICaptureElement> Elements {
        //    get;
        //    set;
        //}
    }

}
