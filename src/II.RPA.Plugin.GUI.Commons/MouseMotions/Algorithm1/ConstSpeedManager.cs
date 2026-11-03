namespace II.RPA.Plugin.GUI.Commons.MouseMotions.Algorithm1
{
    internal class ConstSpeedManager : ISpeedManager
    {
        private long _motionTimeMsPer100Pixels;

        public ConstSpeedManager(long motionTimeMsPer100Pixels)
            => _motionTimeMsPer100Pixels = motionTimeMsPer100Pixels;

        public void GetFlowWithTime(double distance, out Flow flow, out long time)
        {
            flow = new Flow(FlowTemplates.ConstantSpeed());
            double num = _motionTimeMsPer100Pixels / 100.0;
            time = (long)(num * distance);
        }
    }
}
