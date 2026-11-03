using II.RPA.Plugin.Core.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.WebBrowser.Params
{
    public class ParamBase : II.RPA.Plugin.Core.PluginBase.PluginParameterBase
    {
        public string webData { get; set; }
        public string browserType { get; set; }
        public string remoteAddress { get; set; }
    }
}
