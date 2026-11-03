using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamDataCollectConfigurationModel : ParamBase
    {
        /// <summary>
        /// 数据采集选择器  RetDataCollectConfigurationModel JSON
        /// </summary>
        public string Selector { get; set; } = string.Empty;
        /// <summary>
        /// 全局变量JSON
        /// </summary>
        public string GlobalVariable { get; set; } = string.Empty;
        /// <summary>
        /// 浏览器对象信息，包含浏览器类型
        /// </summary>
        public string WebData { get; set; } = null;
        /// <summary>
        /// 流程项目目录
        /// </summary>
        public string PackagePath { get; set; } = string.Empty;
        /// <summary>
        /// 采集类型
        /// 网页: 'web'  软件: 'software' 
        /// </summary>
        [JsonProperty("collectType")]
        public string CollectType { get; set; } = string.Empty;
    }


    public class DataBuriedModel_DataCollect
    {
        /// <summary>
        /// 采集页面url 数据采集页面的url和窗口软件名称；用来分析采集较多的网站和软件是那些，可针对性优化。
        /// </summary>
        public string collect_page_url { get; set; } = string.Empty;

        /// <summary>
        /// 开始时间 （前端处理） 记录点击提示窗口上“采集网页数据”、“采集软件数据”或组件属性中“配置采集规则”按钮的开始时间
        /// <summary>
        public string start_time { get; set; } = string.Empty;

        /// <summary>
        /// 采集页  1、所有页;2、当前页；3、指定页数；
        /// <summary>
        public PageSearchType collect_page_mode { get; set; } = PageSearchType.CurrentPage;

        /// <summary>
        /// 翻页元素  记录翻页元素是否是默认元素。 0为默认 1为自定义 2为未定义
        /// <summary>
        public int page_element_value { get; set; } = 2;

        /// <summary>
        /// 点击配置项来源 (前端处理) 1、弹框开始数据采集； 2、组件编辑区配置采集项（二次配置）、 3、组件属性中配置采集项（二次配置）
        /// <summary>
        public string source { get; set; } = string.Empty;

        /// <summary>
        /// 清空配置
        /// <summary>
        public int clear_connect { get; set; }

        /// <summary>
        ///  刷新列表
        /// <summary>
        public int refresh_connect { get; set; }

        /// <summary>
        ///  预览表设置 对应列表10种类型，每一种都上报。
        /// <summary>
        public string setting_mode { get; set; } = string.Empty;

        /// <summary>
        ///  帮助说明  记录帮助说明点击次数
        /// <summary>
        public int help_connect { get; set; }

        /// <summary>
        /// 采集类型 记录采集的是网页数据还是软件数据。
        /// <summary>
        public string acquisition_type { get; set; } = string.Empty;

        /// <summary>
        /// 采集软件 记录采集软件，比如 Chrome、千牛等。
        /// <summary>
        [JsonIgnore]
        public string acquisition_software { get; set; } = string.Empty;

        /// <summary>
        /// 是否成功 是否保存，是:1，否:0
        /// <summary>
        public int is_success { get; set; } = 0;
    }

    public enum PageSearchType : int
    {
        /// <summary>
        /// 所有页
        /// </summary>
        AllPage = 0,
        /// <summary>
        /// 当前页
        /// </summary>
        CurrentPage = 1,
        /// <summary>
        /// 指定页数
        /// </summary>
        CustomizedPage = 2
    }


}
