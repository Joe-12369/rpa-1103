using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.RPASystem.Params
{
    public class ParamRDPUnlockModel : II.RPA.Plugin.Core.PluginBase.PluginParameterBase
    {
        public string UserName { get; set; }
        public string Pwd { get; set; }
    }
}
