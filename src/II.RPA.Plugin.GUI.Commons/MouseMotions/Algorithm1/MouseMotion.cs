using II.RPA.Plugin.GUI.Commons.MouseMotions.Algorithm1;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography;
using System.Threading;

namespace II.RPA.Plugin.GUI.Commons.MouseMotions
{
    internal class MouseMotion
    {
        private readonly int _minSteps;
        private readonly int _effectFadeSteps;
        private readonly double _timeToStepsDivider;
        private readonly IDeviationProvider _deviationProvider;
        private readonly INoiseProvider _noiseProvider;
        private readonly ISpeedManager _speedManager;
        private readonly OvershootManager _overshootManager;
        private readonly int _xDest;
        private readonly int _yDest;
        private readonly Size _screenSize;
        private readonly Random _random;

        public MouseMotion(MouseMotionNature nature, Random random, int xDest, int yDest, int maxX, int maxY)
        {
            _deviationProvider = nature.DeviationProvider;
            _noiseProvider = nature.NoiseProvider;
            _screenSize = new Size(maxX, maxY);
            _xDest = LimitByScreenWidth(xDest);
            _yDest = LimitByScreenHeight(yDest);
            _random = random;
            _speedManager = nature.SpeedManager;
            _timeToStepsDivider = nature.TimeToStepsDivider;
            _minSteps = nature.MinSteps;
            _effectFadeSteps = nature.EffectFadeSteps;
            _overshootManager = nature.OvershootManager;
        }

        private bool ToDest(int current, int dest) => Math.Abs(current - dest) <= 2;

        public IEnumerable<MouseRoute> BuildRoutes(
            double xNow,
            double yNow)

        {
            var result = new Queue<MouseRoute>();

            MovementFactory movementFactory =
                new MovementFactory(
                    _xDest,
                    _yDest,
                    _speedManager,
                    _overshootManager,
                    _screenSize);

            Queue<Movement> movements = movementFactory.CreateMovements(
                new POINT((int)xNow, (int)yNow));

            var lpPoint = new POINT((int)xNow, (int)yNow);

            while (!ToDest(lpPoint.x, _xDest) || !ToDest(lpPoint.y, _yDest))
            {
                if (movements.Count == 0)
                {
                    movements = movementFactory.CreateMovements(lpPoint);
                }

                Movement movement = movements != null && movements.Count != 0 ?
                    movements.Dequeue() :
                    throw new Exception("no available movements");

                if (movements.Count > 0)
                {

                }

                double distance = movement.Distance;
                long time = movement.Time;
                Flow flow = movement.Flow;
                double xdistance = movement.XDistance;
                double ydistance = movement.YDistance;

                int steps = (int)Math.Ceiling(Math.Min(distance, Math.Max(time / _timeToStepsDivider, _minSteps)));
                double x1 = lpPoint.x;
                double y1 = lpPoint.y;
                double num4 = (_random.NextDouble() - 0.5) * 2.0;
                double num5 = (_random.NextDouble() - 0.5) * 2.0;
                double x2 = 0.0;
                double y2 = 0.0;
                double num6 = 0.0;
                double num7 = 0.0;

                for (int index = 0; index < steps; ++index)
                {
                    double completion = index / (double)steps;
                    double num8 = (_effectFadeSteps - (double)Math.Max(index - (steps - _effectFadeSteps) + 1, 0))
                        / _effectFadeSteps;
                    double stepSize1 = flow.GetStepSize(xdistance, steps, completion);
                    double stepSize2 = flow.GetStepSize(ydistance, steps, completion);
                    x2 += stepSize1;
                    y2 += stepSize2;
                    double completionFraction = Math.Min(1.0, Utils.Hypot(x2, y2) / distance);

                    DoublePoint noise = _noiseProvider.getNoise(_random, stepSize1, stepSize2);
                    DoublePoint deviation = _deviationProvider.GetDeviation(distance, completionFraction);

                    num6 += noise.X;
                    num7 += noise.Y;
                    x1 += stepSize1;
                    y1 += stepSize2;

                    int nextX = Utils.RoundTowards(
                        x1 + deviation.X * num4 * num8 + num6 * num8,
                        movement.DestX);

                    int nexY = Utils.RoundTowards(
                        y1 + deviation.Y * num5 * num8 + num7 * num8,
                        movement.DestY);

                    result.Enqueue(new MouseRoute(nextX, nexY));
                    result.Enqueue(new MouseRoute(nextX, nexY, (int)time / steps));

                    lpPoint = new POINT(nextX, nexY);
                }
            }

            return result;
        }

        private void StepTowards()
        {

        }

        private int LimitByScreenWidth(int value)
            => Math.Max(0, Math.Min(_screenSize.Width - 1, value));

        private int LimitByScreenHeight(int value)
            => Math.Max(0, Math.Min(_screenSize.Height - 1, value));

        private void SleepAround(long sleepMin, long randomPart)
            => Thread.Sleep((int)(sleepMin + _random.NextDouble() * randomPart)); 
    }
}