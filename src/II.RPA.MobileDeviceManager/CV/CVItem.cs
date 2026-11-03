using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileDeviceManager.CV
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
    }
}
