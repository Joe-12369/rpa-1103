using II.RPA.Plugin.Core;
using Newtonsoft.Json;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamDataCollectModel : ParamBase
    {
        /// <summary>
        /// 数据采集选择器  RetDataCollectConfigurationModel JSON
        /// </summary>
        public string Selector { get; set; }
        /// <summary>
        /// 浏览器对象信息，包含浏览器类型
        /// </summary>
        public string WebData { get; set; }
        /// <summary>
        /// 翻页后采集需要延迟的时间
        /// </summary>
        public int Delay { get; set; } = 1000;
        /// <summary>
        ///翻页采集之前需求准直滚动的距离
        /// </summary>
        public int ScrollVertical { get; set; }
        /// <summary>
        /// 页面垂直滚动屏数,0无滚动
        /// </summary>
        public int ScrollScreenNum { get; set; } = 0;
        /// <summary>
        /// 静态文件存储位置
        /// </summary>
        public string DataFilePath { get; set; }
        /// <summary>
        /// 0:所有页，1当前页，n 具体页数
        /// </summary>
        public int PageCount { get; set; } = 1;
        /// <summary>
        /// 0:所有页，1当前页，n 具体页数
        /// </summary>
        public TurnPageConfigType TurnPageConfigType { get; set; } = TurnPageConfigType.CurrentPage;
        public int ScrollDelay { get; set; } = 200;

        /// <summary>
        /// 采集类型
        /// 网页: 'web'  软件: 'software' 
        /// </summary>
        [JsonProperty("collectType")]
        public string CollectType { get; set; } = string.Empty;
    }
}
