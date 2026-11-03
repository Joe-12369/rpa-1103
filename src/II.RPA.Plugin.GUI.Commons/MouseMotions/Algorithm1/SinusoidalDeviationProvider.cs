using System;

namespace II.RPA.Plugin.GUI.Commons.MouseMotions.Algorithm1
{
    internal class SinusoidalDeviationProvider : IDeviationProvider
    {
        public static readonly int DEFAULT_SLOPE_DIVIDER = 10;
        private readonly double _slopeDivider;

        public SinusoidalDeviationProvider(double slopeDivider) => _slopeDivider = slopeDivider;

        public DoublePoint GetDeviation(
          double totalDistanceInPixels,
          double completionFraction)
        {
            double num1 = (1.0 - Math.Cos(completionFraction * Math.PI * 2.0)) / 2.0;
            double num2 = totalDistanceInPixels / _slopeDivider;
            double num3 = totalDistanceInPixels / _slopeDivider;
            return new DoublePoint(num1 * num2, num1 * num3);
        }
    }
}
