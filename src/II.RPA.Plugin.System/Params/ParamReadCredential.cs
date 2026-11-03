using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.RPASystem.Params
{
    public class ParamReadCredential : II.RPA.Plugin.Core.PluginBase.PluginParameterBase
    {
        public int type { get; set; }
        public object application { get; set; }
    }
}
