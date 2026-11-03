using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamMouseHoverModel : ParamBase
    {
        public int timeout { get; set; } = 10000;
        public int offsetX { get; set; }
        public int offsetY { get; set; }
        public int cursorPos { get; set; } = 0;
        public string cvExecutionServiceURL { get; set; } = "";
        public int speed = 60; //鼠标移速
        public int personification = 0; //是否拟人
        public int instant = 0; //是否瞬间移动
        /// <summary>
        /// 是否使用模拟
        /// 1 是
        /// 0 否
        /// </summary>
        public int isUsePerson { get; set; }
    }
}