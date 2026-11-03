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
    public class GetSelectorByPointModel : ParamBase
    {
        [JsonIgnore]
        public string GuiType { get; set; }
        [JsonProperty("filePath")]
        public string FilePath { get; set; }
        [JsonProperty("left")]
        public int Left { get; set; }
        [JsonProperty("top")]
        public int Top { get; set; }
        [JsonIgnore]
        public string FileFullName => Path.Combine(GlobleConfigurations.PackageDirectory, this.FilePath);
    }
}
