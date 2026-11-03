using II.RPA.Plugin.Core.PluginBase;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.DriverInput.Config
{
    internal class Configurations : ConfigurationBase<Configurations>
    {
        private Configurations()
        {
        }

        /// <summary>
        /// 驱动名称 填写的话固定挑选包含此名称的首个驱动进行驱动输入
        /// </summary>
        [JsonProperty("CompositeName")]
        public string CompositeName { get; set; }

    }
  
}
