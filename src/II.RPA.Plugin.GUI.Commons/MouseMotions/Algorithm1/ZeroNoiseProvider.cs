using System;

namespace II.RPA.Plugin.GUI.Commons.MouseMotions.Algorithm1
{
    internal class ZeroNoiseProvider : INoiseProvider
    {
        public DoublePoint getNoise(
            Random random,
            double xStepSize,
            double yStepSize) => DoublePoint.ZERO;
    }
}
