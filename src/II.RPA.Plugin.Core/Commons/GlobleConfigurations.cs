using II.RPA.Plugin.Core.Commons;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
namespace II.RPA.Plugin.Core
{
    /// <summary>
    /// 全局配置类，非各插件公共配置，不可以使用。【更新代价极高】。
    /// </summary>
    public class GlobleConfigurations
    {
        static GlobleConfigurations()
        {
            RunningStateManager = new RunningStateManager();
        }

        #region 全局配置 非各插件公共配置，不可以放置
        #region  元素 和应用 字典相关的一些缓存操作
        /// <summary>
        /// 正在被拾取应用的进程名
        /// </summary>
        public static string InspectAppProcessName { get; set; } = "";
        #endregion

        #region 启动进程相关的一些信息
        private static int _currentProcessId = 0;
        public static int CurrentProcessId
        {
            get
            {
                if (_currentProcessId == 0)
                {
                    _currentProcessId = Process.GetCurrentProcess().Id;
                }
                return _currentProcessId;
            }
        }
        #endregion

        /// <summary>
        /// 图像元素解析地址
        /// </summary>
        public static string CVServer { get; set; } = "";
        /// <summary>
        /// CV操作（执行）前解析服务地址(元素拾取地址)
        /// </summary>
        public static string CVExecutionServiceURL { get; set; } = "";
        /// <summary>
        /// Java拾取模式
        /// </summary>
        public static JavaCaptureModeType JavaCaptureMode { get; set; }
        /// <summary>
        /// 用户登录名，可以是账号或者手机(离线情况传激活码)
        /// </summary>
        public static string ClientUser { get; set; } = "";
        /// <summary>
        /// 用户机器id
        /// </summary>
        public static string ClientMac { get; set; } = "";

        public static bool IsRemoteCaptureServer { get; set; } = false;

        /// <summary>
        /// 是否启用老流程元素AI增强截图恢复。
        /// 开启后，元素自身未启用智能修复时也允许补图并触发兼容AI增强。
        /// </summary>
        public static bool EnableAiEnhancedScreenshot { get; set; } = false;

        /// <summary>
        /// 当前流程包信息，由上游赋值，字段为 packageName、packageVersion、packageInnerId。
        /// </summary>
        public static object PackageInfo { get; set; }


        public static string UserToken { get; set; } = "";
        /// <summary>
        /// 
        /// </summary>
        public static string EngineCode { get; set; } = "";

        public static string captureUniqueID = string.Empty;
        public static string CaptureUniqueID
        {
            get
            {
                return captureUniqueID;
            }
            set
            {
                //captureUniqueID = $"{RandomHelper.RandomString(5)}-{value}";
                captureUniqueID = value;
            }
        }

        public static bool IsSyncProjectPathToZBrain { get; set; } = false;
        public static string ZBrainServicePath { get; set; } = string.Empty;
        public static int CoreServerPort { get; set; } = 0;
        public static ClientType ClientType { get; set; }

        public static BrowserConnType BrowserConnType { get; set; }
        public static int ServiceHubPort { get; set; } = 36516;

        public static int InitConnectTimeout { get; set; } = 3050;
        public static int TransportConnectTimeout { get; set; } = 300;
        public static int DeadlockErrorTimeout { get; set; } = 1000;

        /// <summary>
        /// Selector的默认版本号，每次修改时，注意增加版本号
        /// 【<=700-->1.1】往前不在追溯
        /// 【701-->1.2】
        /// </summary>
        [Obsolete("新版本请使用CurrentSelectorVersion，SelectorLatestVersion为兼容710sp2及老版本存在")]
        public const string SelectorLatestVersion = "1.2";
        public static string CurrentSelectorVersion { get; set; } = "1.6";

        public static string PackageDirectory { get; set; } = string.Empty;
        /// <summary>
        /// 依赖包所在目录，680版本之后（流程使用运行时） 是运行时版本 
        /// </summary>
        public static string PluginPath { get; set; }
        public static string ClientUserId { get; set; }
        public static string ClientTenantCode { get; set; }
        public static string TarsAgentServiceUrl { get; set; }
        public static string TarsAgentServiceToken { get; set; }
        #endregion

        public static bool IsReStart
        {
            get;
            set;
        } = false;

        /// <summary>
        /// Agent 静默运行 使用自定义浏览器标志
        /// </summary>
        public static bool? UseSZBrowser
        {
            get; set;
        } = null;
        public static string RunMode { get; set; }


        /// <summary>
        /// 安装目录下 Support 文件夹路径
        /// </summary>
        public static string SupportDirectory { get; set; }
        public static string[] ClientProcessNames { get; set; } = new string[0];

        /// <summary>
        /// 是否智能修复校验到元素
        /// </summary>
        public static bool IsAiRepaireOfVerify { get; set; }
        /// <summary>
        /// 智能修复埋点数据字典缓存
        /// </summary>
        public static IDictionary<string, string> AiBuriedDataCacheDic { get; set; } = new Dictionary<string, string>();

        public static RunningStateManager RunningStateManager { get; set; }

    }

}
