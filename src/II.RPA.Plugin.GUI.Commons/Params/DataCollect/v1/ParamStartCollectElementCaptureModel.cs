using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons.Params
{
    public class ParamStartCollectElementCaptureModel
    {
        /// <summary>
        /// 元素截图路径
        /// </summary>
        public string PackagePath { get; set; } = string.Empty;
        public string PageUrl { get; set; } = string.Empty;
        public string PageTitle { get; set; } = string.Empty;
        public CaptureElementType CaptureElementType { get; set; } = CaptureElementType.DataCollectingElement;

        /// <summary>
        /// 最后一个拾取的选择器，拾取相似元素的时候需要使用
        /// </summary>
        public List<SelectorProps> LastCaptureSelector { get; set; } = new List<SelectorProps>();
        /// <summary>
        /// 是否是首次拾取，默认：否。清空数据之后，也算首次（首次拾取提供tile\url\下一页按钮等信息）
        /// </summary>
        public bool IsFirstCapture { get; set; } = false;

        /// <summary>
        /// 采集类型
        /// 网页: 'web'  软件: 'software' 
        /// </summary>
        [JsonProperty("collectType")]
        public string CollectType { get; set; } = string.Empty;
    }
}
