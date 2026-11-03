using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Plugin.DriverInput.Params;

namespace II.RPA.Plugin.DriverInput.Params
{
    public class ParamDriverInputModel : ParamBase
    {
        /// <summary>
        /// 是否是追加输入 0追加输入（不处理）; 1为替换输入（只有HID实现）
        /// </summary>
        public int IsAppend = 0;

        public string CompositeName = "";

        public bool useAikeyboard = false;
    }
}
