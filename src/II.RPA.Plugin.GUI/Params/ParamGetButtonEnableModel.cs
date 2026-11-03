using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamGetButtonEnableModel : ParamBase
    {
        public int timeout { get; set; } = 10000;
        /// <summary>
        /// 是否识别隐藏元素 true-识别隐藏元素，结构存在的都获取 false-只处理结构存在且可见
        /// </summary>
        [JsonProperty("isDiscernHidden")]
        public bool IsDiscernHidden { get; set; } = true;
    }
}
