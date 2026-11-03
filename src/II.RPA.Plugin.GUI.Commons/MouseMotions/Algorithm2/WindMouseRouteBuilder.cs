using II.RPA.Plugin.GUI.Commons.MouseMotions;
using System;
using System.Collections.Generic;
using static II.RPA.Plugin.GUI.Commons.MouseMotions.Utils;

namespace MouseMotion
{
    public static class WindMouseRouteBuilder
    {
        private static Random random = new Random();

        public static IEnumerable<MouseRoute> GenerateByWindMouse(
            double xs,
            double ys,
            double xe,
            double ye,
            double gravity,
            double wind,
            double minWait,
            double maxWait,
            double maxStep,
            double taRatio,
            int minSplit,
            int maxSplit,
            double xLimit,
            double yLimit)
        {
            double tDist, dist, windX = 0, windY = 0, veloX = 0, veloY = 0, randomDist, veloMag, step;
            double moved = 0;
            int oldX, oldY, newX = (int)Math.Round(xs), newY = (int)Math.Round(ys);

            double waitDiff = maxWait - minWait;
            double sqrt2 = Math.Sqrt(2.0);
            double sqrt3 = Math.Sqrt(3.0);
            double sqrt5 = Math.Sqrt(5.0);
            var split = random.Next(minSplit, maxSplit);

            tDist = dist = Hypot(xe - xs, ye - ys);

            while (dist > 1.0)
            {
                wind = Math.Min(wind, dist);

                if (dist >= Math.Max(tDist * taRatio, maxStep))
                {
                    int wxSeed = random.Next((int)Math.Round(wind) * 2 + 1);
                    int wySeed = random.Next((int)Math.Round(wind) * 2 + 1);
                    windX = windX / sqrt3 + (wxSeed - wind) / sqrt5;
                    windY = windY / sqrt3 + (wySeed - wind) / sqrt5;
                }
                else
                {
                    windX = windX / sqrt2;
                    windY = windY / sqrt2;
                    if (maxStep < 3)
                        maxStep = random.Next(3) + 3.0;
                    else
                        maxStep = maxStep / sqrt5;
                }

                veloX += windX;
                veloY += windY;
                veloX = veloX + gravity * (xe - xs) / dist;
                veloY = veloY + gravity * (ye - ys) / dist;

                var deveation = Deviation(dist / tDist);
                veloX *= deveation;
                veloY *= deveation;

                if (Hypot(veloX, veloY) > maxStep)
                {
                    randomDist = maxStep / 2.0 + random.Next((int)Math.Round(maxStep) / 2);
                    veloMag = Hypot(veloX, veloY);
                    veloX = (veloX / veloMag) * randomDist;
                    veloY = (veloY / veloMag) * randomDist;
                }

                oldX = (int)Math.Round(xs);
                oldY = (int)Math.Round(ys);
                xs += veloX;
                ys += veloY;
                dist = Hypot(xe - xs, ye - ys);
                newX = (int)Math.Round(xs);
                newY = (int)Math.Round(ys);

                if (oldX != newX || oldY != newY)
                    yield return new MouseRoute(Limit(newX, (int)xLimit), Limit(newY, (int)yLimit));

                step = Hypot(xs - oldX, ys - oldY);
                moved += step;
                int wait = (int)Math.Round(waitDiff * (step / maxStep) + minWait);

                yield return new MouseRoute(Limit(newX, (int)xLimit), Limit(newY, (int)yLimit), wait);

                if (moved >= split)
                {
                    var adjust = GenerateByWindMouse(
                        xs,
                        ys,
                        xe,
                        ye,
                        gravity,
                        wind,
                        minWait,
                        maxWait,
                        maxStep / 1.1,
                        taRatio,
                        minSplit,
                        maxSplit,
                        xLimit,
                        yLimit);

                    var thinkWait = random.Next(50, 150);
                    yield return new MouseRoute(Limit(newX, (int)xLimit), Limit(newY, (int)yLimit), thinkWait);

                    foreach (var item in adjust)
                    {
                        yield return item;
                    }

                    yield break;
                }
            }

            int endX = (int)Math.Round(xe);
            int endY = (int)Math.Round(ye);

            if (endX != newX || endY != newY)
                yield return new MouseRoute(endX, endY);
        }

        private static double Deviation(double ratio)
        {
            return (1.0 - Math.Cos(ratio * Math.PI * 2.0)) * 0.5 / 2.0 + 0.5d;
        }

        private static int Limit(int value, int max, int min = 0)
        {
            return Math.Min(max, Math.Max(min, value));
        }
    }
}
