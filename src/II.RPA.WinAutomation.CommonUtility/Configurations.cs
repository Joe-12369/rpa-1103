using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace II.RPA.WinAutomation.CommonUtility
{
    public class Configurations
    {
        private static ILogger logger = Logger.OfContext();
        private static JObject jObject = null;

        /// <summary>
        /// 全局配置类变量
        /// </summary>
        public static Configurations Configs { get; set; }

        /// <summary>
        /// 元素 和应用 字典，key是根窗口句柄 
        /// </summary>
        public static Dictionary<object, string> ElementAPPDic { get; set; }
            = new Dictionary<object, string>();

        public static void Load()
        {
            try
            {
                Configs = new Configurations();
                string filePath = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Configs.GetType().Assembly.Location)), @"II.RPA\rpacf.json");

                var lines = File.ReadLines(filePath, System.Text.Encoding.UTF8).ToList();
                lines.RemoveAll(s => Regex.IsMatch(s, @"^\s*\/{2}[\s\S]*$"));
                using (JsonTextReader reader = new JsonTextReader(new StringReader(string.Join("", lines))))
                {
                    jObject = JObject.Load(reader);
                }
                Configs.Mode2App = GetValue("UIAMode.Mode2App").ToLower().Split(',');

                int.TryParse(GetValue("Capture.SeekDepth"), out int seekDepth);
                Configs.SeekDepth = seekDepth;

                Configs.MatchSiblings = GetValue("Capture.MatchSiblings") == "1";
                Configs.BrushApp = GetValue("Capture.BrushApp").ToLower().ToObject<List<string>>();
                int.TryParse(GetValue("Capture.JavaCaptureMode"), out int javaCaptureMode);
                Configs.JavaCaptureMode = (JavaCaptureModeType)javaCaptureMode;

                Configs.ErrorScan = GetValue("Matchs.ErrorScan") == "1";
                Configs.FilterHiddenForIndex = GetValue("Capture.FilterHiddenForIndex") == "1";
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "加载配置文件的时候发生了错误");
            }
        }

        #region 拾取 相关

        /// <summary>
        /// 模式2应用
        /// </summary>
        public string[] Mode2App { get; private set; }

        /// <summary>
        /// 寻找子子元素的深度
        /// </summary>
        public int SeekDepth { get; set; }

        /// <summary>
        /// 匹配同胞元素
        /// </summary>
        public bool MatchSiblings { get; set; }

        /// <summary>
        /// Java拾取模式
        /// </summary>
        public JavaCaptureModeType JavaCaptureMode { get; set; }

        private UIAModeType _UIAMode;

        public static UIAModeType UIAMode
        {
            get
            {
                return Configs._UIAMode;
            }
            private set
            {
                Configs._UIAMode = value;
            }
        }

        /// <summary>
        /// 需要从根节点刷一遍的应用
        /// </summary>
        public List<string> BrushApp { get; set; }

        #endregion

        #region 匹配 查找相关

        /// <summary>
        /// 错误扫描，匹配不到元素时 是否 纠错查找
        /// </summary>
        public bool ErrorScan { get; set; }

        /// <summary>
        /// 计算Index时过滤隐藏(Offscreen)元素，避免因隐藏元素可见性变化导致Index漂移
        /// </summary>
        public bool FilterHiddenForIndex { get; set; }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置UIAMode
        /// </summary>
        /// <param name="app">进程名</param>
        /// <returns>默认UIA3</returns>
        public static UIAModeType SetUIAMode(string appName)
        {
            try
            {
                if (Configs.Mode2App.Contains(appName.ToLower()))
                {
                    UIAMode = UIAModeType.UIA2;
                }
                else
                {
                    UIAMode = UIAModeType.UIA3;
                }
                return UIAMode;
            }
            catch
            {
                return UIAModeType.UIA3;
            }
        }

        public string this[string key]
        {
            get
            {
                try
                {
                    string str = "";
                    if (jObject != null)
                    {
                        str = GetValue(key);
                    }
                    return str;
                }
                catch
                {
                    return "";
                }
            }
        }

        public static T GetValue<T>(string key) where T : class
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(jObject.SelectToken(key).ToString());
            }
            catch (Exception)
            {
                return default(T);
            }
        }

        public static string GetValue(string key)
        {
            try
            {
                return Regex.Replace((jObject.SelectToken(key).ToString()), @"\s", "");
            }
            catch (Exception)
            {
                return "";
            }
        }

        #endregion
    }

    public enum UIAModeType
    {
        None = 0,
        UIA2 = 2,
        UIA3 = 3
    }

    public enum JavaCaptureModeType
    {
        /// <summary>
        /// 直接根据点获取（应用库提供的接口）
        /// </summary>
        Direct = 0,
        /// <summary>
        /// 一层一层
        /// </summary>
        LayerByLayer = 1
    }
}
