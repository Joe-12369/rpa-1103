using FlaUI.Core.Input;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Drawing;
using System.Threading;

namespace II.RPA.Plugin.GUI.Commons.MouseMotions
{
    public class MouseMotionHelper
    {
        private static ILogger logger = Logger.OfContext();
        public static void PersonMoveMouse(bool isUsePerson,Point point)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start(); 
            if (isUsePerson)
            {
                if (Mouse.Position.X != point.X || Mouse.Position.Y != point.Y)
                {
                    PersonMoveMouse(point, 65, true, false);
                    logger.LogDebug($"PersonMoveMouse:{stopwatch.ElapsedMilliseconds}");
                    Thread.Sleep(100);
                    Mouse.Position = point;
                }
                else
                {
                    logger.LogInformation("当前鼠标位置和目标位置完全一致、取消本次移动操作!");
                }
                stopwatch.Stop();

            }
            else
            {
                Mouse.Position = point;
                logger.LogDebug($"PositionMoveMouse:{stopwatch.ElapsedMilliseconds}");
                stopwatch.Stop();
            }
        }

        public static void PersonMoveMouse(Point point, int speed = 50, bool isPersonLike = true, bool Instant = false, bool limitingSpeed = false)
        {
            MouseMovement mouseMovement = MouseMovement.FromAccurate(point.X, point.Y, speed, isPersonLike, Instant, limitingSpeed);

            II.RPA.Plugin.GUI.Commons.MouseMotions.MouseMotionHelper.MoveMouse(mouseMovement);
        }
        private static void MoveMouse(MouseMovement movement)
        {
            if (movement.Instant)
            {
                //瞬间移动
                //虽然是瞬间移动，但是会在目标点周围形成非常短的轨迹用来规避一些问题
                var param = new MouseRouteBuildParams(
                            movement.EndX,
                            movement.EndY,
                            -1,
                            movement.Instant,
                            MouseRouteBuildAlgorithm.Algorizhm2);
                MouseRouteHelper.GenerateAndMove(param);
                return;
            }

            if (movement.PersonLike)
            {
                //拟人移动，这里使用了WindMouse算法
                var param = new MouseRouteBuildParams(
                            movement.EndX,
                            movement.EndY,
                            movement.Speed,
                            movement.Instant,
                            MouseRouteBuildAlgorithm.Algorizhm2);
                MouseRouteHelper.GenerateAndMove(param);
                return;
            }
            if (movement.LimitingSpeed)
            {
                Mouse.MovePixelsPerMillisecond = movement.Speed;

            }
            //默认的移动行为
            Mouse.MoveTo(movement.EndX, movement.EndY);
        }
    }
}