using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.RPASystem.Params
{
    public class ParamRDPLockModel : II.RPA.Plugin.Core.PluginBase.PluginParameterBase
    {
        public string UserName { get; set; }

        public string Pwd { get; set; }

        public int? Height { get; set; }

        public int? Width { get; set; }

        public int? ScaleFactor { get; set; }

        public int? Port { get; set; }
    }
}
