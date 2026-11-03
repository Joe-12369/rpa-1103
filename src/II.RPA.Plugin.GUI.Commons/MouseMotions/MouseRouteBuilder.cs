using FlaUI.Core.Input;
using II.RPA.Plugin.GUI.Commons.MouseMotions.Algorithm1;
using MouseMotion;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace II.RPA.Plugin.GUI.Commons.MouseMotions
{
    public class MouseRouteHelper
    {
        public static IEnumerable<MouseRoute> Generate(
            MouseRouteBuildParams buildParams)
        {
            if (buildParams.Instant)
            {
                //瞬间移动，虽然是瞬间移动，但是会生成围绕中心的一个短路径来规避问题
                var circlePoints = new[]
                {
                    new POINT(2,1),
                    new POINT(1,2),
                    new POINT(3,0),
                    new POINT(2,-1),
                    new POINT(2,-2),
                    new POINT(-1,1),
                    new POINT(0,2),
                    new POINT(1,1),
                    new POINT(0,1),
                    new POINT(1,0),
                    new POINT(0,1),
                };

                var result = new List<MouseRoute>(20);

                foreach (var point in circlePoints)
                {
                    var route = new MouseRoute(point.x + buildParams.EndX, point.y + buildParams.EndY);
                    result.Add(route);
                    result.Add(new MouseRoute(route.X, route.Y, 7));
                }

                return result;
            }


            var maxStep = GetMaxStep(buildParams.Speed);

            if (buildParams.Algorithm == MouseRouteBuildAlgorithm.Algorizhm1)
            {
                var speed = maxStep >= 8 ? MouseMoveSpeed.Middle :
                    maxStep >= 16 ? MouseMoveSpeed.Fast :
                    MouseMoveSpeed.Slow;

                //基于行为模式的模拟轨迹生成
                return MouseMotionService.BuildRoues(
                    buildParams.StartX,
                    buildParams.StartY,
                    buildParams.EndX,
                    buildParams.EndY,
                    buildParams.ScreenWidth,
                    buildParams.ScreenHeight,
                    speed);
            }
            else
            {
                //基于WindMouse的模拟轨迹生成
                //    return WindMouseRouteBuilder.GenerateByWindMouse(
                //        buildParams.StartX,
                //        buildParams.StartY,
                //        buildParams.EndX,
                //        buildParams.EndY,
                //        10, //拉扯系数
                //        7, //干扰系数
                //        8, //最小间隔
                //        9, //最大间隔
                //        maxStep, //最大步长
                //        0.18, //近距检测
                //        600, //连续移动最小限制
                //        800, //连续移动最大限制
                //        buildParams.ScreenWidth,
                //        buildParams.ScreenHeight);
                double maxStep1 = double.MinValue;
                int minWait = 0;
                int maxWait = 0;
                if (buildParams.Speed <= 70)
                {
                    maxStep1 = buildParams.Speed / 2.5;
                    minWait = 5;
                    maxWait = 5;
                }
                else
                {
                    maxStep1 = (buildParams.Speed - 60) / 0.3;
                }
                return WindMouseRouteBuilder.GenerateByWindMouse(
                    buildParams.StartX,
                    buildParams.StartY,
                    buildParams.EndX,
                    buildParams.EndY,
                    80, //拉扯系数
                    2, //干扰系数
                    minWait, //最小间隔
                    maxWait, //最大间隔
                    maxStep1, //最大步长
                    0.1, //近距检测
                    600, //连续移动最小限制
                    800, //连续移动最大限制
                    buildParams.ScreenWidth,
                    buildParams.ScreenHeight);

            }
        }
        public static int GetMaxStep(int relativeSpeed)
        {
            //速度规范，慢：8，中：16，快32，极快：64，-1表示瞬间移动
            //归一化成百分比：1~100

            const int slowStep = 8;
            const int quickStep = 32;
            const int maxStep = 64;

            const int minSpeed = 1;
            const int quickSpeed = 60;
            const int maxSpeed = 100;

            if (relativeSpeed < minSpeed || relativeSpeed > maxSpeed)
            {
                return (slowStep + quickStep) / 2;
            }

            if (relativeSpeed <= quickSpeed)
            {
                return slowStep + (int)((relativeSpeed - minSpeed) / (quickSpeed - minSpeed) * 1d)
                    * (quickStep - slowStep);
            }

            return quickStep + (int)((relativeSpeed - quickSpeed) / (maxSpeed - quickSpeed) * 1d)
                * (maxStep - quickStep);
        }

        public static void Move(
            IEnumerable<MouseRoute> routes)
        {
            foreach (var route in routes)
            {
                Mouse.Position = new Point(route.X, route.Y);

                if (route.Delay > 0)
                {
                    Utils.AcurateDelay(route.Delay.Value);
                }
            }
        }

        public static void GenerateAndMove(
            MouseRouteBuildParams buildParams)
        {
            var routes = Generate(buildParams);
            Move(routes);
        }
    }
}