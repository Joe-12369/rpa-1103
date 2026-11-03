using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Window.Params
{
    public class ParamCloseWindowModel : ParamBase
    {
        public int timeout { get; set; } = 10000;
        public int IsKillProcess { get; set; } //0-是 1-否
    }
}
