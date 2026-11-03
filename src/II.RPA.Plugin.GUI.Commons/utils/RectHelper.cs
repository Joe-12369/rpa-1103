using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using FlaUI.Core.Tools;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace II.RPA.Plugin.GUI.Commons
{
    public class RectHelper
    {
        private static ILogger logger = Logger.OfContext();

        public static Rectangle FromAraeArr(int[] area)
        {
            if (area.Length != 4)
            {
                throw new Exception("格式异常，int[]长度必须等于4".I18n("controlType_RectHelper_AADE4DBF"));
            }
            return new Rectangle(area[0], area[1], area[2], area[3]);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static Rectangle PointsToRectangle(float[][] points)
        {
            int locationX = (int)points[0][0];
            int locationY = (int)points[0][1];
            int width = (int)points[1][0] - (int)points[0][0];
            int height = (int)points[2][1] - (int)points[1][1];
            return new Rectangle(locationX, locationY, width, height);
        }

        public static Rectangle GetWindowRect(IntPtr wndHandle)
        {
            if (NativeMethods.GetWindowRect(wndHandle, out var wndRect))
            {
                return new Rectangle(
                    wndRect.Left,
                    wndRect.Top,
                    wndRect.Right - wndRect.Left,
                    wndRect.Bottom - wndRect.Top
                    );
            }
            return Rectangle.Empty;
        }

        public static Point RemotePositionCorrection(Rectangle localRefRect, Rectangle remoteRefRect, float x, float y)
        {
            var remoteRect = new Rectangle((int)x, (int)y,0, 0);//为复用代码，将点问
            var rect = RemoteRectCorrection(localRefRect, remoteRefRect, remoteRect);
            return new Point(rect.X, rect.Y);
        }

        public static Rectangle RemotePointsCorrection(Rectangle localRefRect, Rectangle remoteRefRect, float[][] points)
        {
            var remoteRect = PointsToRectangle(points);
            return RemoteRectCorrection(localRefRect, remoteRefRect, remoteRect);
        }
        public static Rectangle RemoteRectCorrection(Rectangle localRefRect, Rectangle remoteRefRect, Rectangle remoteRect)
        {

            return RemoteRectCorrection(localRefRect, remoteRefRect, new List<Rectangle>() { remoteRect }).First();
        }
        public static IEnumerable<Rectangle> RemoteRectCorrection(Rectangle localRefRect, Rectangle remoteRefRect, IEnumerable<Rectangle> remoteRectList)
        {
            float correctionRatioX = 1, correctionRatioY = 1, correctionRatioW = 1, correctionRatioH = 1;
            if (!localRefRect.Equals(Rectangle.Empty))
            {
                //logger.LogDebug($"localRefRect.Equals(Rectangle.Empty)：{localRefRect}||||{remoteRefRect}");
                //correctionRatioX = localRefRect.X / remoteRefRect.X;
                //correctionRatioY = localRefRect.Y / remoteRefRect.Y;
                correctionRatioW = (float)localRefRect.Width / remoteRefRect.Width;
                correctionRatioH = (float)localRefRect.Height / remoteRefRect.Height;
            }
            //logger.LogDebug($"correctionRatioW：{correctionRatioW},correctionRatioH:{correctionRatioH}");
            foreach (var remoteRect in remoteRectList)
            {
                yield return new Rectangle(
                        (int)(remoteRect.X * correctionRatioW),
                        (int)(remoteRect.Y * correctionRatioH),
                        (int)(remoteRect.Width * correctionRatioW),
                        (int)(remoteRect.Height * correctionRatioH)
                    );
            }
        }
    }
}
