using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.ElementHighlight
{
    /// <summary>
    /// 隐藏高亮框和高亮标签传入的参数
    /// </summary>
    public class HightLightWndHideParam
    {
        /// <summary>
        /// 是否需要完全关闭窗体
        /// </summary>
        public bool IsNeedClose { get; set; } = false;

        /// <summary>
        /// 是否需要将窗体最小化
        /// </summary>
        public bool IsNeedMinimize { get; set; } = false;

        /// <summary>
        /// 是否异步执行
        /// </summary>
        public bool IsAsync { get; set; } = true;

        /// <summary>
        /// 是否彻底关闭（同步关闭一些线程）
        /// </summary>
        public bool IsFinalStop { get; set; } = false;
    }
}
