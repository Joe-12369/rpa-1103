using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamClickTableCellModel : ParamBase
    {
        public string rowNo { get; set; }
        public string columnNo { get; set; }
        public int clickType { get; set; }
        public int timeout { get; set; } = 10000;
    }
}
