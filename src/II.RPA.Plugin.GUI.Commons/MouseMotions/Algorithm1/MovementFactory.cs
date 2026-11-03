using System;
using System.Collections.Generic;
using System.Drawing;

namespace II.RPA.Plugin.GUI.Commons.MouseMotions.Algorithm1
{
    internal class MovementFactory
    {
        private readonly int _xDest;
        private readonly int _yDest;
        private readonly ISpeedManager _speedManager;
        private readonly OvershootManager _overshootManager;
        private readonly Size _screenSize;

        public MovementFactory(
          int xDest,
          int yDest,
          ISpeedManager speedManager,
          OvershootManager overshootManager,
          Size screenSize)
        {
            _xDest = xDest;
            _yDest = yDest;
            _speedManager = speedManager;
            _overshootManager = overshootManager;
            _screenSize = screenSize;
        }

        public Queue<Movement> CreateMovements(POINT currentMousePosition)
        {
            List<Movement> collection = new List<Movement>();
            int cX = currentMousePosition.x;
            int cY = currentMousePosition.y;
            int x1 = _xDest - cX;
            int y1 = _yDest - cY;
            double disatnce1 = Utils.Hypot(x1, y1);
            Flow flow1;
            long time1;
            _speedManager.GetFlowWithTime(disatnce1, out flow1, out time1);
            int overshoots = _overshootManager.GetOvershoots(flow1, time1, disatnce1);

            if (overshoots == 0)
            {
                collection.Add(new Movement()
                {
                    DestX = _xDest,
                    DestY = _yDest,
                    Distance = disatnce1,
                    XDistance = x1,
                    YDistance = y1,
                    Time = time1,
                    Flow = flow1
                });

                return new Queue<Movement>(collection);
            }

            for (int overshootsRemaining = overshoots; overshootsRemaining > 0; --overshootsRemaining)
            {
                IntPoint overshootAmount = _overshootManager.GetOvershootAmount(
                    _xDest - cX,
                    _yDest - cY,
                    time1,
                    overshootsRemaining);

                int num3 = LimitByScreenWidth(_xDest + overshootAmount.X);
                int num4 = LimitByScreenHeight(_yDest + overshootAmount.Y);
                int x2 = num3 - cX;
                int y2 = num4 - cY;

                double distance2 = Utils.Hypot(x2, y2);
                Flow flow2;
                _speedManager.GetFlowWithTime(distance2, out flow2, out long _);

                collection.Add(new Movement()
                {
                    DestX = num3,
                    DestY = num4,
                    Distance = distance2,
                    XDistance = x2,
                    YDistance = y2,
                    Time = time1,
                    Flow = flow2
                });

                cX = num3;
                cY = num4;

                time1 = _overshootManager.DeriveNextMouseMovementTimeMs(time1, overshootsRemaining - 1);
            }

            for (int index = collection.Count - 1; index > 0; --index)
            {
                Movement movement = collection[index];

                if (movement.DestX == _xDest && movement.DestY == _yDest)
                {
                    cX = movement.DestX - movement.XDistance;
                    cY = movement.DestY - movement.YDistance;
                    collection.RemoveAt(index);
                }
                else
                {
                    break;
                }
            }

            int x3 = _xDest - cX;
            int y3 = _yDest - cY;
            double distance3 = Utils.Hypot(x3, y3);
            Flow flow3;
            long time2;
            _speedManager.GetFlowWithTime(distance3, out flow3, out time2);
            long num5 = _overshootManager.DeriveNextMouseMovementTimeMs(time2, 0);

            collection.Add(new Movement()
            {
                DestX = _xDest,
                DestY = _yDest,
                Distance = distance3,
                XDistance = x3,
                YDistance = y3,
                Time = num5,
                Flow = flow3
            });

            return new Queue<Movement>(collection);
        }

        private int LimitByScreenWidth(int value) => Math.Max(0, Math.Min(_screenSize.Width - 1, value));

        private int LimitByScreenHeight(int value) => Math.Max(0, Math.Min(_screenSize.Height - 1, value));
    }
}
