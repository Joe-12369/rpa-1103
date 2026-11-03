using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.DesktopInput.Mouse.Params
{
    public class ParamMouseDragModel: ParamBase
    {
        public int Positiontype { get; set; }
        //0:左键；1：右键；2：中键
        public int Mousetype { get; set; }
        //辅助按键
        public string[] Keys { get; set; }
        public int Startx { get; set; }
        public int Starty { get; set; }
        public int Endx { get; set; }
        public int Endy { get; set; }
        //0匀速、1变速
        //public int SpeedType { get; set; }
        //0 指定位置 1 指定目标
        public int dragType { get; set; }
        //0 随机速度，其他 固定速度(1-100)
        public int speedValue { get; set; }
        //目标元素
        public string selector { get; set; }
    }
}
