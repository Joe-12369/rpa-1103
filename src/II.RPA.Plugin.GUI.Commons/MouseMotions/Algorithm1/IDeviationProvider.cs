namespace II.RPA.Plugin.GUI.Commons.MouseMotions.Algorithm1
{
    internal interface IDeviationProvider
    {
        DoublePoint GetDeviation(double totalDistanceInPixels, double completionFraction);
    }
}
