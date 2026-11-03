namespace II.RPA.Plugin.GUI.Commons.MouseMotions.Algorithm1
{
    internal class ZeroDeviationProvider : IDeviationProvider
    {
        public DoublePoint GetDeviation(
          double totalDistanceInPixels,
          double completionFraction)
        {
            return DoublePoint.ZERO;
        }
    }
}
