using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamGetElementInfoModel : ParamBase
    {
        /// <summary>
        /// 属性名称
        /// 获取元素对象 attributeName: selector 
        /// 获取元素坐标，返回元素范围  attributeName: bounding
        /// 获取文本内容，返回元素text 属性值 attributeName: text
        /// 获取元素值，返回元素value 属性值 attributeName: value
        /// 获取元素链接地址，网页元素，返回 herf或src 的值，win32 元素 无此属性，返回null attributeName: value
        /// 获取元素代码，网页元素，元素标签代码，win32 元素 无此属性，返回null
        /// </summary>
        [JsonProperty("attributeName")]
        public string AttributeName { get; set; }
        /// <summary>
        /// 是否补偿herf 或 src
        /// 0 否 1 是
        /// </summary>
        [JsonProperty("isCompensationLink")]
        public int IsCompensationLink { get; set; }
        public string apiVersion { get; set; }
    }
}
