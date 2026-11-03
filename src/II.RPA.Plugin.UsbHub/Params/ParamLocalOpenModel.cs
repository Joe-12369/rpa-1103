using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.UsbHub.Params
{
    public class ParamLocalOpenModel : II.RPA.Plugin.Core.PluginBase.PluginParameterBase
    {
        public string PortName { get; set; }
        public string Channel { get; set; }
    }
}
