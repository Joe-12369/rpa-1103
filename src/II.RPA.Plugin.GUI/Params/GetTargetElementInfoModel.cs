using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class GetTargetElementInfoModel : ParamBase
    {
        public EventType eventType { get; set; }
        public string guiType { get; set; } = string.Empty;
        public string windowType { get; set; } = string.Empty;
        public string sourceType { get; set; } = string.Empty;
        public string prevCVSelector = string.Empty;
        //非入参
        [JsonIgnore]
        internal IntPtr WndHandle { get; set; }
        [JsonProperty("task_info")]
        public AgentStepInfo taskInfo { get; set; }
        //先这样处理，兼容老代码逻辑
        [JsonIgnore]
        public DataCollect dataCollect { get; set; } = new DataCollect();
        public string filePath { get; set; }
        public string AFilePath => Path.Combine(GlobleConfigurations.PackageDirectory, filePath);
        [JsonProperty("finish_judge")]
        public bool finishJudge { get; set; }
        public bool noMapStep { get; set; }
    }
    public class DataCollect
    {
        [JsonProperty("iSEnableTable")]
        public int ISEnableTable { get; set; } = 0;
        [JsonProperty("columnType")]
        public List<string> ColumnType { get; set; }
    }
    public enum EventType
    {
        /// <summary>
        /// 元素类
        /// </summary>
        Element,
        /// <summary>
        /// 视觉
        /// </summary>
        Visual
    }

}
