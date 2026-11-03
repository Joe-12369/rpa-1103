using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.RPASystem.Params
{
    public class ParamLocalRemoteConModel : II.RPA.Plugin.Core.PluginBase.PluginParameterBase
    {
        /// <summary>
        /// IP地址
        /// </summary>
        [JsonProperty("ip")]
        public string Ip { get; set; }

        /// <summary>
        /// 端口
        /// </summary>
        [JsonProperty("port")]
        public string Port { get; set; } = "3389";

        /// <summary>
        /// 登录用户
        /// </summary>
        [JsonProperty("user")]
        public string User { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        [JsonProperty("pw")]
        public string Pw { get; set; }

        /// <summary>
        /// 等待连接时间
        /// </summary>
        [JsonProperty("waitTime")]
        public int WaitTime { get; set; } = 2000;
    }
}
