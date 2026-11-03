using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.DesktopInput.Mouse.Params
{
    public class ParamMouseMovementTrackModel : ParamBase
    {
        public int positiontype { get; set; }
        public int x { get; set; }
        public int y { get; set; }
        public int speedValue { get; set; } = 40; //值为 0 随机速度， 其他 固定速度（1-100）
        /// <summary>
        /// 0 模拟人工
        /// 1 匀速直线
        /// 2 瞬间移动
        /// </summary>
        public int moveMode { get; set; } = 0;
    }
}
