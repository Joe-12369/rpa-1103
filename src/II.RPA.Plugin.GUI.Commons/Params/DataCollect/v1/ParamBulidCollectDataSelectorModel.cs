using II.RPA.Plugin.Core.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons.Params
{
    public class ParamBulidCollectDataSelectorModel
    {
        /// <summary>
        /// 初始状态的Selector（与普通的元素结构规则相同）
        /// </summary>
        public List<SelectorProps> OriginalSelector { get; set; }
        /// <summary>
        /// 只包含标签的Selector（与普通的元素结构相比，结构层级相同，但不包含属性）
        /// </summary>
        public List<SelectorProps> OnlyContainsTagsSelector { get; set; }

        /// <summary>
        /// 用来作为对比生成数据采集选择器的Selector（初始状态的Selector）
        /// </summary>
        public List<SelectorProps> CompareOriginalSelector { get; set; } = new List<SelectorProps>();
    }
}
