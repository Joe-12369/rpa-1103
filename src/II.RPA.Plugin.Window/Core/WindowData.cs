using Newtonsoft.Json;

namespace II.RPA.Plugin.Window.Core
{
    internal class WindowData
    {
        /// <summary>
        /// 窗口标题
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// 窗口类名
        /// </summary>
        [JsonProperty("class")]
        public string Class { get; set; }

        /// <summary>
        /// 进程ID
        /// </summary>
        [JsonProperty("processId")]
        public int ProcessId { get; set; } = 0;

        /// <summary>
        /// 进程名称
        /// </summary>
        [JsonProperty("processName")]
        public string ProcessName { get; set; }

        /// <summary>
        /// 窗口句柄
        /// </summary>
        [JsonProperty("windowHandle")]
        public string WindowHandle { get; set; }

        /// <summary>
        /// 窗口尺寸
        /// </summary>
        [JsonProperty("windowSize")]
        public string WindowSize { get; set; }

        /// <summary>
        /// 窗口位置
        /// </summary>
        [JsonProperty("windowPoint")]
        public string WindowPoint { get; set; }
    }
}
