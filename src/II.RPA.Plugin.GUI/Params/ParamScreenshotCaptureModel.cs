using II.RPA.Plugin.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamScreenshotCaptureModel : ParamBase
    {
        /// <summary>
        /// 动作指令
        /// </summary>
        [JsonProperty("actionCmd")]
        public string ActionCmd { get; set; }
        [JsonProperty("filePath")]
        public string FilePath { get; set; }
        [JsonProperty("memoryInfo")]
        public Object MemoryInfo { get; set; }
        [JsonIgnore]
        public string FileFullName => Path.Combine(GlobleConfigurations.PackageDirectory, this.FilePath);

    }
}
