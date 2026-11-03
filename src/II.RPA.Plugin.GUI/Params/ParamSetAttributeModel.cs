using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamSetAttributeModel : ParamBase
    {
        public string name { get; set; }
        public string value { get; set; } = null;
    }
}
