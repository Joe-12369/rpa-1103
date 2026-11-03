using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.GUI.Commons.WindowMatch.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Windows.Config
{
    internal class WinConfigurations : ConfigurationBase<WinConfigurations>
    {
        private WinConfigurations()
        {
            this.Mode2Apps = new List<string>();
            this.BrushApp = new List<string>();
            this.AutoSeekDepthApps = new List<string>();
            this.ScanElementTreeApps = new List<string>();
            this.AccEnablerApps = new List<string>();
            this.JumpFallbackApps = new List<string>();
            //this.UseScanCacheApps = new List<string>();
            this.RpaProcess = new string[0];
        }
        [JsonProperty("Mode2Apps")]
        public List<string> Mode2Apps { get; set; }
        [JsonProperty("AutoSeekDepthApps")]
        public List<string> AutoSeekDepthApps { get; set; }
        [JsonProperty("ScanElementTreeApps")]
        public List<string> ScanElementTreeApps { get; set; }
        //[JsonProperty("UseScanCacheApps")]
        //public List<string> UseScanCacheApps { get; set; }
        [JsonProperty("AccEnablerApps")]
        public List<string> AccEnablerApps { get; set; }
        [JsonProperty("SeekDepth")]
        public int SeekDepth { get; set; }
        [JsonProperty("MatchSiblings")]
        public bool MatchSiblings { get; set; }
        [JsonProperty("BrushApp")]
        public List<string> BrushApp { get; set; }
        [JsonProperty("ErrorScan")]
        public bool ErrorScan { get; set; }
        [JsonProperty("MatchRootNodeMode")]
        public int MatchRootNodeMode { get; set; }
        [JsonProperty("RpaProcess")]
        public string[] RpaProcess { get; set; }
        [JsonProperty("ignoreVisible")]
        public string[] IgnoreVisible { get; set; }
        /// <summary>
        /// 构建选择器使用
        /// </summary>
        [JsonProperty("selectorUseScanCacheAppsInfo")]
        public Dictionary<string, AppsInfo> SelectorUseScanCacheAppsInfo { get; set; } = new Dictionary<string, AppsInfo>();
        /// <summary>
        /// 正向获取元素链路
        /// </summary>
        [JsonProperty("forwardElementChain")]
        public bool ForwardElementChain { get; set; }
        /// <summary>
        /// 构建前校验元素
        /// </summary>
        [JsonProperty("validateElementBeforeBuild")]
        public bool ValidateElementBeforeBuild { get; set; }
        /// <summary>
        /// 启用acc拾取元素
        /// </summary>
        [JsonProperty("captureByAcc")]
        public bool CaptureByAcc { get; set; }
        /// <summary>
        /// 计算Index时过滤隐藏(Offscreen)元素，避免因隐藏元素可见性变化导致Index漂移
        /// </summary>
        [JsonProperty("filterHiddenForIndex")]
        public bool FilterHiddenForIndex { get; set; }

        /// <summary>
        /// 允许"某层匹配失败后自动降级为跳级匹配"的应用(进程名，不含扩展名)。
        /// 该兜底是为硬CEF/Chromium等UIA树懒加载场景准备的，它会放弃失败节点的约束，
        /// 用剩余节点在任意层级上宽松匹配；若对所有应用生效，元素被改名/删除(元素校验)时
        /// 仍会被判定为命中，因此只对列表内的进程生效，列表为空表示全部不启用。
        /// </summary>
        [JsonProperty("jumpFallbackApps")]
        public List<string> JumpFallbackApps { get; set; }

        /// <summary>
        /// 判断指定进程是否允许自动跳级兜底
        /// </summary>
        public bool IsJumpFallbackApp(string processName)
        {
            if (string.IsNullOrEmpty(processName) || this.JumpFallbackApps == null || this.JumpFallbackApps.Count == 0)
            {
                return false;
            }
            return this.JumpFallbackApps.Contains(processName, StringComparer.OrdinalIgnoreCase);
        }
    }

    public class AppsInfo
    {
        public string processName { get; set; }
        public string MinVersion { get; set; }
        public string MaxVersion { get; set; }

    }

}
