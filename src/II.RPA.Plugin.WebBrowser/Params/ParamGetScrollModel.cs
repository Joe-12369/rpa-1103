using II.RPA.WebBrowser.Common.Basic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.WebBrowser.Params
{
    public class ParamGetScrollModel : ParamBase
    {
        /// <summary>
        /// 是否网页中元素内滚动条
        /// </summary>
        public bool isElementOfScroll { get; set; }
        public string selector { get; set; }
        /// <summary>
        /// 滚动类型
        /// </summary>
        public DirectionType directionType { get; set; }
    }
}
