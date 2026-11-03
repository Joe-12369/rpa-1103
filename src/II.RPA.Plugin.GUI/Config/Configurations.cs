using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.GUI.AiRepair;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI
{
    internal class Configurations : ConfigurationBase<Configurations>
    {
        private string[] clientProcessNames;
        private Configurations()
        {
            this.Category = new List<GUICategory>();
            this.UIA2WindowClasses = new List<string>();
            clientProcessNames = new string[0];
            AiRepairUtils.AiRepairDataSavePath = string.Empty;
        }
        [JsonProperty("remotePort")]
        public int RemotePort { get; set; }
        [JsonProperty("category")]
        public List<GUICategory> Category { get; set; }
        /// <summary>
        /// CV切换时间间隔
        /// </summary>
        [JsonProperty("CVElementSwitchInterval")]
        public int CVElementSwitchInterval { get; set; }
        /// <summary>
        /// 应用界面相似度
        /// </summary>
        [JsonProperty("AppFaceProximity")]
        public double AppFaceProximity { get; set; }
        /// <summary>
        /// 应用界面相似度检测间隔时间
        /// </summary>
        [JsonProperty("AppFaceProximityMonitorInterval")]
        public double AppFaceProximityMonitorInterval { get; set; }
        /// <summary>
        /// 元素重叠百分比
        /// </summary>
        [JsonProperty("IntersectRatio")]
        public double IntersectRatio { get; set; }
        /// <summary>
        /// 启用远程拾取
        /// </summary>
        [JsonProperty("EnableRemoteCapture")]
        public bool EnableRemoteCapture { get; set; }

        /// <summary>
        /// 应用界面相似度检测间隔时间
        /// </summary>
        [JsonProperty("DataCollectionTurnPageButtonCssSelectorList")]
        public List<string> DataCollectionTurnPageButtonCssSelectorList { get; set; }
        /// <summary>
        /// 数据采集-页面垂直滚动后  延迟拾取时间（毫秒）【V1版本使用】
        /// </summary>
        [JsonProperty("ScrollVerticalDelay")]
        public int ScrollVerticalDelay { get; set; }
        /// <summary>
        /// 数据采集-页面垂直滚动后  延迟拾取时间（毫秒）【V2版本使用 v>=690】
        /// </summary>
        [JsonProperty("ScrollVerticalDelayV2")]
        public int ScrollVerticalDelayV2 { get; set; }
        /// <summary>
        /// 匹配元素后操作的默认超时时间（毫秒）
        /// </summary>
        [JsonProperty("OperationTimeout")]
        public long OperationTimeout { get; set; }
        /// <summary>
        /// Java使用的拾取模式，使用插件（extension)、使用代理（agent)
        /// </summary>
        [JsonProperty("JavaMode")]
        public string JavaMode { get; set; }

        /// <summary>
        /// 初次拾取时，根据 Win32 窗口类名强制使用 UIA2。
        /// 支持精确匹配和以 * 结尾的前缀匹配。
        /// </summary>
        [JsonProperty("UIA2WindowClasses")]
        public List<string> UIA2WindowClasses { get; set; }


        [JsonProperty("ClientProcessNames")]
        public string[] ClientProcessNames
        {
            get {
                return clientProcessNames;
            }
            set {
                clientProcessNames = value;
                GlobleConfigurations.ClientProcessNames = value;
            }
        }
        [JsonProperty("DeskTopTools")]
        public string[] DeskTopTools { get; set; }
        [JsonProperty("CaptureElementChoose")]
        public CaptureElementChoose CaptureElementChoose { get; set; }


        [JsonProperty("CvToNomalMoveMouse")]
        public bool CvToNomalMoveMouse { get; set; }
    }
    internal class GUICategory
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("sort")]
        public int Sort { get; set; }
    }
    internal class CaptureElementChoose
    {
        public double IntersectRatio { get; set; } = 0.5;
        public int MinWidth { get; set; } = 14;
        public int MinHeight { get; set; } = 14;
        public int MaxJammingPadding { get; set; } = 5;
    }
}
