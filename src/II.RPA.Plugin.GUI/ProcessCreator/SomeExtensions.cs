using FlaUI.Core.Logging;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.International.Converters.PinYinConverter;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using Point = System.Windows.Point;

namespace II.RPA.Plugin.GUI.ProcessCreator
{
    public static class SomeExtensions
    {
        private static readonly Microsoft.Extensions.Logging.ILogger logger = Logging.Logger.OfContext();

        #region W32 API

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(ref W32Point pt);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref W32MonitorInfo lpmi);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(W32Point pt, uint dwFlags);

        [StructLayout(LayoutKind.Sequential)]
        public struct W32Point
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct W32MonitorInfo
        {
            public int Size;
            public W32Rect Monitor;
            public W32Rect WorkArea;
            public uint Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct W32Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        #endregion

        #region window

        public static bool ActivateCenteredToMouse(this Window window)
        {
            ComputeTopLeft(ref window);
            return window.Activate();
        }
        public static void MoverCenteredToMouse(this Window window)
        {
            ComputeTopLeft(ref window);
        }

        public static void ShowCenteredToMouse(this Window window)
        {
            // in case the default start-up location isn't set to Manual
            WindowStartupLocation oldLocation = window.WindowStartupLocation;
            // set location to manual -> window will be placed by Top and Left property
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            ComputeTopLeft(ref window);
            window.Show();
            window.WindowStartupLocation = oldLocation;
        }
        private static void ComputeTopLeft(ref Window window)
        {
            W32Point pt = new W32Point();
            if (!GetCursorPos(ref pt))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            PresentationSource source = PresentationSource.FromVisual(window);
            double dpiX = source.CompositionTarget.TransformToDevice.M11;
            double dpiY = source.CompositionTarget.TransformToDevice.M22;

            logger.LogInformation($"current dpi:{dpiX},{dpiY}");

            IntPtr monHandle = MonitorFromPoint(pt, 0x00000002);
            W32MonitorInfo monInfo = new W32MonitorInfo
            {
                Size = Marshal.SizeOf(typeof(W32MonitorInfo))
            };

            if (!GetMonitorInfo(monHandle, ref monInfo))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            // use WorkArea struct to include the taskbar position.
            W32Rect monitor = monInfo.Monitor;
            //double offsetX = Math.Round(window.Width / 2);
            //double offsetY = Math.Round(window.Height / 2);
            double offsetX = 2 / dpiX;
            double offsetY = 2 / dpiX;
            double top = pt.Y / dpiX - offsetY;
            double left = pt.X / dpiX - offsetX;

            Rect screen = new Rect(
                new Point(monitor.Left / dpiX, monitor.Top / dpiX),
                new Point(monitor.Right / dpiX, monitor.Bottom / dpiX));
            Rect wnd = new Rect(
                new Point(left, top),
                new Point(left + window.Width, top + window.Height));

            window.Top = wnd.Top - 3;
            window.Left = wnd.Left - 3;

            //if(window.Top > screen.Top - window)
            //if (pt.Y / dpiX < 2)
            //{
            //    window.Top = -1;
            //}
            //if (pt.X / dpiX < 2)
            //{
            //    window.Left = -1;
            //}
            //if (pt.X / dpiX > screen.Width - 2)
            //{
            //    window.Left = screen.Width + 1;
            //}
            //if (pt.Y / dpiX > screen.Height - 2)
            //{
            //    window.Top = screen.Height + 1;
            //}

            if (!screen.Contains(wnd))
            {
                if (wnd.Top < screen.Top)
                {
                    double diff = Math.Abs(screen.Top - wnd.Top);
                    window.Top = wnd.Top + diff;
                }

                if (wnd.Bottom > screen.Bottom)
                {
                    double diff = wnd.Bottom - screen.Bottom;
                    window.Top = wnd.Top - diff;
                }

                if (wnd.Left < screen.Left)
                {
                    double diff = Math.Abs(screen.Left - wnd.Left);
                    window.Left = wnd.Left + diff;
                }

                if (wnd.Right > screen.Right)
                {
                    double diff = wnd.Right - screen.Right;
                    window.Left = wnd.Left - diff;
                }
            }

        }
        public static Rectangle Downscale(this Rectangle rectangle, double factor)
        {
            Rectangle result = new Rectangle(rectangle.Location, rectangle.Size);
            result.X = (int)((double)result.X / factor);
            result.Y = (int)((double)result.Y / factor);
            result.Height = (int)((double)result.Height / factor);
            result.Width = (int)((double)result.Width / factor);
            return result;
        }

        public static Rectangle GetCurrentDesktopSize(Visual aVisual)
        {
            W32Point pt = new W32Point();
            if (!GetCursorPos(ref pt))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            PresentationSource source = aVisual.Dispatcher.Invoke(() => PresentationSource.FromVisual(aVisual)) ;
            double dpiX = aVisual.Dispatcher.Invoke(() => source.CompositionTarget.TransformToDevice.M11);
            //double dpiY = source.CompositionTarget.TransformToDevice.M22;

            logger.LogInformation($"current dpi:{dpiX}");

            IntPtr monHandle = MonitorFromPoint(pt, 0x00000002);
            W32MonitorInfo monInfo = new W32MonitorInfo
            {
                Size = Marshal.SizeOf(typeof(W32MonitorInfo))
            };

            if (!GetMonitorInfo(monHandle, ref monInfo))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            W32Rect monitor = monInfo.WorkArea;
            Rect screen = new Rect(new Point(monitor.Left / dpiX, monitor.Top / dpiX),
                                   new Point(monitor.Right / dpiX, monitor.Bottom / dpiX));
            return new Rectangle 
            {
                Height = (int)screen.Height,
                Width = (int)screen.Width,
            };
        }
        #endregion

        #region pingyin
        private static Dictionary<int, List<string>> GetTotalPingYinDictionary(string text)
        {
            var chs = text.ToCharArray();

            //记录每个汉字的全拼
            Dictionary<int, List<string>> totalPingYinList = new Dictionary<int, List<string>>();

            for (int i = 0; i < chs.Length; i++)
            {
                var pinyinList = new List<string>();

                //是否是有效的汉字
                if (ChineseChar.IsValidChar(chs[i]))
                {
                    ChineseChar cc = new ChineseChar(chs[i]);
                    pinyinList = cc.Pinyins.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
                }
                else
                {
                    pinyinList.Add(chs[i].ToString());
                }

                //去除声调，转小写
                pinyinList = pinyinList.ConvertAll(p => Regex.Replace(p, @"\d", "").ToLower());

                //去重
                pinyinList = pinyinList.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();
                if (pinyinList.Any())
                {
                    totalPingYinList[i] = pinyinList;
                }
            }

            return totalPingYinList;
        }
        /// <summary>
        /// 获取汉语拼音全拼
        /// </summary>
        /// <param name="text">The string.</param>
        /// <returns></returns>
        public static List<string> GetTotalPingYin(this string text)
        {
            var result = new List<string>();
            foreach (var pys in GetTotalPingYinDictionary(text))
            {
                var items = pys.Value;
                if (result.Count <= 0)
                {
                    result = items;
                }
                else
                {
                    //全拼循环匹配
                    var newTotalPingYinList = new List<string>();
                    foreach (var totalPingYin in result)
                    {
                        newTotalPingYinList.AddRange(items.Select(item => totalPingYin + item));
                    }
                    newTotalPingYinList = newTotalPingYinList.Distinct().ToList();
                    result = newTotalPingYinList;
                }
            }
            return result;
        }

        /// <summary>
        /// 获取汉语拼音首字母
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static List<string> GetFirstPingYin(this string text)
        {
            var result = new List<string>();
            foreach (var pys in GetTotalPingYinDictionary(text))
            {
                var items = pys.Value;
                if (result.Count <= 0)
                {
                    result = items.ConvertAll(p => p.Substring(0, 1)).Distinct().ToList();
                }
                else
                {
                    //首字母循环匹配
                    var newFirstPingYinList = new List<string>();
                    foreach (var firstPingYin in result)
                    {
                        newFirstPingYinList.AddRange(items.Select(item => firstPingYin + item.Substring(0, 1)));
                    }
                    newFirstPingYinList = newFirstPingYinList.Distinct().ToList();
                    result = newFirstPingYinList;
                }
            }
            return result;
        }
        #endregion
    }
}