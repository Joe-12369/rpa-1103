using System;
using System.Drawing;

namespace II.RPA.Plugin.Core.Commons
{
    public class DpiConverter
    {
        public static Rectangle ConvertRectangle(Rectangle rect, bool isReduce = true)
        {
            double scale = DpiConverter.GetScalingFactor();
            if (isReduce)
            {
                //缩小
                return new Rectangle((int)((double)rect.Left / scale), (int)((double)rect.Top / scale), (int)((double)rect.Width / scale), (int)((double)rect.Height / scale));
            }
            else
            {
                //放大
                return new Rectangle((int)((double)rect.Left * scale), (int)((double)rect.Top * scale), (int)((double)rect.Width * scale), (int)((double)rect.Height * scale));
            }
        }

      public static double GetScalingFactor()
        {
            var scalingFactor = 1.0;
            using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                IntPtr desktop = graphics.GetHdc();
                int logicalScreenHeight = NativeMethods.GetDeviceCaps(desktop, GET_DEVICE_CAPS_INDEX.VERTRES);
                int physicalScreenHeight = NativeMethods.GetDeviceCaps(desktop, GET_DEVICE_CAPS_INDEX.DESKTOPVERTRES);
                double deviceCaps = (double)NativeMethods.GetDeviceCaps(desktop, GET_DEVICE_CAPS_INDEX.LOGPIXELSX);
                graphics.ReleaseHdc(desktop);
                scalingFactor = Math.Round(deviceCaps / 96.0, 2);
                if (scalingFactor == 1.0)
                {
                    scalingFactor = Math.Round((double)physicalScreenHeight / (double)logicalScreenHeight, 2);
                }

            }
            return scalingFactor;
        }
    }
}
