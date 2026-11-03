using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons.Params
{
    public class ParamSetTextModel
    {
        public enum InputEnum
        {
            Backaground,
            Simulation,
            Stick
        }
        public string text { get; set; }
        /// <summary>
        /// 0 后台
        /// 1 模拟
        /// 2 剪切板
        /// </summary>
        public int inputType { get; set; }
        /// <summary>
        /// 是否替换
        /// </summary>
        public int isReplace { get; set; }
        /// <summary>
        /// 是否校验结果
        /// </summary>
        public int isVerify { get; set; }
        /// <summary>
        /// 重新输入
        /// </summary>
        public int Retry { get; set; }
        /// <summary>
        /// 0 不切换 1 切换 
        /// </summary>
        public int Trigger { get; set; }
        /// <summary>
        /// 输入完成后是否回车
        /// </summary>
        public int isEnter { get; set; }
        /// <summary>
        ///  原意:输入间隔
        ///  现意:按键响应时长
        /// </summary>
        public int Interval { get; set; }
        /// <summary>
        /// 回车前延时
        /// </summary>
        public int delaybeforEnter { get; set; } = 200;

        //- label: "中心"
        //value: '0'
        //- label: "左上"
        //value: '1'
        //- label: "右上"
        //value: '2'
        //- label: "左下"
        //value: '3'
        //- label: "右下"
        //value: '4'
        [JsonProperty("elementPosition")]
        public int ElementPosition
        {
            get;
            set;
        } = 0;
        [JsonProperty("offsetX")]
        public int OffsetX
        {
            get;
            set;
        } = 0;
        [JsonProperty("offsetY")]
        public int OffsetY
        {
            get;
            set;
        } = 0;
        /// <summary>
        /// 焦点超时时间 ms
        /// </summary>
        [JsonProperty("focusTimeout")]
        public int FocusTimeout { get; set; } = 1000;
        public int ForceSwitchENG { get; set; } = 0;
    }
}
