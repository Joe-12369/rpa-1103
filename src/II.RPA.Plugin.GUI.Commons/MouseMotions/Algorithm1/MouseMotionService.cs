using AutoIt;
using II.RPA.Plugin.GUI.Commons.MouseMotions.Algorithm1;
using System;
using System.Collections.Generic;

namespace II.RPA.Plugin.GUI.Commons.MouseMotions.Algorithm1
{
    public class MouseMotionService
    {
        private static MouseMotionFactory[] factories = new MouseMotionFactory[3];

        private static MouseMotionFactory CreateFactory(MouseMoveSpeed speed)
        {
            if (speed == MouseMoveSpeed.Instant)
            {
                return null;
            }

            MouseMotionFactory factory = factories[(int)(speed - 1)];

            if (factory == null)
            {
                switch (speed)
                {
                    case MouseMoveSpeed.Fast:
                        factory = FactoryTemplates.CreateFastGamerMotionFactory();
                        break;
                    case MouseMoveSpeed.Slow:
                        factory = FactoryTemplates.CreateGrannyMotionFactory();
                        break;
                    default:
                        factory = FactoryTemplates.CreateAverageComputerUserMotionFactory();
                        break;
                }

                factories[(int)(speed - 1)] = factory;
            }
            return factory;
        }

        public static IEnumerable<MouseRoute> BuildRoues(
            int sX,
            int sY,
            int tX,
            int tY,
            int maxX,
            int maxY,
            MouseMoveSpeed speed = MouseMoveSpeed.Middle)
        {
            if (speed == MouseMoveSpeed.Instant)
            {
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
                    var route = new MouseRoute(point.x + tX, point.y + tY);
                    result.Add(route);
                    result.Add(new MouseRoute(route.X, route.Y, 7));
                }

                return result;
            }
            else
            {
                return CreateFactory(speed).BuildRoutes(sX, sY, tX, tY, maxX, maxY);
            }
        }
    }
}