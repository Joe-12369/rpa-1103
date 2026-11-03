using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.CV
{
    [Serializable]
    public class CVItem
    {
        [JsonIgnore]
        public float[] left_top
        {
            get
            {
                return this.result_value.locations[0];
            }
        }
        [JsonIgnore]
        public float[] right_top
        {
            get
            {
                return this.result_value.locations[1];
            }
        }
        [JsonIgnore]
        public float[] right_bottom
        {
            get
            {
                return this.result_value.locations[2];
            }
        }
        [JsonIgnore]
        public float[] left_bottom
        {
            get
            {
                return this.result_value.locations[3];
            }
        }
        [JsonIgnore]
        public string content { get; set; }
        /// <summary>
        /// 此属性[6.7.1版本之后可用]，Zbrain 返回的元素区域信息，通过区域的四点坐标获得
        /// </summary>
        [JsonIgnore]
        public Rectangle Area { get; set; }
        /// <summary>
        /// 此属性[6.8.0 chatrpa版本之后可用]，Zbrain 返回的元素区域信息【未经过转换，zbrain原信息】，通过区域的四点坐标获得
        /// </summary>
        [JsonIgnore]
        public Rectangle ZbrainArea { get; set; }
        /// <summary>
        /// 此属性[6.8.0 chatrpa版本之后可用]，
        /// </summary>
        public string uniqueId { get; set; }
        /// <summary>
        /// 此属性[6.8.0 chatrpa版本之后可用]，Zbrain 返回的该元素对应的步骤
        /// </summary>
        public string step { get; set; }
        /// <summary>
        /// 此属性[6.8.0 chatrpa版本之后可用]，True来自多模态，Flase 来自石昊
        /// </summary>
        public string from_mml { get; set; }
        /// <summary>
        /// 判断文字/非文字信息
        /// </summary>
        public string algorithm { get; set; }
        /// <summary>
        /// 解析结果唯一标识
        /// </summary>
        public string result_id { get; set; }
        /// <summary>
        /// 识别结果结构体
        /// </summary>
        public CVItemElementObj result_value { get; set; }
        public int width { get; set; }
        public int height { get; set; }

        /// <summary>
        /// 扩展字段
        /// </summary>
        public string extend { get; set; }

    }

    [Serializable]
    /// <summary>
    /// 识别结果结构体信息
    /// </summary>
    public class CVItemElementObj
    {
        public CVItemElementObj()
        {
            this.locations = new float[4][];
            this.semantic_info = new SemanticInfoModel();
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

        public SemanticInfoModel semantic_info { get; set; }
    }

    public class OperationMatchingByCVService_Result
    {
        public OperationMatchingByCVService_Result()
        {
            this.result = new int[4][];
        }
        /// <summary>
        /// 重新计算后的元素坐标信息（四点） [[x1, y1],[x2, y2],[x3, y4],[x4, y4]]，（左上、右上、右下、左下）
        /// </summary>
        public int[][] result { get; set; }
    }
    public class SemanticInfoModel
    {
        //元素类别，字符串
        public string cls { get; set; } = string.Empty;
        //#元素语义信息，若无则为空列表[]
        public object semantic { get; set; } = null;
        public object anchor { get; set; } = null;

    }

}
