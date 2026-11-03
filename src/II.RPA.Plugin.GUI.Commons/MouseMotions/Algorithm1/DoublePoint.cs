namespace II.RPA.Plugin.GUI.Commons.MouseMotions.Algorithm1
{
    internal class DoublePoint
    {
        public static readonly DoublePoint ZERO = new DoublePoint(0.0, 0.0);

        public double X { get; set; }

        public double Y { get; set; }

        public DoublePoint(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
}
