using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic.Params
{
    public class ParamBuildSelectorModel : ParamModel
    {
        public bool innerTextFirst { get; set; }
        public bool rawData { get; set; }
        public bool excludeInnerText { get; set; }
        public bool excludeHiddenEle { get; set; } = true;//这里作为730版本的默认行为，属性控制提供更多扩展

        public List<Dictionary<string, object>> ValidNodeAttributes { get; set; }
    }
}
