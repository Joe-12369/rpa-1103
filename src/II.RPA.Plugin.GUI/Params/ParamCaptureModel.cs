using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using System;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamCaptureModel : ParamBase
    {
        public string filepath { get; set; }
        public bool isCVCapture { get; set; }
        public string cvServer { get; set; }
        public int InspectStatus { get; set; }
        public int IsCvSupport { get; set; } = 0;
        public CaptureModeType CaptureMode { get; set; } = CaptureModeType.Ordinary;
        /// <summary>
        /// 原来通用配置信息，主要是原selector的通用配置
        /// </summary>
        [JsonProperty("commonProps")]
        public CommonProps CommonProps { get; set; } = null;
        public int IsAutoSimilar { get; set; } = 0;
    }

    public class CaptureOperationDataModel
    {

        /// <summary>
        /// 返回拾取模式
        /// </summary>
        public string pick_mode { get; set; } = string.Empty;
        /// <summary>
        /// 开始时间
        /// <summary>
        public string start_time { get; set; } = string.Empty;
        /// <summary>
        /// 元素软件名称
        /// <summary>
        public string app_name { get; set; } = string.Empty;
        /// <summary>
        /// 元素类型
        /// <summary>
        public string element_type { get; set; } = string.Empty;
        /// <summary>
        /// F4按键刷新次数
        /// <summary>
        public int f4_key_number { get; set; }
        /// <summary>
        ///  F2按键切换次数
        /// <summary>
        public int f2_key_number { get; set; }
        /// <summary>
        ///  是否成功
        /// <summary>
        public int is_success { get; set; }
        /// <summary>
        ///   来源
        /// <summary>
        public string source { get; set; } = string.Empty;
        /// <summary>
        /// 触发条件
        /// <summary>
        public string trigger_condition { get; set; } = string.Empty;
    }
    [Flags]
    public enum CaptureModeType
    {
        /// <summary>
        /// 智能拾取 模式
        /// </summary>
        Intelligence = 0,
        /// <summary>
        /// 普通拾取 模式
        /// </summary>
        Ordinary = 1,
        /// <summary>
        /// 浮窗 模式
        /// </summary>
        FloatWnd = 2,
        /// <summary>
        /// 老CV
        /// </summary>
        CV = 3
    }
    public enum ElementType
    {
        None = 0,
        /// <summary>
        /// 普通元素（DOM)
        /// </summary>
        Ordinary = 1,
        /// <summary>
        /// 智能元素（CV）
        /// </summary>
        Intelligence = 2
    }
}
