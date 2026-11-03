using System.Collections.Generic;

namespace II.RPA.MobileDeviceManager.MobileInstall
{
    internal class ProcessItem
    {
        public double NeedHandleCount { get; set; }

        public double HadHandleCount { get; set; }

        public List<string> HandledFiles { get; set; }

        public ProcessItem(double NeedHandleCount)
        {
            this.NeedHandleCount = NeedHandleCount;
            HandledFiles = new List<string>();
        }
    }
}
