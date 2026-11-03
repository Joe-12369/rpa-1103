using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons.WindowMatch.Entities
{
    public enum MatchConfigType
    {
        /// <summary>
        /// 普通
        /// </summary>
        Normal,
        /// <summary>
        /// 应用内嵌IE，非已支持浏览器
        /// </summary>
        AppEmbeddedIE,
        Custom
    }
    public class MatchConfig
    {
        private string key = string.Empty;
        [JsonProperty("windowType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public WindowType WindowType { get; set; } = WindowType.None;

        //普通配置 组定义配置，普通配置执行顺序 按照：ClassNames>TitleFeatures>ProcessFeatures>IEClassNames
        [JsonProperty("configType", ItemConverterType = typeof(StringEnumConverter))]
        public MatchConfigType ConfigType { get; set; } = MatchConfigType.Normal;
        [JsonProperty("browserWndClassNames")]
        public string[] BrowserWndClassNames { get; set; }
        [JsonProperty("titleFeatures")]
        public string[] TitleFeatures { get; set; }
        [JsonProperty("processFeatures")]
        public string[] ProcessFeatures { get; set; }
        [JsonProperty("renderClassNames")]
        public string[] RenderClassNames { get; set; }
        [JsonProperty("IERenderClassNames")]
        public string[] IERenderClassNames { get; set; }
        [JsonProperty("excludeProcess")]
        public List<string> ExcludeProcess { get; set; } = new List<string>();
        [JsonProperty("appExePath")]
        public string AppExePath { get; set; }
        [JsonProperty("productName")]
        public string ProductName { get; set; }
        [JsonProperty("lableName")]
        public string LableName { get; set; }

        [JsonIgnore]
        public string ConfigKey
        {
            get
            {
                if (string.IsNullOrEmpty(key))
                {

                    key = this.ToJsonNoneFormating();
                }
                return key;
            }
        }
    }
}
