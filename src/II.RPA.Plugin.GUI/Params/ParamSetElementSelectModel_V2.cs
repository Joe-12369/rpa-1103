using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamSetElementSelectModel_V2 : ParamBase
    {
        public string setValue { get; set; }
        /// <summary>
        /// 0 按索引 1：按文本 2: 按 value (针对浏览器)
        /// </summary>
        public int selectType { get; set; }
        public int timeout { get; set; } = 10000;
        /// <summary>
        /// 0 精准匹配 1 模糊匹配 2 正则表达式
        /// </summary>
        public int matchMode { get; set; }
    }
}
