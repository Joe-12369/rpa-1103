using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic.Params
{
    public class ParamAssociatedElementModel : ParamModel
    {
        public string filter { get; set; }
        public int level { get; set; }
        public string index{ get; set; }
        public int position { get; set; }
    }
}
