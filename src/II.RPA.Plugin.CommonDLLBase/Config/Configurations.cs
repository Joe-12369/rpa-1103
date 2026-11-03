using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.CommonDLLBase
{
    public class Configurations : ConfigurationBase<Configurations>
    {
        private Configurations()
        {
        }

        public int _ServiceHubPort = 36516;

        [JsonProperty("serviceHubPort")]
        public int ServiceHubPort
        {
            get
            {
                return _ServiceHubPort;
            }
            set
            {
                _ServiceHubPort = value;
                GlobleConfigurations.ServiceHubPort = _ServiceHubPort;
            }
        }

        public int _InitConnectTimeout = 3050;

        [JsonProperty("initConnectTimeout")]
        public int InitConnectTimeout
        {
            get
            {
                return _InitConnectTimeout;
            }
            set
            {
                _InitConnectTimeout = value;
                GlobleConfigurations.InitConnectTimeout = _InitConnectTimeout;
            }
        }

        public int _TransportConnectTimeout = 300;

        [JsonProperty("transportConnectTimeout")]
        public int TransportConnectTimeout
        {
            get
            {
                return _TransportConnectTimeout;
            }
            set
            {
                _TransportConnectTimeout = value;
                GlobleConfigurations.TransportConnectTimeout = _TransportConnectTimeout;
            }
        }

        public int _DeadlockErrorTimeout = 400;

        [JsonProperty("deadlockErrorTimeout")]
        public int DeadlockErrorTimeout
        {
            get
            {
                return _DeadlockErrorTimeout;
            }
            set
            {
                _DeadlockErrorTimeout = value;
                GlobleConfigurations.DeadlockErrorTimeout = _DeadlockErrorTimeout;
            }
        }

        private string _BrowserConnMode = string.Empty;
        [JsonProperty("browserConnMode")]
        public string BrowserConnMode
        {
            get
            {
                return _BrowserConnMode;
            }
            set
            {
                _BrowserConnMode = value;
                switch (value)
                {
                    case "NMH":
                        GlobleConfigurations.BrowserConnType = BrowserConnType.NativeMessageHost;
                        break;
                    case "SH":
                        GlobleConfigurations.BrowserConnType = BrowserConnType.ServiceHub;
                        break;
                    case "AUTO":
                    default:
                        GlobleConfigurations.BrowserConnType = BrowserConnType.ServiceHub | BrowserConnType.NativeMessageHost;
                        break;
                }
            }
        }
    }

}
