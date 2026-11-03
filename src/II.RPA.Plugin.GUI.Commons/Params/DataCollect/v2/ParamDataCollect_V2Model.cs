using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace II.RPA.Plugin.GUI.Commons.Params
{
    public enum DeduplicateType
    {
        None = 0, Deduplicate = 1
    }
    public enum HighlightType
    {
        None = 0, Highlight = 1
    }
    public class ParamDataCollect_V2Model : II.RPA.Plugin.Core.PluginBase.PluginParameterBase
    {
        /// <summary>
        /// 
        /// </summary>
        public string RequestId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("dataCollectConfig")]
        public RetDataCollectConfigurationModel_v2 DataCollectConfig { get; set; }
        /// <summary>
        /// 颜色引导配置
        /// </summary>
        [JsonProperty("colorGuidanceConfig")]
        public ColorGuidanceConfig ColorGuidanceConfig { get; set; } = null;
        /// <summary>
        /// 采集数据静态文件村范围值
        /// </summary>
        public string DataFilePath { get; set; }
        /// <summary>
        /// 采集数量
        /// </summary>
        public int CollectDataCount { get; set; }
        /// <summary>
        /// 0:所有页，1当前页，n 具体页数
        /// </summary>
        public int TurnPageCount { get; set; }
        /// <summary>
        /// 0:不去除重复数据，1去除重复数据
        /// </summary>
        public DeduplicateType DeduplicateType { get; set; }

        /// <summary>
        /// 将要执行的元素类型
        /// </summary>
        [JsonIgnore]
        public string GuiType { get; set; }
        [JsonIgnore]
        public bool IsTable
        {
            get
            {
                return this.DataCollectConfig.CollectionConfig.TableSelector?.Count > 0;
            }
        }
        [JsonIgnore]
        public bool IsPreview => ColorGuidanceConfig?.ActionCmd == "preview";
        public bool IsColorGuidance => !string.IsNullOrEmpty(ColorGuidanceConfig?.ActionCmd);
        /// <summary>
        /// 定位数据，网页对象、或窗口对象
        /// </summary>
        [JsonProperty("locateData")]
        public JObject LocateData { get; set; }
        /// </summary>
        [JsonProperty("isRandomLocationClick")]
        public bool IsRandomLocationClick { get; set; }

        [JsonIgnore]
        public bool IsWeb => DataCollectConfig.PageInfo.WindowType == "web";
        /// <summary>
        /// 是否有翻页按钮 配置
        /// </summary>
        [JsonIgnore]

        public bool HasTurningPageBtn => !(string.IsNullOrEmpty(DataCollectConfig.TurningPageConfig?.Selector) || DataCollectConfig.TurningPageConfig?.Selector == "{}");

        [JsonIgnore]
        public WindowObject WindowInfo { get; set; }

        [JsonIgnore]
        public IGuiNode MarkedElement { get; set; }
        

    }
    public class ColorGuidanceConfig
    {
        /// <summary>
        /// 着色命令 preview | set | clear
        /// </summary>
        [JsonProperty("actionCmd")]
        public string ActionCmd { get; set; } = string.Empty;
        [JsonProperty("rows")]
        public List<int> rows { get; set; } = new List<int>();
        [JsonProperty("columns")]
        public List<int> columns { get; set; } = new List<int>();
        [JsonProperty("highlight")]
        public HighlightType Highlight { get; set; } = HighlightType.Highlight;

        [Obsolete, JsonProperty("rowIndex")]
        public int RowIndex { get; set; } = -1;
        [Obsolete, JsonProperty("columnIndex")]
        public int ColumnIndex { get; set; } = -1;

        [JsonProperty("invalidRowIdxs")]
        public List<int> InvalidRowIdxs { get; set; } = new List<int>();
    }
}
