using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Test.II.RPA
{
    public class WindowElementScreenshot
    {
        #region Windows API 声明
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hGDIObj);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hDestDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        private const int SRCCOPY = 0x00CC0020;
        #endregion

        /// <summary>
        /// 捕获指定窗口的截图
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="includeFrame">是否包含窗口框架（标题栏、边框等）</param>
        /// <returns>窗口截图</returns>
        public static Bitmap CaptureWindow(IntPtr hWnd, bool includeFrame = false)
        {
            RECT windowRect;
            RECT clientRect;

            if (!GetWindowRect(hWnd, out windowRect))
            {
                throw new InvalidOperationException("无法获取窗口矩形");
            }

            if (!GetClientRect(hWnd, out clientRect))
            {
                throw new InvalidOperationException("无法获取客户区矩形");
            }

            int width, height;
            IntPtr hDC;

            if (includeFrame)
            {
                // 包含窗口框架
                width = windowRect.Right - windowRect.Left;
                height = windowRect.Bottom - windowRect.Top;
                hDC = GetWindowDC(hWnd);
            }
            else
            {
                // 仅客户区
                width = clientRect.Right - clientRect.Left;
                height = clientRect.Bottom - clientRect.Top;
                hDC = GetWindowDC(hWnd);
            }

            if (hDC == IntPtr.Zero)
            {
                throw new InvalidOperationException("无法获取窗口DC");
            }

            IntPtr hMemDC = CreateCompatibleDC(hDC);
            IntPtr hBitmap = CreateCompatibleBitmap(hDC, width, height);
            IntPtr hOld = SelectObject(hMemDC, hBitmap);

            try
            {
                bool success;
                if (includeFrame)
                {
                    success = BitBlt(hMemDC, 0, 0, width, height, hDC, 0, 0, SRCCOPY);
                }
                else
                {
                    // 客户区需要偏移
                    POINT clientOrigin = new POINT { X = 0, Y = 0 };
                    ClientToScreen(hWnd, ref clientOrigin);

                    int offsetX = clientOrigin.X - windowRect.Left;
                    int offsetY = clientOrigin.Y - windowRect.Top;

                    success = BitBlt(hMemDC, 0, 0, width, height, hDC, offsetX, offsetY, SRCCOPY);
                }

                if (!success)
                {
                    throw new InvalidOperationException("截图失败");
                }

                return Image.FromHbitmap(hBitmap);
            }
            finally
            {
                SelectObject(hMemDC, hOld);
                DeleteObject(hBitmap);
                DeleteDC(hMemDC);
                ReleaseDC(hWnd, hDC);
            }
        }

        /// <summary>
        /// 将屏幕坐标转换为窗口客户区坐标
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="screenPoint">屏幕坐标</param>
        /// <returns>窗口客户区坐标</returns>
        public static Point ScreenToClient(IntPtr hWnd, Point screenPoint)
        {
            POINT point = new POINT { X = screenPoint.X, Y = screenPoint.Y };
            ScreenToClient(hWnd, ref point);
            return new Point(point.X, point.Y);
        }

        /// <summary>
        /// 从窗口截图中提取指定元素区域的图片
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="elementScreenRect">元素在屏幕上的矩形区域</param>
        /// <param name="includeFrame">窗口截图是否包含框架</param>
        /// <returns>元素截图</returns>
        public static Bitmap CaptureElement(IntPtr hWnd, Rectangle elementScreenRect, bool includeFrame = false)
        {
            // 1. 获取窗口截图
            using (Bitmap windowBitmap = CaptureWindow(hWnd, includeFrame))
            {
                // 2. 计算元素相对于窗口的坐标
                Rectangle elementWindowRect;

                if (includeFrame)
                {
                    // 如果包含框架，需要转换为相对于窗口左上角的坐标
                    RECT windowRect;
                    GetWindowRect(hWnd, out windowRect);

                    elementWindowRect = new Rectangle(
                        elementScreenRect.X - windowRect.Left,
                        elementScreenRect.Y - windowRect.Top,
                        elementScreenRect.Width,
                        elementScreenRect.Height
                    );
                }
                else
                {
                    // 如果不包含框架，转换为客户区坐标
                    Point clientTopLeft = ScreenToClient(hWnd, elementScreenRect.Location);
                    elementWindowRect = new Rectangle(
                        clientTopLeft.X,
                        clientTopLeft.Y,
                        elementScreenRect.Width,
                        elementScreenRect.Height
                    );
                }

                // 3. 检查边界
                if (elementWindowRect.X < 0 || elementWindowRect.Y < 0 ||
                    elementWindowRect.Right > windowBitmap.Width ||
                    elementWindowRect.Bottom > windowBitmap.Height)
                {
                    throw new ArgumentException("元素区域超出窗口范围");
                }

                // 4. 从窗口截图中裁剪元素区域
                Bitmap elementBitmap = new Bitmap(elementWindowRect.Width, elementWindowRect.Height);
                using (Graphics g = Graphics.FromImage(elementBitmap))
                {
                    g.DrawImage(windowBitmap,
                        new Rectangle(0, 0, elementWindowRect.Width, elementWindowRect.Height),
                        elementWindowRect,
                        GraphicsUnit.Pixel);
                }

                return elementBitmap;
            }
        }

        /// <summary>
        /// 直接通过坐标和尺寸捕获元素
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="screenX">元素在屏幕上的X坐标</param>
        /// <param name="screenY">元素在屏幕上的Y坐标</param>
        /// <param name="width">元素宽度</param>
        /// <param name="height">元素高度</param>
        /// <param name="includeFrame">窗口截图是否包含框架</param>
        /// <returns>元素截图</returns>
        public static Bitmap CaptureElement(IntPtr hWnd, int screenX, int screenY, int width, int height, bool includeFrame = false)
        {
            Rectangle elementRect = new Rectangle(screenX, screenY, width, height);
            return CaptureElement(hWnd, elementRect, includeFrame);
        }
    }
}
