using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.WindowMatch.Entities;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace II.RPA.WebBrowser.Common.MultiInstance
{
    /// <summary>
    /// 动态发现未配置的 Chromium 浏览器。
    /// 依据：窗口进程树中存在 II.RPA.NativeMessagingHost.exe 子进程即认为是浏览器。
    /// 自动采集 ProductName、进程名、exe 路径等信息，构建 MatchConfig 并持久化到
    /// ChromiumBrowserConfig.json，配合 PluginPackage.ActivateDiscoveredBrowser 即可立即生效。
    /// </summary>
    public static class ChromiumBrowserDiscovery
    {
        private const string NMH_PROCESS_NAME = "II.RPA.NativeMessagingHost";
        private const string CHROMIUM_RENDER_CLASS = "Chrome_RenderWidgetHostHWND";
        private const string IE_RENDER_CLASS = "Internet Explorer_Server";

        private static readonly ILogger logger = Logger.OfContext();
        private static readonly object _fileLock = new object();

        /// <summary>
        /// 进程级发现缓存。Key = 进程 PID，Value = 发现结果（是否有 NMH + MatchConfig）。
        /// 避免每次拾取同一进程的窗口时重复走进程树枚举。
        /// </summary>
        private static readonly ConcurrentDictionary<int, DiscoveryCacheEntry> _processCache =
            new ConcurrentDictionary<int, DiscoveryCacheEntry>();

        /// <summary>
        /// NMH 进程级发现缓存。Key = NMH 进程 PID，Value = 发现结果。
        /// 用于 TryDiscoverFromNMH 路径，避免同一 NMH 进程重复枚举。
        /// </summary>
        private static readonly ConcurrentDictionary<int, DiscoveryCacheEntry> _nmhCache =
            new ConcurrentDictionary<int, DiscoveryCacheEntry>();

        /// <summary>
        /// 当 TryDiscover 成功发现新浏览器时触发。
        /// 订阅方（如 PluginPackage）可调用 ActivateDiscoveredBrowser 即时注册。
        /// </summary>
        public static event Action<MatchConfig> OnBrowserDiscovered;

        private static readonly string ConfigFilePath;

        static ChromiumBrowserDiscovery()
        {
            var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            ConfigFilePath = Path.Combine(localApp, "IIRPA", "ChromiumBrowserConfig.json");
        }

        #region Public API

        /// <summary>
        /// 尝试发现并持久化一个新的 Chromium 浏览器配置。
        /// 通过 BrowserProcessMatcher 检测窗口进程树中是否存在 NMH 进程，
        /// 自动采集 ProductName、进程名、exe 路径，构建 MatchConfig 并写入配置文件。
        /// 如果已存在同 ProductName 的配置，返回已有配置（幂等）。
        /// 返回 null 表示窗口不满足发现条件。
        /// </summary>
        public static MatchConfig TryDiscover(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero || !NativeMethods.IsWindow(windowHandle))
            {
                logger.LogDebug("TryDiscover: 窗口句柄无效");
                return null;
            }

            // 获取窗口进程 PID，用于缓存查询
            NativeMethods.GetWindowThreadProcessId(windowHandle, out uint pidUint);
            int windowPid = (int)pidUint;
            if (windowPid == 0)
            {
                logger.LogDebug("TryDiscover: 无法获取窗口进程 PID");
                return null;
            }

            // 查缓存：同一进程已检查过，直接返回上次结果
            if (_processCache.TryGetValue(windowPid, out var cached))
            {
                logger.LogDebug($"TryDiscover: 命中缓存 PID={windowPid}, HasNmh={cached.HasNmh}, IsComputing={cached.IsComputing}");
                // 计算中 → 已有 Task 在跑，本次跳过避免重复枚举
                return cached.IsComputing ? null : cached.Config;
            }

            // 原子占位：防止同一 PID 的并发调用重复进入重操作
            if (!_processCache.TryAdd(windowPid, new DiscoveryCacheEntry { IsComputing = true }))
            {
                // TryAdd 失败 = 另一个 Task 刚好先占位了（极端并发），跳过
                logger.LogDebug($"TryDiscover: PID={windowPid} TryAdd 失败，已有并发计算在进行，跳过");
                return null;
            }

            // 获取窗口进程名，作为 BrowserProcessMatcher 的 processFeatureNames 参数
            string windowProcessName = GetWindowProcessName(windowHandle);
            if (string.IsNullOrEmpty(windowProcessName))
            {
                logger.LogDebug("TryDiscover: 无法获取窗口进程名");
                _processCache[windowPid] = new DiscoveryCacheEntry { HasNmh = false };
                return null;
            }
            if (Basic.BrowserWindowProvider.IsSupportedBrowser(windowProcessName))
            {
                logger.LogDebug($"{windowProcessName} 与已知浏览器同名，自动增加配置存在风险，因此此次不自动增加配置");
                _processCache[windowPid] = new DiscoveryCacheEntry { HasNmh = false };
                return null;
            }
            // 通过 BrowserProcessMatcher 检测 NMH 并获取主进程 PID（重操作）
            var hostIds = BrowserProcessMatcher.FindHostProcessIdsFromWindow(
                windowHandle,
                new[] { NMH_PROCESS_NAME },
                new[] { windowProcessName });

            if (hostIds == null || hostIds.Count == 0)
            {
                logger.LogDebug("TryDiscover: 窗口进程树中未发现 NMH 进程");
                _processCache[windowPid] = new DiscoveryCacheEntry { HasNmh = false };
                return null;
            }

            int mainPid = BrowserProcessMatcher.ResolveMainProcessIdFromWindow(
                windowHandle,
                new[] { windowProcessName });

            if (mainPid <= 0)
            {
                logger.LogDebug("TryDiscover: 无法解析浏览器主进程 PID");
                _processCache[windowPid] = new DiscoveryCacheEntry { HasNmh = true };
                return null;
            }

            var config = BuildPersistAndActivate(mainPid);
            _processCache[windowPid] = new DiscoveryCacheEntry { HasNmh = true, Config = config };
            return config;
        }

        /// <summary>
        /// 不依赖浏览器窗口，直接从所有运行中的 NMH 进程出发，反向发现浏览器主进程。
        /// 遍历 NMH 进程 → 沿父链向上找浏览器 → 采集信息 → 构建配置 → 持久化 → 激活。
        /// 适用于浏览器已运行但尚未拾取窗口的场景（如后台启动、首次激活）。
        /// </summary>
        public static void TryDiscoverFromNMH()
        {
            var nmhProcesses = BrowserProcessMatcher.GetProcessesByNames(new[] { NMH_PROCESS_NAME });

            foreach (var nmhProc in nmhProcesses)
            {
                try
                {
                    int nmhPid = nmhProc.Id;

                    // 查 NMH 缓存
                    if (_nmhCache.TryGetValue(nmhPid, out var cached))
                    {
                        logger.LogDebug($"TryDiscoverFromNMH: 命中 NMH 缓存 PID={nmhPid}, HasNmh={cached.HasNmh}");
                        continue;
                    }

                    // 从 NMH 向上解析浏览器主进程
                    string processFeatureName;
                    int mainPid = BrowserProcessMatcher.ResolveBrowserProcessFromNMH(nmhProc, out processFeatureName);
                    if (mainPid <= 0 || string.IsNullOrEmpty(processFeatureName))
                    {
                        logger.LogDebug($"TryDiscoverFromNMH: NMH PID={nmhPid} 无法解析到浏览器主进程");
                        _nmhCache[nmhPid] = new DiscoveryCacheEntry { HasNmh = false };
                        continue;
                    }
                    if (Basic.BrowserWindowProvider.IsSupportedBrowser(processFeatureName))
                    {
                        logger.LogDebug($"TryDiscoverFromNMH: NMH PID={nmhPid} 解析到浏览器主进程 PID={mainPid} ({processFeatureName}) 是已经支持过的浏览器进程，如果是同名浏览器，需要使用“添加自定义浏览器功能”");
                        _nmhCache[nmhPid] = new DiscoveryCacheEntry { HasNmh = false };
                        continue;
                    }
                    logger.LogInformation($"TryDiscoverFromNMH: NMH PID={nmhPid} → 浏览器主进程 PID={mainPid} ({processFeatureName})");

                    var config = BuildPersistAndActivate(mainPid);
                    if (config != null)
                    {
                        _nmhCache[nmhPid] = new DiscoveryCacheEntry { HasNmh = true, Config = config };
                    }
                    else
                    {
                        _nmhCache[nmhPid] = new DiscoveryCacheEntry { HasNmh = true };
                    }
                }
                catch (Exception ex)
                {
                    logger.LogDebug($"TryDiscoverFromNMH: 处理 NMH PID={nmhProc.Id} 异常: {ex.Message}");
                }
                finally
                {
                    nmhProc.Dispose();
                }
            }
        }

        /// <summary>
        /// 判断窗口所属进程树中是否存在 II.RPA.NativeMessagingHost.exe。
        /// 直接委托给 BrowserProcessMatcher。
        /// </summary>
        public static bool HasNativeMessagingHost(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero || !NativeMethods.IsWindow(windowHandle))
                return false;

            try
            {
                // 先查缓存
                NativeMethods.GetWindowThreadProcessId(windowHandle, out uint pidUint);
                int windowPid = (int)pidUint;
                if (windowPid != 0 && _processCache.TryGetValue(windowPid, out var cached))
                    return cached.HasNmh;

                string windowProcessName = GetWindowProcessName(windowHandle);
                if (string.IsNullOrEmpty(windowProcessName))
                    return false;

                var hostIds = BrowserProcessMatcher.FindHostProcessIdsFromWindow(
                    windowHandle,
                    new[] { NMH_PROCESS_NAME },
                    new[] { windowProcessName });

                bool hasHost = hostIds != null && hostIds.Count > 0;

                // 写入缓存
                if (windowPid != 0)
                    _processCache[windowPid] = new DiscoveryCacheEntry { HasNmh = hasHost };

                return hasHost;
            }
            catch (Exception ex)
            {
                logger.LogDebug($"HasNativeMessagingHost 异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 将 MatchConfig 写入 ChromiumBrowserConfig.json。
        /// 文件不存在时会自动创建目录和文件。
        /// 返回 false 表示 productName 已存在（幂等保护）。
        /// </summary>
        public static bool PersistMatchConfig(string productName, MatchConfig config)
        {
            if (string.IsNullOrEmpty(productName) || config == null)
                return false;

            var map = ReadConfigMap();
            if (map.ContainsKey(productName))
            {
                logger.LogDebug($"PersistMatchConfig: '{productName}' 已存在，跳过写入");
                return false;
            }

            map[productName] = config;
            WriteConfigMap(map);
            logger.LogInformation($"浏览器配置已持久化: {productName}");
            return true;
        }

        #endregion

        #region 共享的配置构建 / 持久化 / 激活

        /// <summary>
        /// 根据浏览器主进程 PID，采集 exe 信息 → ProductName → 构建 MatchConfig →
        /// 持久化到配置文件 → 触发 OnBrowserDiscovered 激活事件。
        /// 幂等：若 ProductName 在配置中已存在，返回已有配置不重复写入。
        /// 返回 MatchConfig，失败（无法获取 exe / 进程名）返回 null。
        /// </summary>
        private static MatchConfig BuildPersistAndActivate(int mainPid)
        {
            string exePath = GetExeFilePath(mainPid);
            if (string.IsNullOrEmpty(exePath))
            {
                logger.LogWarning($"BuildPersistAndActivate: 无法获取主进程 PID {mainPid} 的 exe 路径");
                return null;
            }

            string processName;
            try
            {
                using (var proc = Process.GetProcessById(mainPid))
                    processName = proc.ProcessName;
            }
            catch (Exception ex)
            {
                logger.LogDebug($"BuildPersistAndActivate: 无法获取主进程名 PID {mainPid}: {ex.Message}");
                return null;
            }

            string productName = GetProductName(exePath);

            // 幂等：已存在则直接返回
            var existingMap = ReadConfigMap();
            if (existingMap != null && existingMap.ContainsKey(productName))
            {
                logger.LogDebug($"浏览器 '{productName}' 已存在，返回已有配置");
                return existingMap[productName];
            }

            // 构建并持久化
            var config = BuildMatchConfig(exePath, processName, productName);

            if (!PersistMatchConfig(productName, config))
            {
                logger.LogWarning($"持久化配置失败: {productName}");
            }

            // 通知订阅方（如 PluginPackage）即时激活新发现的浏览器
            OnBrowserDiscovered?.Invoke(config);

            return config;
        }

        #endregion

        #region MatchConfig 构建

        /// <summary>
        /// 根据动态采集的信息构建 MatchConfig。
        /// 仅填充必要字段：ProductName、LableName、ProcessFeatures、AppExePath，
        /// 其余使用 Chromium 通用默认值。
        /// </summary>
        private static MatchConfig BuildMatchConfig(string exePath, string processName, string productName)
        {
            return new MatchConfig
            {
                ConfigType = MatchConfigType.Normal,
                ProductName = productName,
                LableName = productName,
                ProcessFeatures = new[] { processName },
                AppExePath = exePath,
                RenderClassNames = new[] { CHROMIUM_RENDER_CLASS },
                IERenderClassNames = new[] { IE_RENDER_CLASS },
                BrowserWndClassNames = new string[0],
                TitleFeatures = new string[0]
            };
        }

        #endregion

        #region Exe 信息提取

        /// <summary>
        /// 通过 PID 安全获取 exe 文件全路径。
        /// </summary>
        private static string GetExeFilePath(int pid)
        {
            try
            {
                using (var process = Process.GetProcessById(pid))
                    return process.GetExeFilePath();
            }
            catch (Exception ex)
            {
                logger.LogDebug($"无法获取 PID {pid} 的 exe 路径: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取窗口所属进程名。
        /// </summary>
        private static string GetWindowProcessName(IntPtr windowHandle)
        {
            try
            {
                return UiCommon.GetProcessNameByCache(windowHandle);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 从 exe 文件读取 ProductName（通过 FileVersionInfo）。
        /// 如果 ProductName 为空则回退为文件名（不含扩展名）。
        /// </summary>
        private static string GetProductName(string exePath)
        {
            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(exePath);
                if (!string.IsNullOrWhiteSpace(versionInfo.ProductName))
                    return versionInfo.ProductName.Trim();
            }
            catch (Exception ex)
            {
                logger.LogDebug($"无法读取 FileVersionInfo: {exePath}: {ex.Message}");
            }

            // 回退：使用文件名（不含扩展名）
            return Path.GetFileNameWithoutExtension(exePath);
        }

        #endregion

        #region JSON 配置读写

        /// <summary>
        /// 读取 ChromiumBrowserConfig.json 中的 BrowserConfigMap。
        /// 文件由 PluginPackage.SetMatchConfigMap() 在启动时完成迁移和初始化，
        /// 此处仅做读取。
        /// </summary>
        private static Dictionary<string, MatchConfig> ReadConfigMap()
        {
            lock (_fileLock)
            {
                try
                {
                    if (!File.Exists(ConfigFilePath))
                        return new Dictionary<string, MatchConfig>();

                    var json = File.ReadAllText(ConfigFilePath);
                    if (string.IsNullOrWhiteSpace(json))
                        return new Dictionary<string, MatchConfig>();

                    var obj = JObject.Parse(json);
                    return obj["BrowserConfigMap"]?.ToObject<Dictionary<string, MatchConfig>>()
                           ?? new Dictionary<string, MatchConfig>();
                }
                catch (Exception ex)
                {
                    logger.LogError($"读取 ChromiumBrowserConfig.json 异常: {ex.Message}");
                    return new Dictionary<string, MatchConfig>();
                }
            }
        }

        /// <summary>
        /// 将 BrowserConfigMap 写回 ChromiumBrowserConfig.json。
        /// 使用 temp → replace 策略保证写入原子性。
        /// </summary>
        private static void WriteConfigMap(Dictionary<string, MatchConfig> map)
        {
            lock (_fileLock)
            {
                try
                {
                    var dir = Path.GetDirectoryName(ConfigFilePath);
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    var obj = new JObject
                    {
                        ["BrowserConfigMap"] = JObject.FromObject(map)
                    };

                    string json = obj.ToString();
                    string tempPath = ConfigFilePath + ".tmp";

                    File.WriteAllText(tempPath, json);

                    if (File.Exists(ConfigFilePath))
                        File.Replace(tempPath, ConfigFilePath, ConfigFilePath + ".bak");
                    else
                        File.Move(tempPath, ConfigFilePath);

                    // 清理备份
                    var bakPath = ConfigFilePath + ".bak";
                    if (File.Exists(bakPath))
                    {
                        try { File.Delete(bakPath); }
                        catch { /* 非关键 */ }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"写入 ChromiumBrowserConfig.json 异常: {ex.Message}");
                }
            }
        }

        #endregion

        #region 缓存

        /// <summary>
        /// 进程级发现缓存条目。
        /// HasNmh=false 表示该进程已确认没有 NMH 子进程，后续直接返回 null。
        /// HasNmh=true 时 Config 为已构建/已持久化的 MatchConfig。
        /// IsComputing=true 表示已有 Task 正在枚举该进程的进程树，后续调用应跳过。
        /// </summary>
        private class DiscoveryCacheEntry
        {
            /// <summary>是否正在计算中（防止同一 PID 并发重复计算）</summary>
            public bool IsComputing;
            /// <summary>该进程是否拥有 II.RPA.NativeMessagingHost 子进程</summary>
            public bool HasNmh;
            /// <summary>HasNmh=true 时的匹配配置，否则为 null</summary>
            public MatchConfig Config;
        }

        #endregion
    }
}
