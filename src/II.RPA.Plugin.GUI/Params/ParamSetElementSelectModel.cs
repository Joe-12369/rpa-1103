using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamSetElementSelectModel : ParamBase
    {
        public string setValue { get; set; }
        /// <summary>
        /// 0 按索引 1：按文本 2: 按 value (针对浏览器)
        /// </summary>
        public int selectType { get; set; }
        public int timeout { get; set; } = 10000;
        public bool isMultiple { get; set; } = false;
    }
}
