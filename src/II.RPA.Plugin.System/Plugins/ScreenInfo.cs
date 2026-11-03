using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace II.RPA.Plugin.RPASystem.Plugins
{
    public class ScreenInfo
    {
        public int Width { get; set; }

        public int Height { get; set; }

        public int ScalingFactor { get; set; }

        [DllImport("gdi32.dll", EntryPoint = "GetDeviceCaps", SetLastError = true)]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        public static ScreenInfo Get()
        {
            var g = Graphics.FromHwnd(IntPtr.Zero);
            IntPtr iPtr = g.GetHdc();
            var width = GetDeviceCaps(iPtr, 118);
            var height = GetDeviceCaps(iPtr, 117);

            var scalingFactor = (1d * width) /
                SystemParameters.PrimaryScreenWidth;

            return new ScreenInfo
            {
                ScalingFactor = (int)(Math.Round(scalingFactor, 2) * 100),
                Height = height,
                Width = width
            };
        }

        //public override bool Equals(object obj)
        //{
        //    if (obj is ScreenInfo oScreenInfo)
        //    {
        //        return oScreenInfo.Height == Height &&
        //            oScreenInfo.Width == Width &&
        //            oScreenInfo.ScalingFactor == ScalingFactor;
        //    }

        //    return false;
        //}

        public override string ToString()
        {
            return $"{Width} x {Height} ({ScalingFactor}%)";
        }
    }
}
