using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons.Params;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic.Params
{
    public class ParamQuerySelectorModel : ParamModel
    {
        public string selector { get; set; }
        public string cssSelector { get; set; }
        public string version { get; set; }
        public string mode { get; set; }
        public int index { get; set; }
        public string RelateElementId { get; set; }
        public ElementRelateMode ElementRelateMode { get; set; } = ElementRelateMode.None;


        public string resultAttrName { get; set; }
        public ElementRelativeType relativeType { get; set; }
        public string positionNum { get; set; }
        public string filter { get; set; }
        public bool discernHidden { get; set; }

        public string xpathSelector { get; set; }
    }
}
