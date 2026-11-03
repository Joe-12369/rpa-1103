using II.RPA.Plugin.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamMouseClickModel : ParamBase
    {
        /// <summary>
        /// 0左键 1 右键 2中
        /// </summary>
        public int clickType { get; set; } = 0;
        public int offsetX { get; set; } = 0;
        public int offsetY { get; set; } = 0;
        /// <summary>
        ///  0：后台操作，1：模拟操作；
        /// </summary>
        public int issimulate { get; set; } = 0;
        /// <summary>
        /// /*- label: "中心"
        ///    value: '0'
        ///  - label: "左上"
        ///    value: '1'
        ///  - label: "右上"
        ///    value: '2'
        ///  - label: "左下"
        ///    value: '3'
        ///  - label: "右下"
        ///    value: '4'*/
        /// </summary>
        public int cursorPos { get; set; } = 0;
        /// <summary>
        /// 0 单击 1 双击  2三击 3按下 4弹起
        /// </summary>
        public int keyType { get; set; } = 0;
        public int timeout { get; set; } = 10000;
        public string cvExecutionServiceURL { get; set; } = "";
        public bool IsEnableResultVerify
        {
            get;
            set;
        }
        /// <summary>
        /// 是否使用模拟
        /// 1 是
        /// 0 否
        /// </summary>
        public int isUsePerson { get; set; }
        /// <summary>
        /// 辅助按键
        /// </summary>
        [JsonProperty("aidKeyType")]
        public AidKeyType AidKeyType { get; set; } = AidKeyType.None;
    }

}
