using Newtonsoft.Json;

namespace II.RPA.Plugin.Window.Params
{
    public class ParamBase : II.RPA.Plugin.Core.PluginBase.PluginParameterBase
    {
        public string WindowData { get; set; }
        public string Title { get; set; }
        public string Selecter { get; set; } //元素结构
        public string SwitchType { get; set; } = ""; //1、元素-selecter 2、标题-title  3、激活-active 4、桌面-desktop 5、窗口对象-window

        [JsonProperty("keywords")]
        public string[] Keywords { get; set; }
        [JsonProperty("matchType")]
        public GetWindowDataList_MatchType MatchType { get; set; } = GetWindowDataList_MatchType.Contains;
    }
}
