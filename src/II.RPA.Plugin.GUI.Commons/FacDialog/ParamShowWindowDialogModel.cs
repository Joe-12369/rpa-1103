using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace II.RPA.Plugin.GUI.Commons.FacDialog
{
    public class ParamShowWindowDialogModel
    {
        /// <summary>
        /// 弹窗标题
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }
        /// <summary>
        /// 弹窗类型 枚举['info', 'success', 'warning', 'error']
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }
        /// <summary>
        /// 弹窗内容
        /// </summary>
        [JsonProperty("content")]
        public string Content { get; set; }
        /// <summary>
        /// 按钮组（依据顺序从左到右）
        /// </summary>
        [JsonProperty("buttons")]
        public List<ButtonsItem> Buttons { get; set; }
    }
    public class ButtonsItem
    {
        /// <summary>
        /// 按钮名称
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; set; }
        /// <summary>
        /// 按钮是否需要加颜色
        /// </summary>
        [JsonProperty("isNormal")]
        public bool IsNormal { get; set; }
        /// <summary>
        /// 对应的事件ID
        /// </summary>
        [JsonProperty("eventId")]
        public string EventId { get; set; }
    }
    
}
