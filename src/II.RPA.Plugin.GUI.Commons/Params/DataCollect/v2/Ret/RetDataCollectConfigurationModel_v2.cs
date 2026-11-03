using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons.Params.DataCollect
{
    /// <summary>
    /// 数据采集最终返回结果类
    /// </summary>
    public class RetDataCollectConfigurationModel_v2
    {
        /// <summary>
        /// 翻页配置
        /// </summary>
        [JsonProperty("turningPageConfig")]
        public TurningPageConfig TurningPageConfig { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("pageInfo")]
        public PageInfo PageInfo { get; set; }
        /// <summary>
        /// 采集配置
        /// </summary>
        [JsonProperty("collectionConfig")]
        public CollectionConfig CollectionConfig { get; set; } = new CollectionConfig();

    }

    public enum TurningBtnType
    {
        None = 0,
        /// <summary>
        /// 翻页按钮
        /// </summary>
        TurningPageBtn = 1,
        /// <summary>
        /// 加载更多按钮
        /// </summary>
        LoadMoreBtn = 2,
    }
    public enum ScrollType
    {
        //0快速滚动到底部 、1无滚动效果、2缓慢滚动到底部、3自动滚动到底部。老组件无此属性默认0

        /// <summary>
        /// 快速滚动到底部（默认值）
        /// </summary>
        FastScrollToBottom = 0,
        /// <summary>
        /// 无滚动效果
        /// </summary>
        NoScroll = 1,
        /// <summary>
        /// 缓慢滚动到底部
        /// </summary>
        SlowScrollToBottom = 2,
        /// <summary>
        /// 自动滚动到底部
        /// </summary>
        AutoScrollToBottom = 3
    }

    public class TurningPageConfig
    {

        /// <summary>
        /// 
        /// </summary>
        public TurningBtnType BtnType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("selector")]
        public string Selector { get; set; }
        /// <summary>
        /// 模拟点击 0否 后台点击，1是 模拟点击
        /// </summary>
        public int SimulateClick { get; set; } = 1;
    }

    public class PageInfo
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("windowTitle")]
        public string WindowTitle { get; set; } = string.Empty;
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("pageUrl")]
        public string PageUrl { get; set; } = string.Empty;
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("windowType")]
        public string WindowType { get; set; } = string.Empty;
        /// <summary>
        /// 
        /// </summary>       
        [JsonProperty("processName")]
        public string ProcessName { get; set; } = string.Empty;

        [JsonProperty("extendedTags")]
        public ExtendedTags ExtendedTags { get; set; } = ExtendedTags.None;
    }

    public class FilterConfig
    {
        /// <summary>
        /// 
        /// </summary>       
        [JsonProperty("filterType")]
        public DataFilterType FilterType { get; set; }
        /// <summary>
        /// 
        /// </summary>     
        [JsonProperty("pattern")]
        public PatternType Pattern { get; set; }
        /// <summary>
        /// 
        /// </summary>   
        [JsonProperty("parameters")]
        public List<string> Parameters { get; set; }
        /// <summary>
        /// 
        /// </summary>   
        [JsonProperty("parameterObjects")]
        public List<ParameterObject> ParameterObjects { get; set; } = new List<ParameterObject>();
        /// <summary>
        /// 
        /// </summary>     
        [JsonProperty("logical")]
        public DataFilterLogicalType Logical { get; set; }
    }
    public class ParameterObject
    {
        [JsonProperty("val")]
        public string Val { get; set; }
    }
    public class ProcessingConfig
    {
        [JsonProperty("processType")]
        public ProcessingType ProcessType { get; set; }
        [JsonProperty("isEnable")]
        public int IsEnable { get; set; }
        [JsonIgnore]
        public bool Enabled => IsEnable == 1;//1启用，0不启用
        /// <summary>
        /// 
        /// </summary>     
        [JsonProperty("parameters")]
        public object[] Parameters { get; set; }
        /// <summary>
        /// 
        /// </summary>   
        [JsonProperty("parameterObjects")]
        public List<ParameterObject> ParameterObjects { get; set; } = new List<ParameterObject>();

        /// <summary>
        /// 是否完成参数数据转换（将参数数据转换为C#可用格式，按需使用默认false）
        /// </summary>
        [JsonIgnore]
        public bool IsConversion { get; set; } = false;
    }

    public class ColumnConfig
    {
        public ColumnConfig()
        {

        }
        /// <summary>
        /// 标题
        /// </summary>     
        [JsonProperty("columnHeader")]
        public string ColumnHeader { get; set; } = string.Empty;
        [JsonProperty("firstRowTitle")]
        public int FirstRowTitle { get; set; } = 0;
        [JsonIgnore]
        public bool IsFirstRowTitle => FirstRowTitle == 1;
        [JsonProperty("columnHeader_bk")]
        public string ColumnHeaderBk { get; set; } = string.Empty;
        /// <summary>
        /// 
        /// </summary>     
        [JsonProperty("dateType")]
        public string DateType { get; set; } = string.Empty;
        /// <summary>
        /// 
        /// </summary>    
        [JsonProperty("defaultColumnIndex")]
        public int DefaultColumnIndex { get; set; }
        ///// <summary>
        ///// 
        ///// </summary>    
        //[JsonProperty("columnIndex")]
        //public int ColumnIndex { get; set; }
        /// <summary>
        /// 
        /// </summary>     
        [JsonProperty("columnAttributes")]
        public List<string> ColumnAttributes { get; set; } = new List<string>();
        /// <summary>
        /// 
        /// </summary>    
        [JsonProperty("dataAttrName")]
        public string DataAttrName { get; set; } = "innerText";
        /// <summary>
        /// 
        /// </summary>     
        [JsonProperty("filterConfig")]
        public List<FilterConfig> FilterConfig { get; set; } = new List<FilterConfig> { };
        /// <summary>
        /// 
        /// </summary>  
        [JsonProperty("dataProcessConfig")]
        public List<ProcessingConfig> DataProcessConfig { get; set; } = new List<ProcessingConfig> { };
    }

    public class CollectionConfig
    {
        public CollectionConfig()
        {
            this.TableSelector = new List<SelectorProps>();
            this.ColumnSelectorArray = new List<List<SelectorProps>>();
            this.ColumnConfigArray = new List<ColumnConfig>();
            this.MergeConfigMap = new Dictionary<string, MergeConfig>();
            this.CommonSelector = new List<SelectorProps>();
        }
        /// <summary>
        /// 
        /// </summary>    
        [JsonProperty("tableSelector")]
        public List<SelectorProps> TableSelector { get; set; }
        /// <summary>
        /// 
        /// </summary>   
        [JsonProperty("columnSelectorArray")]
        public List<List<SelectorProps>> ColumnSelectorArray { get; set; }
        /// <summary>
        /// 
        /// </summary>   
        [JsonProperty("columnConfigArray")]
        public List<ColumnConfig> ColumnConfigArray { get; set; }

        [JsonProperty("mergeConfigMap")]
        public Dictionary<string, MergeConfig> MergeConfigMap { get; set; }
        [JsonProperty("commonSelector")]
        public List<SelectorProps> CommonSelector { get; set; }
        /// <summary>
        /// 垂直滚动后的延迟时间
        /// </summary>
        [JsonProperty("scrollVerticalDelay")]
        public int ScrollVerticalDelay { get; set; } = -1;//新元素等待时间
        /// <summary>
        /// 垂直滚动后的延迟时间
        /// </summary>
        [JsonProperty("scrollMaxTotalDelay")]
        public int ScrollMaxTotalDelay { get; set; }
        /// <summary>
        /// 数据版本号，与selector版本保持一致
        /// </summary>
        [JsonProperty("selectorVersion")]
        public string SelectorVersion { get; set; }
        /// <summary>
        /// 数据版本号，与selector版本保持一致
        /// </summary>
        [JsonProperty("scrollType")]
        public ScrollType ScrollType { get; set; }
    }

    public class MergeConfig
    {
        public MergeConfig()
        {

        }
        /// <summary>
        /// 标题
        /// </summary>     
        [JsonProperty("mergeColumnHeader")]
        public string MergeColumnHeader { get; set; } = string.Empty;

        /// <summary>
        /// 标题
        /// </summary>     
        [JsonProperty("mergeColumnIndexs")]
        public int[] MergeColumnIndexs { get; set; } = new int[0];
        /// <summary>
        /// 
        /// </summary>     
        [JsonProperty("filterConfig")]
        public List<FilterConfig> FilterConfig { get; set; } = new List<FilterConfig> { };
        /// <summary>
        /// 
        /// </summary>  
        [JsonProperty("dataProcessConfig")]
        public List<ProcessingConfig> DataProcessConfig { get; set; } = new List<ProcessingConfig> { };
    }

}
