using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.Plugin.GUI.Commons.WindowMatch.Entities;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamBase : PluginParameterBase
    {
        [JsonProperty("selector")]
        public string Selector { get; set; } = string.Empty;

        public int processId { get; set; } = default;
        /// <summary>
        /// 高级选择器过滤参数 str
        /// </summary>
        public string Filter { get; set; } = string.Empty;
        /// <summary>
        /// 浏览器类型
        /// </summary>
        [JsonProperty("browserType")]
        public string BrowserType { get; set; } = string.Empty;
        /// <summary>
        /// 运行唯一标识符，【CoreServer调用的Run方法 执行的生命周期内】，标识符应该只有一个
        /// </summary>
        [JsonProperty("runUniqueID")]
        public string RunUniqueID { get; set; } = string.Empty;
        public string CodeVersion { get; set; } = string.Empty;
        public ElementMatchType MatchType { get; set; } = ElementMatchType.Normal;
        /// <summary>
        /// Engien\CoreServer 等传递过来的重试相关的参数
        /// </summary>
        /// <summary>
        /// 定位数据，网页对象、或窗口对象
        /// </summary>
        [JsonProperty("locateData")]
        public JObject LocateData { get; set; }
        /// <summary>
        /// 是否随机移动鼠标（模拟人工）
        /// </summary>
        [JsonProperty("isRandomMouseMove")]
        public bool IsRandomMouseMove { get; set; }
        /// <summary>
        /// 是否随机位置点击元素（模拟人工）
        /// </summary>
        [JsonProperty("isRandomLocationClick")]
        public bool IsRandomLocationClick { get; set; }

        [JsonIgnore]
        public WindowObject WindowInfo { get; set; }
        /// <summary>
        /// selector 中携带的version信息
        /// </summary>
        [JsonIgnore]
        public string SelectorVersion { get; set; } = string.Empty;
        /// <summary>
        /// selector 中携带的processName信息
        /// </summary>
        [JsonIgnore]
        public string SelectorProcessName { get; set; } = string.Empty;
        /// <summary>
        /// selector 中携带的GuiType信息
        /// </summary>
        [JsonIgnore]
        public string SelectorGuiType { get; set; } = string.Empty;
        /// <summary>
        /// selector 中携带的ExtendedTags信息
        /// </summary>
        [JsonIgnore]
        public ExtendedTags ExtendedTags { get; set; } = ExtendedTags.None;

        [JsonIgnore]
        public bool DiscernHidden { get; set; } = false;
        /// <summary>
        /// 真正使用的 SelectorType
        /// </summary>
        [JsonIgnore]
        public string SelectorType
        {
            get
            {
                if (!IsWebSelectorType || WindowInfo == null || WindowInfo.WindowType == Commons.WindowMatch.WindowType.None)
                    return SelectorGuiType;
                if (WindowInfo.WindowType == (Commons.WindowMatch.WindowType.IE | Commons.WindowMatch.WindowType.Windows))
                {
                    return Commons.WindowMatch.WindowType.IE.ToString();
                }
                return WindowInfo.WindowType.ToString();
            }
        }
        [JsonIgnore]
        public bool IsWebSelectorType => GuiElement.selectorType_Web.Contains(this.SelectorGuiType, StringComparer.OrdinalIgnoreCase);


        /// <summary>
        /// 校验类型-元素结构||AI增强  selector||aiEnhanced
        /// </summary>
        [JsonProperty("verifyMode")]
        public string VerifyMode { get; set; } = string.Empty;
    }

}
