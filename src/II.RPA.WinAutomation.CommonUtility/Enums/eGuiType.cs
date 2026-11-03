using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WinAutomation.CommonUtility
{
    public enum eGuiType : int
    {
        None = 400,//SDK中的、不明确任务组件的 异常
        Windows = 401,
        /// <summary>
        /// Java
        /// </summary>
        Java = 402,
        [Description("谷歌浏览器")]
        Chrome = 403,
        [Description("IE浏览器")]
        IE = 404,
        CV = 405,
        SAP = 406,
        [Description("火狐浏览器")]
        FireFox = 407,
        [Description("实在浏览器")]
        ZExplorer = 408,
        Mobile = 409,
        [Description("Edge浏览器")]
        Edge = 410,
        WdtErp = 411,
        [Description("360安全浏览器")]
        Web360se = 412,
        [Description("搜狗浏览器")]
        Sogou = 413,
        Window = 414,
        [Description("360极速浏览器")]
        Web360X = 415,
        [Description("紫鸟浏览器")]
        SuperBrowser = 416,
        /// <summary>
        ///使用Java 无插件模式拾取
        /// </summary>
        Swing = 417,
        QT = 418,
        Chromium = 419,
        ///...
        DataBase = 460,
        CoreServer = 499
    }



}
