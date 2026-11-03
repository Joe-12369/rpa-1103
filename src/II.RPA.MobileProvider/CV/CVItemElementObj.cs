using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.CV
{
    [Serializable]
    /// <summary>
    /// 识别结果结构体信息
    /// </summary>
    public class CVItemElementObj
    {
        public CVItemElementObj()
        {
            this.locations = new float[4][];
        }
        /// <summary>
        /// 元素文本内容
        /// </summary>
        public string text_contents { get; set; }
        /// <summary>
        /// 元素小图base64编码
        /// </summary>
        public string template_image { get; set; }
        /// <summary>
        /// 元素坐标位置 [[x1, y1],[x2, y2],[x3, y4],[x4, y4]]，（左上、右上、右下、左下）
        /// </summary>
        public float[][] locations { get; set; }
        /// <summary>
        /// 该元素是否唯一
        /// </summary>
        public bool is_unique { get; set; }
        /// <summary>
        /// 元素对应的锚点id
        /// </summary>
        public string anchor { get; set; }
    }
}
