using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons.Params
{
    public enum ElementRelateMode
    {
        None = 0,
        ToParent = 1,
        InSameIframe  = 2
    }
    public class ParamQueryTargetSelectorAllModel
    {
        public string RelateElementId { get; set; }
        public string selector { get; set; }
        public string attributeName { get; set; }
        public int startIndex { get; set; }
        public int endIndex { get; set; }
        public bool isCompensationLink { get; set; }
        public bool isAntitone { get; set; }
        public string selectorVersion { get; set; }
        public bool discernHidden { get; set; }

        public ElementRelateMode ElementRelateMode { get; set; } = ElementRelateMode.None;
    }
}
