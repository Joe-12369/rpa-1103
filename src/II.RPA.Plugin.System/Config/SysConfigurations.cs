using II.RPA.Plugin.Core.PluginBase;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.RPASystem.Config
{
    internal class SysConfigurations : ConfigurationBase<SysConfigurations>
    {
        private SysConfigurations()
        {
        }
        public string RemoteServiceAddress { get; set; } = string.Empty;
    }

}
