using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamGetRegionModel : ParamBase
    {
        /// <summary>
        /// 1 父元素相对位置 2 窗口相对位置 3 绝对位置
        /// </summary>
        public int positionType { get; set; }
        public int timeout { get; set; } = 10000;

        public string cvExecutionServiceURL { get; set; } = "";
    }
}
