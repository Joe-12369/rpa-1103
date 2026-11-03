using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic.Params
{
    public class ParamScrollToModel : ParamModel
    {
        public int top { get; set; }
        public int left { get; set; }
        public string behavior { get; set; } = "smooth";
    }
}
