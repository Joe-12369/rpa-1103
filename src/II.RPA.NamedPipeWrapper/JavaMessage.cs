using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.NamedPipeWrapper
{
    public class JavaMessage
    {
        public string action { get; set; }
        public string selector { get; set; }
        public string excuteResult { get; set; }
        public int clickType { get; set; }
        public int offsetX { get; set; }
        public int offsetY { get; set; }
        public string text { get; set; }

        public int issimulate { get; set; } //是否是模拟点击1，是，0，否
        public int selectType { get; set; }
        public string JsonParam { get; set; }//Json类型参数
        public string setvalue { get; set; }
        public int interval { get; set; } = 0;

        public int check { get; set; }

        public string snapFilepath { get; set; }

        public bool findOnce { get; set; }
        public int cursorPos { get; set; } //鼠标所在位置
        public int keyType { get; set; }// 0 单击 1 双击
        public int isReplace { get; set; }//是否连续
        public int positionType { get; set; }

        public bool IsDataColloetion = false;
        public int isVerify { get; set; }
    }
}
