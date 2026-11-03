using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic.Params
{
    public class ParamCloseAllModel : ParamModel
    {
        public string closeMode { get; set; }
        public int defaultTimeout { get; set; } = 5000;
    }
}
