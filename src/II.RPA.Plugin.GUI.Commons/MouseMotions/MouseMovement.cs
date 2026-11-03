using II.RPA.Plugin.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons.MouseMotions
{
    public class MouseMovement
    {
        private MouseMovement()
        {
        }

        /// <summary>
        /// 目标点X
        /// </summary>
        public int EndX { get; set; }

        /// <summary>
        /// 目标点Y
        /// </summary>
        public int EndY { get; set; }

        /// <summary>
        /// 移速
        /// </summary>
        public int Speed { get; set; }

        /// <summary>
        /// 拟人
        /// </summary>
        public bool PersonLike { get; set; }

        /// <summary>
        /// 不生成轨迹（瞬间移动）
        /// </summary>
        public bool Instant { get; set; }

        /// <summary>
        /// 限定匀速直线
        /// </summary>
        public bool LimitingSpeed { get; set; }

        public static MouseMovement FromElement(
            Rectangle elementRect,
            int offsetX,
            int offsetY,
            int pickMode,
            int speed,
            bool personLike,
            bool instant,
            bool limitingSpeed)
        {
            var point = PickElementPoint(
                elementRect,
                pickMode,
                offsetX,
                offsetY);

            return FromAccurate(point.X, point.Y, speed, personLike, instant,limitingSpeed);
        }

        public static MouseMovement FromAccurate(
            int endX,
            int endY,
            int speed,
            bool personLike,
            bool instant,
            bool limitingSpeed)
        {
            return new MouseMovement
            {
                EndX = endX,
                EndY = endY,
                Speed = speed,
                PersonLike = personLike,
                Instant = instant,
                LimitingSpeed = limitingSpeed
            };
        }

        public static Point PickElementPoint(
            Rectangle elementRect,
            int pickMode,
            int offsetX,
            int offsetY)
        {
            var TargetPoint = new Point();

            if (pickMode == 0)
            {
                TargetPoint.X = elementRect.X + elementRect.Width / 2 + offsetX;
                TargetPoint.Y = elementRect.Y + elementRect.Height / 2 + offsetY;
            }
            else if (pickMode == 1)//左上
            {
                TargetPoint.X = elementRect.X + offsetX;
                TargetPoint.Y = elementRect.Y + offsetY;
            }
            else if (pickMode == 2)//右上
            {
                TargetPoint.X = elementRect.X + elementRect.Width + offsetX;
                TargetPoint.Y = elementRect.Y + offsetY;
            }
            else if (pickMode == 3)//左下
            {
                TargetPoint.X = elementRect.X + offsetX;
                TargetPoint.Y = elementRect.Y + elementRect.Height + offsetY;
            }
            else if (pickMode == 4)//右下
            {
                TargetPoint.X = elementRect.X + elementRect.Width + offsetX;
                TargetPoint.Y = elementRect.Y + elementRect.Height + offsetY;
            }
            else if (pickMode == 5)//随机
            {
                TargetPoint = elementRect.GetRandomPoint();
            }

            return TargetPoint;
        }
    }
}
