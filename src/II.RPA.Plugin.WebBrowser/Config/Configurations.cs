using II.RPA.Plugin.Core.PluginBase;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace II.RPA.Plugin.WebBrowser.Config
{
    internal class Configurations : ConfigurationBase<Configurations>
    {
        private Configurations()
        {
            this.Category = new List<GUICategory>();
        }
        [JsonProperty("category")]
        public List<GUICategory> Category { get; set; }
        
    }
    internal class GUICategory
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
