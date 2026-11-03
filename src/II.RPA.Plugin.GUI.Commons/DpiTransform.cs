using FlaUI.Core.WindowsAPI;
using II.RPA.Plugin.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons
{
    public static class DpiTransform
    {
        private static double _scalingFactor;
        public static double GetScalingFactor()
        {
            if (_scalingFactor == 0.0)
            {
                using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
                {

                    IntPtr hdc = graphics.GetHdc();
                    int deviceCaps = II.RPA.WinAPI.Gdi32.GetDeviceCaps(hdc, (int)GET_DEVICE_CAPS_INDEX.VERTRES);
                    int deviceCaps2 = II.RPA.WinAPI.Gdi32.GetDeviceCaps(hdc, (int)GET_DEVICE_CAPS_INDEX.DESKTOPVERTRES);
                    int deviceCaps3 = II.RPA.WinAPI.Gdi32.GetDeviceCaps(hdc, (int)GET_DEVICE_CAPS_INDEX.LOGPIXELSX);

                    graphics.ReleaseHdc(hdc);
                    _scalingFactor = Math.Round((double)deviceCaps3 / 96.0, 2);
                    if(_scalingFactor==1.0)
                    {
                        _scalingFactor = Math.Round((double)deviceCaps2 / (double)deviceCaps, 2);
                    }
                }  
            }
            return _scalingFactor;
        }

        public static Rectangle TransformBackRectangle(Rectangle rec)
        {
            double scalingFactor = GetScalingFactor();
            return new Rectangle((int)((double)rec.Left*scalingFactor),(int)((double)rec.Top*scalingFactor), (int)((double)rec.Width*scalingFactor), (int)((double)rec.Height*scalingFactor));
        }
    }
   
}
