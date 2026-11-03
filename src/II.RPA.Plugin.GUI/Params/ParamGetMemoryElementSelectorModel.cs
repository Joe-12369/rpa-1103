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
    public class ParamGetMemoryElementSelectorModel : ParamBase
    {
        public string guiType { get; set; }
        public string filePath { get; set; }
        public string AFilePath => Path.Combine(GlobleConfigurations.PackageDirectory, filePath);

        [JsonProperty("memoryInfo")]
        public Object MemoryInfo { get; set; }
        public WindowInfo WindowInfo { get; set; } = null;
    }


}
