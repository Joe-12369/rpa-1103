using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ChatSelector
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("chatRpaSpeedParam")]
        public ParamChatRpaSpeedModel paramChatRpaSpeedModel { get; set; }
    }
    public class ParamChatRpaSpeedModel
    {
        public string guiType { get; set; } = string.Empty;

        public string windowType { get; set; } = string.Empty;

        //public string webData { get; set; }
        public string winData { get; set; } = string.Empty;

        public string TagNames { get; set; } = string.Empty;

        public string AttrKeywords { get; set; } = string.Empty;

        public string clsNames { get; set; } = string.Empty;
        /// <summary>
        /// normal  cvToNormal    webCombineCv
        /// </summary>
        public string sourceType { get; set; } = string.Empty;

        public bool async = false;
        public string asyncKey = string.Empty;
        public string prevCVSelector = string.Empty;
        [JsonProperty("special_operation")]
        public string SpecialOperation { get; set; } = string.Empty;
        [JsonProperty("task_info")]

        public object TaskInfo { get; set; }
        [JsonProperty("memoryInfo")]
        public Object MemoryInfo { get; set; }

        [JsonIgnore]
        internal IntPtr WndHandle { get; set; }
        [JsonIgnore]
        internal bool IsFirstRecognition { get; set; }
        [JsonIgnore]
        internal string ElementId { get; set; }
        [JsonIgnore]
        internal string ElementData { get; set; }
    }
}
