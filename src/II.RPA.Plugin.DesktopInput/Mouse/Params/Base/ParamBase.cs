using Newtonsoft.Json;

namespace II.RPA.Plugin.DesktopInput.Mouse.Params
{
    public class ParamBase : II.RPA.Plugin.Core.PluginBase.PluginParameterBase
    {
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
    }
}
