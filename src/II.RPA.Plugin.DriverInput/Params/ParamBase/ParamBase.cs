using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.DriverInput.Params
{
    public class ParamBase : II.RPA.Plugin.Core.PluginBase.PluginParameterBase
    {
        /// <summary>
        /// 输入模式 1：Ps2 2:HID
        /// </summary>
        public int InputMode = 1;

        /// <summary>
        /// 输入内容(字母数字符号 中文不支持)
        /// </summary>
        public string InputContent = string.Empty;

        /// <summary>
        /// 字符串之间的输入时间间隔
        /// </summary>
        public decimal InputInterval = 50;

        /// <summary>
        /// 键盘按压持续时间
        /// </summary>
        public int KeyDownDuration = 50;
    }
}
