using System;

namespace II.RPA.Plugin.GUI.Commons.MouseMotions.Algorithm1
{
    internal interface INoiseProvider
    {
        DoublePoint getNoise(Random random, double xStepSize, double yStepSize);
    }
}
