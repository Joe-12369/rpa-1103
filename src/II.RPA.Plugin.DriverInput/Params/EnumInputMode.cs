using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.DriverInput.Params
{
    /// <summary>
    /// 驱动输入类型 
    /// </summary>
    public enum EnumInputMode:int
    {
        /// <summary>
        /// PS2驱动输入 使用winring0 需要管理员权限使用
        /// </summary>
        PS2=0,

        /// <summary>
        /// HID驱动输入 使用HID工具 不需要管理员权限
        /// </summary>
        HID=1
    }
}
