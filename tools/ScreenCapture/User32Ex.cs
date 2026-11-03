using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ScreenCapture
{
    public class User32Ex
    {
        public const string DLL = "user32.dll";
        public const int HORZRES = 8;
        public const int VERTRES = 10;
        public const int DESKTOPVERTRES = 117;
        public const int DESKTOPHORZRES = 118;
        [DllImport(DLL)]
        public static extern IntPtr GetDC(IntPtr ptr);
        [DllImport(DLL, EntryPoint = "ReleaseDC")]
        public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDc);
        [DllImport("gdi32.dll")]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        public static Size WorkingArea
        {
            get
            {
                IntPtr hdc = GetDC(IntPtr.Zero);
                Size size = new Size();
                size.Width = GetDeviceCaps(hdc, HORZRES);
                size.Height = GetDeviceCaps(hdc, VERTRES);
                ReleaseDC(IntPtr.Zero, hdc);
                return size;
            }
        }
        public static Size DESKTOP
        {
            get
            {
                IntPtr hdc = GetDC(IntPtr.Zero);
                Size size = new Size();
                size.Width = GetDeviceCaps(hdc, DESKTOPHORZRES);
                size.Height = GetDeviceCaps(hdc, DESKTOPVERTRES);
                ReleaseDC(IntPtr.Zero, hdc);
                return size;
            }
        }
        [DllImport(DLL)]
        public static extern bool SetProcessDPIAware();
    }
}
