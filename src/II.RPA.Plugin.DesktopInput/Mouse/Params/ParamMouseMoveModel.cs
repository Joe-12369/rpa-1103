using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.DesktopInput.Mouse.Params
{
    public class ParamMouseMoveModel : ParamBase
    {
        public int positiontype { get; set; }
        public int x { get; set; }
        public int y { get; set; }
        public int speedValue { get; set; } = 40; //值为 0 随机速度， 其他 固定速度（1-100）
        /// <summary>
        /// 是否使用模拟
        /// 1.是
        /// 0.否
        /// </summary>
        public int isUsePerson { get; set; }
    }
}
