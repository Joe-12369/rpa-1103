using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic.Params
{
    //public enum MoveDirection
    //{
    //    /// <summary>
    //    /// 从左到右
    //    /// </summary>
    //    LeftToRight,
    //    /// <summary>
    //    /// 从上到下
    //    /// </summary>
    //    TopToBottom
    //}
    //public enum MoveSpeed
    //{
    //    /// <summary>
    //    /// 快
    //    /// </summary>
    //    Fast,
    //    /// <summary>
    //    /// 中
    //    /// </summary>
    //    Medium,
    //    /// <summary>
    //    /// 慢
    //    /// </summary>
    //    Slow
    //}
    //public enum MoveModel
    //{
    //    /// <summary>
    //    /// 平滑
    //    /// </summary>
    //    Smooth,
    //    /// <summary>
    //    /// 抖动
    //    /// </summary>
    //    Jitter
    //}
    public class ParamGetChartDataModel : ParamModel
    {
        public bool isRemoveWspaceChar { get; set; }
        public bool isPinChartElement { get; set; }
        public string parentElementId { get; set; }
        public int moveDirection { get; set; }
        public int moveSpeed { get; set; }
        public int moveModel { get; set; }
        public float offset { get; set; }

        public float radiusRatio { get; set; }
        public float horOffset { get; set; }
        public float verOffset { get; set; }
    }
}
