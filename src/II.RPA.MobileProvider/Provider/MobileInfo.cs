using II.RPA.MobileProvider.Params;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Provider
{
    public class MobileInfo
    {
        public string Udid { get; set; }

        public string PlatformName { get; set; }

        public string PlatformVersion { get; set; }

        public bool ForceNewSession { get; set; }

        //此部分预留（为后续可选参数做动态配置）
        public Dictionary<string, string> AdditionalCapabilities { get; set; }
        public void Replace(MobileInfo other)
        {
            if (this != other)
            {
                PlatformName = other.PlatformName;
                PlatformVersion = other.PlatformVersion;
                AdditionalCapabilities = other.AdditionalCapabilities;
            }
        }

        public bool IsAutoUnlock { get; set; }
        public UnlockType UnlockType { get; set; }
        public string UnlockPw { get; set; }
    }
}
