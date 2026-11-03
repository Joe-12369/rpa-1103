using System;

namespace II.RPA.Plugin.GUI.Commons.MouseMotions.Algorithm1
{
    internal class DefaultNoiseProvider : INoiseProvider
    {
        public static readonly double DEFAULT_NOISINESS_DIVIDER = 2.0;
        private static readonly double SMALL_DELTA = 1E-05;
        private readonly double _noisinessDivider;

        public DefaultNoiseProvider(double noisinessDivider)
            => _noisinessDivider = noisinessDivider;

        public DoublePoint getNoise(Random random, double xStepSize, double yStepSize)
        {
            if (Math.Abs(xStepSize - 0.0) < SMALL_DELTA &&
                Math.Abs(yStepSize - 0.0) < SMALL_DELTA)
            {
                return DoublePoint.ZERO;
            }

            double x = 0.0;
            double y = 0.0;
            double num1 = Utils.Hypot(xStepSize, yStepSize);
            double num2 = Math.Max(0.0, 8.0 - num1) / 50.0;

            if (random.NextDouble() < num2)
            {
                x = (random.NextDouble() - 0.5) * Math.Max(0.0, 8.0 - num1) / _noisinessDivider;
                y = (random.NextDouble() - 0.5) * Math.Max(0.0, 8.0 - num1) / _noisinessDivider;
            }

            return new DoublePoint(x, y);
        }
    }
}
