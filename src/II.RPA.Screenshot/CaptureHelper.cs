using II.RPA.Logging;
using II.RPA.Screenshot.IniFile;
using II.RPA.Screenshot.Plugin;
using II.RPA.Screenshot.Plugin.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;

namespace II.RPA.Screenshot
{

    /// <summary>
    /// CaptureHelper contains all the capture logic 
    /// </summary>
    public class CaptureHelper : IDisposable
    {
        private static readonly ILogger logger = Logger.OfContext();

        private static readonly CoreConfiguration CoreConfig = IniConfig.GetIniSection<CoreConfiguration>();


        private WindowDetails _selectedCaptureWindow;
        private readonly bool _captureMouseCursor;
        private ICapture _capture;
        private CaptureMode _captureMode;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Cleanup
            }
            _selectedCaptureWindow = null;
            _capture = null;
        }


        public static Image CaptureIe(int maxHeight, bool captureMouse, int tabId)
        {
            using (CaptureHelper captureHelper = new CaptureHelper(CaptureMode.IE, captureMouse))
            {
                captureHelper.SelectedCaptureWindow = null;
                return captureHelper.MakeCaptureByLimitHeight(maxHeight, tabId);
            }

        }

        public CaptureHelper(CaptureMode captureMode)
        {
            _captureMode = captureMode;
            _capture = new Capture();
        }

        public CaptureHelper(CaptureMode captureMode, bool captureMouseCursor) : this(captureMode)
        {
            _captureMouseCursor = captureMouseCursor;
        }

        public WindowDetails SelectedCaptureWindow
        {
            get
            {
                return _selectedCaptureWindow;
            }
            set
            {
                _selectedCaptureWindow = value;
            }
        }

        /// <summary>
        /// Make Capture with specified destinations
        /// </summary>
        private Image MakeCaptureByLimitHeight(int height, int tabId)
        {
            logger.LogDebug($"Capturing with mode {_captureMode} and using Cursor {_captureMouseCursor}");
            _capture.CaptureDetails.CaptureMode = _captureMode;

            _capture = WindowCapture.CaptureCursor(_capture);
            _capture.CursorVisible = _captureMouseCursor && CoreConfig.CaptureMousepointer;
            WindowDetails activeWindow = new WindowDetails((IntPtr)tabId);
            if (IeCapture.CaptureIe(_capture, activeWindow) != null)
            {
                _capture.CaptureDetails.AddMetaData("source", "Internet Explorer");
                SetDpi();
                return _capture.Image;
            }
            else
            {
                logger.LogWarning("Unknown capture mode: " + _captureMode);
                return null;
            }
        }

        private void SetDpi()
        {
            //if (_capture.CaptureDetails != null)
            //{
            //    ((Bitmap)_capture.Image)?.SetResolution(_capture.CaptureDetails.DpiX, _capture.CaptureDetails.DpiY);
            //}


        }

    }
}
