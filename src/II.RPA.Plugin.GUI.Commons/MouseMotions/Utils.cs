using System;
using System.Diagnostics;
using System.Threading;

namespace II.RPA.Plugin.GUI.Commons.MouseMotions
{
    public class Utils
    {
        public static Random Random { get; } = new Random();

        public static double NextDouble() => Random.NextDouble();

        public static double Hypot(int x, int y)
            => Math.Sqrt(Math.Pow(x, 2.0) + Math.Pow(y, 2.0));

        public static double Hypot(double x, double y)
            => Math.Sqrt(Math.Pow(x, 2.0) + Math.Pow(y, 2.0));

        public static long CurrentTimeMillis()
            => (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;

        public static int RoundTowards(double value, int target)
            => target > value ? (int)Math.Ceiling(value) : (int)Math.Floor(value);

        public static double MathHypot(double a, double b)
        {
            return Math.Sqrt(Math.Pow(a, 2) + Math.Pow(b, 2));
        }

        public static void AcurateDelay(int ms)
        {
            var watch = Stopwatch.StartNew();

            while (watch.ElapsedMilliseconds < ms)
            {
                Thread.Sleep(0);
            }

            watch.Stop();
        }
    }
}
