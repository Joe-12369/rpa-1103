using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace II.RPA.Plugin.GUI.Commons.Params
{
    /// <summary>
    /// 元素查找入参类
    /// </summary>
    public class ParamCollectDataBySelectorModel
    {
        public string WedData { get; set; } = null;
        /// <summary>
        /// selector 版本号
        /// </summary>
        public string NextPageBtnSelectorVersion { get; set; } = string.Empty;
        public List<SelectorProps> NextPageBtnSelector { get; set; } = new List<SelectorProps>();
        /// <summary>
        /// 延迟拾取，毫秒
        /// </summary>
        public int Delay { get; set; } = 1000;
        /// <summary>
        /// 页面垂直滚动距离，px,0无滚动
        /// </summary>
        public int ScrollVertical { get; set; } = 0;
        /// <summary>
        /// 0:所有页，1当前页，n 具体页数
        /// </summary>
        public int PageCount { get; set; } = 1;
        /// <summary>
        /// 0:所有页，1当前页，n 具体页数
        /// </summary>
        public TurnPageConfigType TurnPageConfigType { get; set; } = TurnPageConfigType.CurrentPage;
        /// <summary>
        /// 静态文件存储位置
        /// </summary>
        public string DataFilePath { get; set; }
        public bool IsTable { get; set; } = false;
        public List<SelectorProps> SameParentsProps { get; set; } = new List<SelectorProps>();

        public List<CollectData_SelectorData> SelectorDataList { get; set; } = new List<CollectData_SelectorData>();
        public int ScrollDelay { get; set; } = 200;
    }
    public class CollectData_SelectorData
    {
        private static ILogger logger = Logger.OfContext();

        /// <summary>
        /// 列的标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 列名集合 专门针对普通表格
        /// </summary>
        public List<ColNameStruct> CommonTalbleTitleList = new List<ColNameStruct>();
        /// <summary>
        /// selector 版本号
        /// </summary>
        public string Version { get; set; } = string.Empty;
        /// <summary>
        /// 可直接使用的选择器
        /// </summary>
        public List<SelectorProps> Selector { get; set; } = new List<SelectorProps>();
        /// <summary>
        /// 采集类型
        /// </summary>
        public CollectType CollectType { get; set; } = CollectType.Content;
        //采集类型 对应的属性名，数据来源于CollectType
        public string CollectAttrName
        {
            get
            {
                try
                {
                    string collectTypeStr = CollectType.ToString();
                    FieldInfo fieldInfo = CollectType.GetType().GetField(collectTypeStr);
                    CollectAttribute reulstAttrName = fieldInfo.GetCustomAttributes(typeof(CollectAttribute), false).FirstOrDefault() as CollectAttribute;
                    if (reulstAttrName != null)
                    {
                        return reulstAttrName.AttributeName;
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "获取数据采集类型对应的属性名异常");
                }
                return string.Empty;
            }
        }
        /// <summary>
        /// 正则默认：null
        /// </summary>
        public string Regular { get; set; } = string.Empty;

        /// <summary>
        /// 是否已经进行文本编译（变量编译），默认 否
        /// </summary>
        //public bool IsCompiled { get; set; } = false;
    }


}

