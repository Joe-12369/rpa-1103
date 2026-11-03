using II.RPA.Plugin.Core.PluginBase;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.CV
{
    internal class Configurations : ConfigurationBase<Configurations>
    {
        private Configurations()
        {
        }
        [JsonProperty("MatchRootNodeMode")]
        public MatchRootNodeModeType MatchRootNodeMode { get; set; }

        internal bool NeedHandleCorrection { get; set; } = false;
    }
    internal enum MatchRootNodeModeType
    {
        /// <summary>
        /// 老的UIA节点匹配方式
        /// </summary>
        UIA = 0,
        /// <summary>
        /// 窗口匹配方式
        /// </summary>
        WindowMatch = 1
    }
}
