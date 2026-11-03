using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic.Params
{
    public class ParamSelectOptionModel : ParamModel
    {
        public string type { get; set; }
        public int selected { get; set;  }
        public List<int> idxList { get; set; }
    }
}
