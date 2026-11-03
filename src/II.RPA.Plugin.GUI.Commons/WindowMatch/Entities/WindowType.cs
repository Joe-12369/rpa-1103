using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons.WindowMatch
{
    /// <summary>
    /// 窗口类型，枚举项必须与eGuiType同名，会有按名称转换操作
    /// </summary>
    public enum WindowType
    {
        None = 0,
        Windows = 1,
        Chrome = 1 << 1,
        IE = 1 << 2,
        FireFox = 1 << 3,
        Edge = 1 << 4,
        Web360se = 1 << 5,
        Sogou = 1 << 6,
        Web360X = 1 << 7,
        SuperBrowser = 1 << 8,
        ZExplorer = 1 << 9,
        Desktop = 1 << 10,
        Chromium = 1 << 11,
    }

}
