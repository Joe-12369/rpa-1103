
namespace II.RPA.MobileDeviceManager.Common
{
    public class Device
    {
        public string Udid { get; set; }

        public bool IsReady { get; set; }

        public bool WifiIsOffline { get; set; }

        public string Model { get; set; }

        public string Version { get; set; }

        public string Brand { get; set; }

        public string Name { get; set; }
        public bool IsConnected { get; set; }
    }
}
