using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.UsbHub.Params
{
    public class ParamNetworkCloseModel : II.RPA.Plugin.Core.PluginBase.PluginParameterBase
    {
        public string IpAddress { get; set; }
        public int Port { get; set; }
        public string Channel { get; set; }
    }
}
