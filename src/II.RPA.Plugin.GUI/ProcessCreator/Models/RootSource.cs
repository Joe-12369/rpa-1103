using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.ProcessCreator.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum RootSource
    {
        [Description("local")]
        Local = 0,
        [Description("global")]
        Global = 1,
        [Description("central")]
        Central = 2,
    }

}
