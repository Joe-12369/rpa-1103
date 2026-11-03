using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.WebBrowser.Params
{
    public class ParamScrollModel : ParamBase
    {
        public string value { get; set; }
        /// <summary>
        /// 是否网页中元素内滚动条
        /// </summary>
        public bool isElementOfScroll { get; set; }
        public string selector { get; set; }
        /// <summary>
        /// 滚动类型
        /// </summary>
        public DirectionType directionType { get; set; }
        public string targetSelector { get; set; }
        public int offsetX { get; set; }
        public int offsetY { get; set; }
        public ScrollType scrollType { get; set; }
    }
    public enum DirectionType
    {
        /// <summary>
        /// 横向
        /// </summary>
        Horizontal,
        /// <summary>
        /// 纵向
        /// </summary>
        Vertical
    }
    public enum ScrollType
    {
        /// <summary>
        /// 底部
        /// </summary>
        Bottom,
        /// <summary>
        /// 顶部
        /// </summary>
        Top,
        /// <summary>
        /// 最左侧
        /// </summary>
        Left,
        /// <summary>
        /// 最右侧
        /// </summary>
        Right,
        /// <summary>
        /// 滚动一屏
        /// </summary>
        OneScreen,
        /// <summary>
        /// 指定位置
        /// </summary>
        CusPosition,
        /// <summary>
        /// 指定元素
        /// </summary>
        CusElement
    }
}
