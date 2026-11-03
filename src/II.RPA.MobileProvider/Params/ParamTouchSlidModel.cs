using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Params
{
    public class ParamTouchSlidModel : MobileElementValidationOptions
    {
        public string sessionId { get; set; }

        public string selector { get; set; }

        /// <summary>
        /// 滑动区域   ，参见EnumSlidArea
        /// </summary>
        public string slidArea { get; set; }

        public string slidType { get; set; }

        public string slidDir { get; set; }

        public int startX { get; set; }

        public int endX { get; set; }

        public int startY { get; set; }

        public int endY { get; set; }
        /// <summary>
        /// 滑动时间
        /// </summary>
        public int slidTime { get; set; } = 100;
        public double timeout { get; set; }
        public int waitAfterSlidTime { get; set; } = 100;
    }
    /// <summary>
    /// 滑动区域
    /// </summary>
    enum EmSlidArea
    {
        EntireScreen,  //整个屏幕
        TheElement,  //制定元素

    }
    /// <summary>
    /// 滑动方式
    /// </summary>
    public enum EmSlidType
    {
        Direction,
        Location
    }
    enum EmSlidDir
    {
        DownToUp,    //向上
        UpToDown,    //向下
        RightToLeft,   //向左
        LeftToRight    //向右
    }

    public class ParamTouchSlidModel_TestSild
    {
        public string sessionId { get; set; }

        public string slidType { get; set; }


        public int startX { get; set; }

        public int endX { get; set; }

        public int startY { get; set; }

        public int endY { get; set; }
        /// <summary>
        /// 滑动时间
        /// </summary>
        public float slidTime { get; set; } = 100;
        public float waitAfterSlidTime { get; set; } = 100;
    }

    public class ParamTouchSlidModel_V2 : MobileElementValidationOptions
    {
        public string sessionId { get; set; }

        public EmSlidType slidType { get; set; }
        public int startType { get; set; } //0-元素 1-图像 2-坐标

        public int startX { get; set; }

        public int startY { get; set; }

        /// <summary>
        /// 滑动时间
        /// </summary>
        public int slidTime { get; set; } = 100;
        public double timeout { get; set; }


        #region 方向
        public string startSelector { get; set; }
        public ShotInfo startShotInfo { get; set; }
        /// <summary>
        /// 角度方向
        /// </summary>
        public float angular { get; set; }
        /// <summary>
        /// 滑动距离（像素）
        /// </summary>
        public float slidingPixel { get; set; }
        /// <summary>
        /// 长按拖动
        /// </summary>
        public bool pressDrag { get; set; }
        /// <summary>
        /// 持续滑动至指定目标出现
        /// </summary>
        public bool slidWaitEleShow { get; set; }
        /// <summary>
        /// 指定出现目标（类型）0-元素 1-图像
        /// </summary>
        public int targetType { get; set; }
        public string targetSelector { get; set; }
        public ShotInfo targetShotInfo { get; set; }
        public int slidMaxTimes { get; set; }
        #endregion

        #region 坐标
        public List<SlidRect> slidRects { get; set; }
        #endregion
        /// <summary>
        /// 模拟真人滑动速率
        /// </summary>
        public bool simulatedSlidingRate { get; set; }
        /// <summary>
        /// 模拟真人滑动曲率
        /// </summary>
        public bool simulatedSlidingPath { get; set; }

    }
    public class ShotInfo
    {
        public string imgSrc { get; set; }
        public int offsetX { get; set; }
        public int offsetY { get; set; }
        public float thr { get; set; }
        public ScopeRect scopeRect { get; set; }
        public object deviceResolution { get; set; }
    }
    public class SlidRect
    {
        public int startX { get; set; }
        public int startY { get; set; }
        public int endX { get; set; }
        public int endY { get; set; }
        public float slidTime { get; set; }
        public float waitAfterSlidTime { get; set; }
    }
}
