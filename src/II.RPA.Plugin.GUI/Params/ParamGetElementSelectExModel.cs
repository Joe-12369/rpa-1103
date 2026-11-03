using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamGetElementSelectExModel:ParamBase
    {
        /// <summary>
        /// 0：获取当前 1: 获取全部
        /// </summary>
        public int selectMode { get; set; } = 0;
        /// <summary>
        /// 0：按索引（按顺序）1：按文本 2: 按 value (针对浏览器)
        /// </summary>
        public int selectType { get; set; } = 1;
        public int timeout { get; set; } = 10000;
    }
}
