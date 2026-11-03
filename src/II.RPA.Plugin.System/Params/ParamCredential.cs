using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.RPASystem.Params
{
    public class ParamCredential : II.RPA.Plugin.Core.PluginBase.PluginParameterBase
    {
        public string application { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public int type { get; set; }
        public int persistence { get; set; }
    }
}
