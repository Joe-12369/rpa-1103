using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamSetTextModel : ParamBase
    {
        public int timeout { get; set; } = 10000;
        public string cvExecutionServiceURL { get; set; } = "";
        public string txt { get; set; }
        /// <summary>
        /// 0 清空输入 1 追加输入
        /// </summary>
        public int isReplace { get; set; } = 0;
        /// <summary>
        /// 0.非模拟输入1.模拟输入 ,2.剪切板输入
        /// </summary>
        public int isSimulate { get; set; } = 0;
        public int interval { get; set; } = 0;
        public int isEnter { get; set; } = 0;// 1 是 0 否
        public int delaybeforEnter { get; set; } = 200;// 等待 时长
        //  Trigger  0 不切换 1 切换 默认1
        public int Trigger { get; set; } = 0;
        public int isVerify { get; set; } = 0;
        public int Retry { get; set; } = 0;
        [JsonProperty("state")]
        public string inputState { get; set; } = string.Empty;

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
        [JsonProperty("cursorPos")]
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

        //  0 不切换 1 强制切换美式键盘
        public int forceSwitchENG { get; set; } = 0;
    }
}
