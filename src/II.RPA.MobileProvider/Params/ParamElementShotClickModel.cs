using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Params
{
    public class ParamElementShotClickModel
    {
        public string sessionId { get; set; }
        public string imgSrc { get; set; }
        ///参见EumClickType
        public int clickType { get; set; }
        public int stayTime { get; set; }
        public int clickTimes { get; set; }
        public int interval { get; set; }
        public int offsetX { get; set; }
        public int offsetY { get; set; }
        public float thr { get; set; }
        public ScopeRect scopeRect { get; set; }
        public bool isRoundClick { get; set; }
        public int moreImageProtype { get; set; }
        public object deviceResolution { get; set; }
    }
    public class ScopeRect
    {
        public int startX { get; set; }
        public int startY { get; set; }
        public int endX { get; set; }
        public int endY { get; set; }
    }
    public enum MoreImageProtype
    {
        /// <summary>
        /// 异常返回
        /// </summary>
        Exception,
        /// <summary>
        /// 返回多个坐标
        /// </summary>
        ReturnMoreRect,
        /// <summary>
        /// 暂停并手动处理
        /// </summary>
        PauseAndManual
    }
}
