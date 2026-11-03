using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamGetElementSelectorModel : ParamBase
    {
        private string elementId;

        public string guiType { get; set; }
        public string ElementData { get; set; }
        public int windowHandle { get; set; }
        public string filePath { get; set; }
        public string AFilePath => Path.Combine(GlobleConfigurations.PackageDirectory, filePath);
        public string sourceType { get; set; }

        [JsonIgnore]
        public string ElementId
        {
            get
            {
                if (string.IsNullOrEmpty(elementId))
                {
                    var elementDataObj = JObject.Parse(this.ElementData);
                    elementId = elementDataObj["uniqueId"].ToString();
                }
                return elementId;
            }
            set => elementId = value;
        }
    }
}
