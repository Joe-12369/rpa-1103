using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamIsElementExistModel : ParamBase
    {
        public bool isHighlighting { get; set; } = false;
        public string cvExecutionServiceURL { get; set; } = "";
        public int timeout { get; set; } = 10000;
        /// <summary>
        /// 等待元素组件的是否图像出现依据属性 0-图像出现
        /// </summary>
        public int choose { get; set; } = 1;
        /// <summary>
        /// 是否识别隐藏元素 true-识别隐藏元素，结构存在的都获取 false-只处理结构存在且可见
        /// </summary>
        [JsonProperty("isDiscernHidden")]
        public bool IsDiscernHidden { get; set; } = true;
    }
}
