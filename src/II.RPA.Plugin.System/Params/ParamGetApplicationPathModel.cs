using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.RPASystem.Params
{
    public class ParamGetApplicationPathModel : II.RPA.Plugin.Core.PluginBase.PluginParameterBase
    {
        public string[] AppProcessNames { get; set; }
        public string[] DisplayNames { get; set; }
        public string[] ExceFileNames { get; set; }

    }
}
