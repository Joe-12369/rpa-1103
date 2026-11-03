using II.RPA.Plugin.Core.PluginBase;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.WdtErp
{
    internal class Configurations : ConfigurationBase<Configurations>
    {
        [JsonProperty("Port")]
        public int Port { get; set; }

        [JsonProperty("UseInjectionTempDirectory")]
        public bool UseInjectionTempDirectory { get; set; } = true;

    }
}
