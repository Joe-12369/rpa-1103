using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic.Params
{
    public class ParamcssSelectorModel : ParamModel
    {
        public string cssSelector { get; set; }
        public bool innerTextFirst { get; set; }
        public bool excludeHiddenEle { get; set; } = true;
        public List<string> cssSelectors { get; set; }
    }
}
