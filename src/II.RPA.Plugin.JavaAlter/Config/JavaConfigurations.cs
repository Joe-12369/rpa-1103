using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Java.Config
{
    internal class WindowMap 
    {
        public string Title { get; set; }
        public string NewTitle { get; set; }
    }
    internal class JavaConfigurations : ConfigurationBase<JavaConfigurations>
    {
        private JavaConfigurations()
        {
        }
        public int ClickBefore { get; set; } = 0;
        public int ClickAfter { get; set; } = 0;

        [JsonProperty("whitelist")]
        public List<string> Whitelist { get; set; }

        [JsonProperty("windowMap")]
        public List<WindowMap> WindowMaps { get; set; }

        
    }
}
