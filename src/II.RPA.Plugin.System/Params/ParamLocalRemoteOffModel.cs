using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.RPASystem.Params
{
    public class ParamLocalRemoteOffModel : II.RPA.Plugin.Core.PluginBase.PluginParameterBase
    {
        /// <summary>
        /// IP地址
        /// </summary>
        [JsonProperty("ip")]
        public string Ip { get; set; }

        /// <summary>
        /// 登录用户
        /// </summary>
        [JsonProperty("user")]
        public string User { get; set; }
    }
}
