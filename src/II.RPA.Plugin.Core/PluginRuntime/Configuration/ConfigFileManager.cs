using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace II.RPA.Plugin.Core.PluginBase.PluginConfiguration
{
    public class ConfigFileManager : INeedReset
    {
        private const string FILENAME_CONFIGUPDATELOGS = "configUpdateLogs.json";
        private const string FILENAME_CONFIG = "config.json";
        private const string DIR_PRIVATE = "private";
        /// <summary>与 private config.json 同目录的完整备份，用于损坏时还原（与 LoadConfigInfo 解析规则一致）。</summary>
        private const string SUFFIX_LAST_GOOD = ".lastgood";

        private static readonly object _lockObj = new object();

        private ILogger logger = Logger.OfContext();

        private static ConfigFileManager _instance = null;

        public static ConfigFileManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockObj)
                    {
                        if (_instance == null)
                        {
                            _instance = new ConfigFileManager();
                            NeedResetObjectManager.AddNeedResetObject(_instance);
                        }
                    }
                }
                return _instance;
            }
        }
        private ConfigFileManager()
        {

        }
        public void Reset(bool isSamePackage)
        {
            if (isSamePackage)
            {
                return;
            }
            NeedResetObjectManager.RemoveNeedResetObject(_instance);
            _instance = null;
        }
        #region 获取目录公共方法
        private string GetRuntimeConfigFilePath(string pluginName)
        {
            string runtimeConfigFilePath = Path.Combine(GlobleConfigurations.PluginPath, "packages", pluginName, FILENAME_CONFIG);
            if (!File.Exists(runtimeConfigFilePath))
            {
                return Path.Combine(GlobleConfigurations.PluginPath, pluginName, FILENAME_CONFIG);
            }
            return runtimeConfigFilePath;
        }
        private string GetStandardConfigFilePath(string pluginName)
        {
            return Path.Combine(GlobleConfigurations.PackageDirectory, DIR_PRIVATE, "standard", pluginName, FILENAME_CONFIG);
        }
        private string GetPrivateConfigFilePath(string pluginName)
        {
            return Path.Combine(GlobleConfigurations.PackageDirectory, DIR_PRIVATE, pluginName, FILENAME_CONFIG);
        }
        private string GetUpdateLogFilePath()
        {
            return Path.Combine(GlobleConfigurations.PackageDirectory, DIR_PRIVATE, "standard", FILENAME_CONFIGUPDATELOGS);
        }
        #endregion
        private static List<PluginInfo> PluginInfos { get; set; }
        private Version RuntimeVersion = null;
        private int RuntimeBuildNumber = 0;
        private Version MinRuntimeVersion = new Version("6.8.0.0");
        private static readonly object _pluginInfosLock = new object();

        /// <summary>configUpdateLogs.json 进程内缓存，避免每次按需升级都读盘。</summary>
        private readonly object _configUpdateLogsLock = new object();
        private Dictionary<string, ConfigUpdateLog> _cachedConfigUpdateLogs;
        private long _cachedConfigUpdateLogsFileUtcTicks = long.MinValue;

        // 文件操作重试配置
        private const int MAX_RETRY_COUNT = 3;
        private const int RETRY_DELAY_MS = 500;
        public void SetDefaultPluginInfos(List<PluginInfo> pluginInfos)
        {
            lock (_pluginInfosLock)
            {
                PluginInfos = pluginInfos;
            }
        }
        /// <summary>
        /// 启动时仅初始化运行时版本（及可选合并插件列表）；具体配置合并升级在首次读取配置时按需执行，避免枚举 private 下全部插件。
        /// </summary>
        public void CheckUpgradeConfigFiles(List<PluginInfo> pluginInfos)
        {
            try
            {
                if (pluginInfos != null && pluginInfos.Count > 0)
                {
                    lock (_pluginInfosLock)
                    {
                        PluginInfos = pluginInfos;
                    }
                }
                this.RuntimeVersion = LoadRuntimeVersion(out this.RuntimeBuildNumber);
                RPAException.RuntimeVersion = this.RuntimeVersion.ToString();
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"初始化运行时版本信息失败：{ex.Message}");
                logger.LogDebug($"详细异常：{ex}");
                throw;
            }
        }

        /// <summary>
        /// 流程包：首次拷贝（若缺）→ 按需升级 → 返回与 LoadConfigInfo 相同规则处理后的 JSON 文本（整行 // 已剔除）。无包目录或非包场景返回 null 表示调用方走运行时路径。
        /// </summary>
        public string TryGetPluginConfigJsonTextForLoad(string pluginName)
        {
            if (string.IsNullOrEmpty(pluginName))
            {
                return null;
            }
            if (!string.IsNullOrEmpty(GlobleConfigurations.PackageDirectory))
            {
                string privatePath = GetPrivateConfigFilePath(pluginName);
                if (!File.Exists(privatePath))
                {
                    string copied = FirstCopyConfigFile(pluginName);
                    if (string.IsNullOrEmpty(copied) || !File.Exists(privatePath))
                    {
                        return null;
                    }
                }
                EnsurePluginConfigUpToDate(pluginName);
                if (!TryReadConfigJsonTextStripComments(privatePath, out string text))
                {
                    return null;
                }
                if (!IsJsonObjectSyntaxValidFromText(text))
                {
                    EnsureWrittenPrivateConfigJsonValidOrRestore(privatePath);
                    if (!TryReadConfigJsonTextStripComments(privatePath, out text))
                    {
                        return null;
                    }
                }
                return text;
            }
            string runtimePath = GetRuntimeConfigFilePath(pluginName);
            if (!TryReadConfigJsonTextStripComments(runtimePath, out string runtimeText))
            {
                return null;
            }
            return runtimeText;
        }

        private void EnsureRuntimeVersionLoaded()
        {
            if (this.RuntimeVersion != null)
            {
                return;
            }
            this.RuntimeVersion = LoadRuntimeVersion(out this.RuntimeBuildNumber);
            RPAException.RuntimeVersion = this.RuntimeVersion.ToString();
        }

        /// <summary>
        /// 与 <see cref="UpdatePluginConfigFile"/> 内版本判断一致：日志已对齐当前运行时版本与构建号则无需再执行合并/拷贝升级（同一版本仅在实际升级时读写配置一次）。
        /// </summary>
        private bool IsUpgradeNeededAccordingToLog(ConfigUpdateLog log)
        {
            string verStr = log?.CurrentVersion;
            if (string.IsNullOrWhiteSpace(verStr))
            {
                verStr = "1.0.0.0";
            }
            int logBuild = log != null ? log.RuntimeBuildNumber : 0;
            Version curVersion;
            try
            {
                curVersion = new Version(verStr);
            }
            catch (ArgumentException)
            {
                return true;
            }
            catch (FormatException)
            {
                return true;
            }
            return RuntimeVersion > curVersion
                || (RuntimeVersion == curVersion && this.RuntimeBuildNumber > logBuild);
        }

        /// <summary>
        /// 单插件按需升级（持日志缓存锁，避免并发合并同一字典）。失败仅记日志，不影响读取已有文件。
        /// </summary>
        private void EnsurePluginConfigUpToDate(string pluginName)
        {
            if (string.IsNullOrEmpty(GlobleConfigurations.PackageDirectory))
            {
                return;
            }
            EnsureRuntimeVersionLoaded();
            if (RuntimeVersion < MinRuntimeVersion)
            {
                return;
            }
            PluginInfo pluginInfo;
            lock (_pluginInfosLock)
            {
                pluginInfo = PluginInfos?.FirstOrDefault(info => info.Name == pluginName);
            }
            if (pluginInfo == null)
            {
                return;
            }
            try
            {
                lock (_configUpdateLogsLock)
                {
                    Dictionary<string, ConfigUpdateLog> configUpdateLogs = GetOrReloadConfigUpdateLogsLocked();
                    configUpdateLogs.TryGetValue(pluginName, out ConfigUpdateLog existingLog);
                    if (!IsUpgradeNeededAccordingToLog(existingLog))
                    {
                        return;
                    }
                    bool updateSuccess = UpdatePluginConfigFile(
                        pluginName,
                        pluginInfo,
                        configUpdateLogs,
                        out var updateLog);
                    if (updateSuccess && updateLog != null)
                    {
                        SaveConfigUpdateLogsCore(configUpdateLogs);
                        logger.LogInformation($"插件 {pluginName} 配置文件按需更新成功");
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"插件 {pluginName} 按需配置文件更新失败，已跳过");
            }
        }

        private static bool IsJsonObjectSyntaxValidFromText(string configStr)
        {
            if (string.IsNullOrWhiteSpace(configStr))
            {
                return false;
            }
            try
            {
                JObject.Parse(configStr);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private bool TryReadConfigJsonTextStripComments(string configFilePath, out string text)
        {
            text = null;
            if (string.IsNullOrEmpty(configFilePath) || !File.Exists(configFilePath))
            {
                return false;
            }
            try
            {
                var lines = File.ReadLines(configFilePath, Encoding.UTF8).ToList();
                lines.RemoveAll(s => Regex.IsMatch(s, @"^\s*\/{2}[\s\S]*$"));
                string configStr = string.Join("", lines);
                if (string.IsNullOrWhiteSpace(configStr))
                {
                    return false;
                }
                text = configStr;
                return true;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"读取配置文件文本失败: {configFilePath}");
                return false;
            }
        }

        /// <summary>须在持有 <see cref="_configUpdateLogsLock"/> 时调用。</summary>
        private Dictionary<string, ConfigUpdateLog> GetOrReloadConfigUpdateLogsLocked()
        {
            string path = GetUpdateLogFilePath();
            if (!File.Exists(path))
            {
                _cachedConfigUpdateLogs = _cachedConfigUpdateLogs ?? new Dictionary<string, ConfigUpdateLog>();
                _cachedConfigUpdateLogsFileUtcTicks = -1;
                return _cachedConfigUpdateLogs;
            }
            long ticks = File.GetLastWriteTimeUtc(path).Ticks;
            if (_cachedConfigUpdateLogs != null && ticks == _cachedConfigUpdateLogsFileUtcTicks)
            {
                return _cachedConfigUpdateLogs;
            }
            try
            {
                string logsJson = File.ReadAllText(path, Encoding.UTF8);
                if (string.IsNullOrEmpty(logsJson))
                {
                    logger.LogInformation($"配置更新日志文件存在，但没有内容，这是非正常的情况，需要结合日志排查");
                    _cachedConfigUpdateLogs = new Dictionary<string, ConfigUpdateLog>();
                }
                else
                {
                    _cachedConfigUpdateLogs = logsJson.ToObject<Dictionary<string, ConfigUpdateLog>>() ?? new Dictionary<string, ConfigUpdateLog>();
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"加载配置更新日志失败，使用空字典: {path}");
                _cachedConfigUpdateLogs = new Dictionary<string, ConfigUpdateLog>();
            }
            _cachedConfigUpdateLogsFileUtcTicks = ticks;
            return _cachedConfigUpdateLogs;
        }

        private Dictionary<string, ConfigUpdateLog> GetOrLoadConfigUpdateLogs()
        {
            lock (_configUpdateLogsLock)
            {
                return GetOrReloadConfigUpdateLogsLocked();
            }
        }

        /// <summary>写盘后刷新缓存；外部调用需自行加锁或走 <see cref="SaveConfigUpdateLogs"/>。</summary>
        private void SaveConfigUpdateLogsCore(Dictionary<string, ConfigUpdateLog> configUpdateLogs)
        {
            string filePath = GetUpdateLogFilePath();
            SaveTextFile(filePath, configUpdateLogs.ToJson());
            if (File.Exists(filePath))
            {
                _cachedConfigUpdateLogs = configUpdateLogs;
                _cachedConfigUpdateLogsFileUtcTicks = File.GetLastWriteTimeUtc(filePath).Ticks;
            }
            else
            {
                _cachedConfigUpdateLogs = configUpdateLogs;
                _cachedConfigUpdateLogsFileUtcTicks = -1;
            }
        }
        /// <summary>
        /// 更新单个插件的配置文件（原子性操作）
        /// </summary>
        /// <param name="pluginName">插件名称</param>
        /// <param name="pluginInfo">插件信息</param>
        /// <param name="configUpdateLogs">配置更新日志字典</param>
        /// <param name="updatedLog">更新后的日志对象</param>
        /// <returns>是否更新成功</returns>
        private bool UpdatePluginConfigFile(
            string pluginName,
            PluginInfo pluginInfo,
            Dictionary<string, ConfigUpdateLog> configUpdateLogs,
            out ConfigUpdateLog updatedLog)
        {
            updatedLog = null;
            string standardConfigFilePath = this.GetStandardConfigFilePath(pluginName);
            string privateConfigFilePath = this.GetPrivateConfigFilePath(pluginName);

            // 获取或创建更新日志
            if (!configUpdateLogs.TryGetValue(pluginName, out var updateLog))
            {
                updateLog = new ConfigUpdateLog()
                {
                    PluginName = pluginName,
                    CurrentVersion = "1.0.0.0",
                    RuntimeBuildNumber = 0
                };
                configUpdateLogs[pluginName] = updateLog;
            }

            // 保存原始日志状态，用于失败时恢复
            string originalCurrentVersion = updateLog.CurrentVersion;
            int originalRuntimeBuildNumber = updateLog.RuntimeBuildNumber;
            string originalLogTime = updateLog.LogTime;

            JObject sConfig = null;
            JObject diffConfig = null;
            JObject runtimeConfig = this.LoadConfigInfo(pluginInfo.ConfigFilePath);
            JObject pConfig = this.LoadConfigInfo(privateConfigFilePath);

            // 检查必要的配置文件是否存在
            if (runtimeConfig == null)
            {
                logger.LogWarning($"插件 {pluginName} 的运行时配置文件不存在: {pluginInfo.ConfigFilePath}");
                return false;
            }
            if (pConfig == null)
            {
                logger.LogWarning($"插件 {pluginName} 的私有配置文件不存在: {privateConfigFilePath}");
                return false;
            }

            var curVersion = new Version(updateLog.CurrentVersion);
            //存在对比两份数据的版本差异，standard永远使用最新版本的数据，即与对应运行时相同版本的数据
            if (RuntimeVersion > curVersion
                || (RuntimeVersion == curVersion && this.RuntimeBuildNumber > updateLog.RuntimeBuildNumber))
            {
                sConfig = this.LoadConfigInfo(standardConfigFilePath);

                // 检查private配置文件是否与standard配置文件内容相同
                // 如果相同，说明用户没有手动修改过配置文件，直接覆盖更新即可
                bool isPrivateSameAsStandard = false;
                if (sConfig != null && pConfig != null)
                {
                    isPrivateSameAsStandard = JToken.DeepEquals(pConfig, sConfig);
                }

                if (isPrivateSameAsStandard)
                {
                    // 用户没有手动修改，直接覆盖更新
                    return UpdatePluginConfigFileDirectCopy(
                        pluginName,
                        pluginInfo,
                        privateConfigFilePath,
                        standardConfigFilePath,
                        updateLog,
                        out updatedLog);
                }

                // 如果sConfig为null，使用pConfig作为对比基准
                JObject compareConfig = sConfig ?? pConfig;
                if (compareConfig == null)
                {
                    logger.LogWarning($"插件 {pluginName} 无法找到有效的对比配置文件");
                    return false;
                }
                diffConfig = (JObject)GetJsonDifference(runtimeConfig, compareConfig);
            }
            else
            {
                return false; // 版本不需要更新
            }

            // 若不同，则更新
            if (diffConfig != null && diffConfig.HasValues)
            {
                // 创建合并后的配置副本，避免直接修改原对象
                JObject mergedConfig = (JObject)pConfig.DeepClone();
                MergeConfigJson(diffConfig, mergedConfig);

                try
                {
                    if (!TryEnsureLastGoodBackupBeforeWrite(privateConfigFilePath))
                    {
                        logger.LogWarning($"插件 {pluginName} 无法在写入前生成 lastgood 备份，跳过本次合并升级");
                        return false;
                    }
                    // 先保存private文件，如果失败会抛出异常，不会更新standard文件和日志
                    SaveTextFile(privateConfigFilePath, mergedConfig.ToString());
                    if (!EnsureWrittenPrivateConfigJsonValidOrRestore(privateConfigFilePath))
                    {
                        logger.LogError($"插件 {pluginName} 合并写入后 JSON 校验失败且无法从 {SUFFIX_LAST_GOOD} 还原: {privateConfigFilePath}");
                        throw new ApplicationException(Localize($"插件 {pluginName} 私有配置文件写入后校验失败", "ConfigFileManager_PostWriteValidateMerge"));
                    }

                    // private文件保存成功后，再拷贝standard文件
                    this.CopyStandardConfigFile(pluginInfo);

                    // 文件操作全部成功后，才更新日志（在内存中）
                    if (updateLog.UpdateHistoryList == null)
                        updateLog.UpdateHistoryList = new List<UpdateHistoryInfo>();

                    updateLog.UpdateHistoryList.Add(new UpdateHistoryInfo
                    {
                        LogTime = updateLog.LogTime,
                        UpdateVersion = updateLog.CurrentVersion,
                        RuntimeBuildNumber = updateLog.RuntimeBuildNumber,
                        MergeInfo = diffConfig.ToJsonNoneFormating()
                    });
                    updateLog.PreviousVersion = updateLog.CurrentVersion;
                    updateLog.CurrentVersion = this.RuntimeVersion.ToString();
                    updateLog.RuntimeBuildNumber = this.RuntimeBuildNumber;
                    updateLog.LogTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    updatedLog = updateLog;
                    return true;
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"插件 {pluginName} 更新private或standard文件失败，不会更新日志。尝试将 private 收敛为可解析状态");
                    EnsureWrittenPrivateConfigJsonValidOrRestore(privateConfigFilePath);
                    throw;
                }
            }
            else
            {
                // 没有差异（没有新增属性/数组元素），只更新standard文件和日志
                // 不更新private文件，保留用户手动修改的属性值
                try
                {
                    //拷贝一份最新的文件到Standard目录,覆盖拷贝
                    this.CopyStandardConfigFile(pluginInfo);

                    // standard文件更新成功后，才更新日志（在内存中）
                    if (updateLog.UpdateHistoryList == null)
                        updateLog.UpdateHistoryList = new List<UpdateHistoryInfo>();

                    updateLog.UpdateHistoryList.Add(new UpdateHistoryInfo
                    {
                        LogTime = updateLog.LogTime,
                        UpdateVersion = updateLog.CurrentVersion,
                        RuntimeBuildNumber = updateLog.RuntimeBuildNumber,
                        MergeInfo = string.Empty
                    });
                    updateLog.PreviousVersion = updateLog.CurrentVersion;
                    updateLog.CurrentVersion = this.RuntimeVersion.ToString();
                    updateLog.RuntimeBuildNumber = this.RuntimeBuildNumber;
                    updateLog.LogTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    updatedLog = updateLog;
                    logger.LogInformation($"插件 {pluginName} 配置文件无新增属性/数组元素，已更新standard文件和日志，private文件保持不变");
                    return true;
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"插件 {pluginName} 更新standard文件或日志失败");
                    throw; // 重新抛出异常，让外层处理
                }
            }
        }

        /// <summary>
        /// 直接拷贝更新插件配置文件（无手动修改的情况）
        /// </summary>
        private bool UpdatePluginConfigFileDirectCopy(
            string pluginName,
            PluginInfo pluginInfo,
            string privateConfigFilePath,
            string standardConfigFilePath,
            ConfigUpdateLog updateLog,
            out ConfigUpdateLog updatedLog)
        {
            updatedLog = null;

            try
            {
                // 检查磁盘空间
                try
                {
                    FileInfo sourceFileInfo = new FileInfo(pluginInfo.ConfigFilePath);
                    CheckDiskSpace(privateConfigFilePath, sourceFileInfo.Length);
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"检查磁盘空间时发生异常，继续执行: {privateConfigFilePath}");
                }

                if (!TryEnsureLastGoodBackupBeforeWrite(privateConfigFilePath))
                {
                    logger.LogWarning($"插件 {pluginName} 无法在覆盖前生成 lastgood 备份，跳过本次直接拷贝升级");
                    return false;
                }
                // 先更新private文件，如果失败会抛出异常
                CopyFileWithRetry(pluginInfo.ConfigFilePath, privateConfigFilePath);
                if (!EnsureWrittenPrivateConfigJsonValidOrRestore(privateConfigFilePath))
                {
                    logger.LogError($"插件 {pluginName} 直接拷贝写入后 JSON 校验失败且无法从 {SUFFIX_LAST_GOOD} 还原: {privateConfigFilePath}");
                    throw new ApplicationException(Localize($"插件 {pluginName} 私有配置文件拷贝后校验失败", "ConfigFileManager_PostWriteValidateDirect"));
                }

                // private文件更新成功后，再更新standard文件
                this.CopyStandardConfigFile(pluginInfo);

                // 文件操作全部成功后，才更新日志（在内存中）
                if (updateLog.UpdateHistoryList == null)
                    updateLog.UpdateHistoryList = new List<UpdateHistoryInfo>();

                updateLog.UpdateHistoryList.Add(new UpdateHistoryInfo
                {
                    LogTime = updateLog.LogTime,
                    UpdateVersion = updateLog.CurrentVersion,
                    RuntimeBuildNumber = updateLog.RuntimeBuildNumber,
                    MergeInfo = "{\"msg\":\"无手动修改，文件覆盖更新\"}".I18n("ConfigFileManager_DEF5D667")
                });
                updateLog.PreviousVersion = updateLog.CurrentVersion;
                updateLog.CurrentVersion = this.RuntimeVersion.ToString();
                updateLog.RuntimeBuildNumber = this.RuntimeBuildNumber;
                updateLog.LogTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                updatedLog = updateLog;
                logger.LogInformation($"插件 {pluginName} 的配置文件未手动修改，已直接覆盖更新");
                return true;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"插件 {pluginName} 直接拷贝更新失败，尝试将 private 收敛为可解析状态");
                EnsureWrittenPrivateConfigJsonValidOrRestore(privateConfigFilePath);
                throw;
            }
        }

        #region Json数据求差异、合并

        /// <summary>
        /// 获取两个JSON对象的差异（obj1相对于obj2的差异）
        /// </summary>
        /// <param name="obj1">新版本JSON对象</param>
        /// <param name="obj2">旧版本JSON对象</param>
        /// <returns>差异对象，如果无差异返回null</returns>
        private JToken GetJsonDifference(JToken obj1, JToken obj2)
        {
            // 空值检查
            if (obj1 == null && obj2 == null)
            {
                return null;
            }
            if (obj1 == null || obj2 == null)
            {
                return obj1 ?? obj2;
            }

            // 类型不同，直接返回obj1
            if (obj1.Type != obj2.Type)
            {
                return obj1;
            }

            // 提前检查是否完全相等，避免不必要的递归
            if (JToken.DeepEquals(obj1, obj2))
            {
                return null;
            }

            switch (obj1.Type)
            {
                case JTokenType.Object:
                    return GetObjectDifference((JObject)obj1, (JObject)obj2);

                case JTokenType.Array:
                    return GetArrayDifference((JArray)obj1, (JArray)obj2);

                // 基本类型：如果值不同，返回obj1
                // 使用 DeepEquals 进行精确比较，避免字符串比较的精度问题
                case JTokenType.String:
                case JTokenType.Integer:
                case JTokenType.Float:
                case JTokenType.Boolean:
                case JTokenType.Date:
                case JTokenType.Guid:
                case JTokenType.Uri:
                case JTokenType.TimeSpan:
                case JTokenType.Bytes:
                case JTokenType.Raw:
                    // 由于已经在前面用 DeepEquals 检查过，这里理论上不会执行到
                    // 但为了代码完整性，仍然保留此逻辑
                    return JToken.DeepEquals(obj1, obj2) ? null : obj1;

                case JTokenType.Null:
                    return null;

                case JTokenType.Property:
                case JTokenType.Constructor:
                case JTokenType.Comment:
                case JTokenType.None:
                default:
                    return null;
            }
        }

        /// <summary>
        /// 获取两个JSON对象的差异
        /// </summary>
        private JObject GetObjectDifference(JObject obj1, JObject obj2)
        {
            JObject diff = new JObject();
            bool hasDifference = false;

            // 遍历obj1的所有属性
            foreach (var property in obj1.Properties())
            {
                string propertyName = property.Name;

                // obj2中没有该属性，直接添加
                if (!obj2.TryGetValue(propertyName, out JToken obj2Value))
                {
                    diff[propertyName] = property.Value;
                    hasDifference = true;
                    continue;
                }

                // 递归比较属性值
                JToken nestedDiff = GetJsonDifference(property.Value, obj2Value);
                if (nestedDiff != null)
                {
                    diff[propertyName] = nestedDiff;
                    hasDifference = true;
                }
            }

            return hasDifference ? diff : null;
        }

        /// <summary>
        /// 获取两个JSON数组的差异（优化版本，使用HashSet提高性能）
        /// </summary>
        private JArray GetArrayDifference(JArray arr1, JArray arr2)
        {
            // 如果数组长度相同且都为空，直接返回null
            if (arr1.Count == 0 && arr2.Count == 0)
            {
                return null;
            }

            // 如果arr2为空，arr1中的所有元素都是差异
            if (arr2.Count == 0)
            {
                return arr1.Count > 0 ? new JArray(arr1) : null;
            }

            // 如果arr1为空，没有差异（因为我们要找arr1相对于arr2的差异）
            if (arr1.Count == 0)
            {
                return null;
            }

            // 对于小数组（小于等于10个元素），使用简单比较
            // 对于大数组，使用HashSet优化查找性能
            if (arr1.Count <= 10 && arr2.Count <= 10)
            {
                return GetArrayDifferenceSimple(arr1, arr2);
            }
            else
            {
                return GetArrayDifferenceOptimized(arr1, arr2);
            }
        }

        /// <summary>
        /// 简单数组差异比较（适用于小数组）
        /// </summary>
        private JArray GetArrayDifferenceSimple(JArray arr1, JArray arr2)
        {
            JArray diffArr = new JArray();

            for (int i = 0; i < arr1.Count; i++)
            {
                JToken item1 = arr1[i];
                bool found = false;

                for (int j = 0; j < arr2.Count; j++)
                {
                    if (JToken.DeepEquals(item1, arr2[j]))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    diffArr.Add(item1);
                }
            }

            return diffArr.Count > 0 ? diffArr : null;
        }

        /// <summary>
        /// 优化的数组差异比较（适用于大数组，使用HashSet提高查找性能）
        /// </summary>
        private JArray GetArrayDifferenceOptimized(JArray arr1, JArray arr2)
        {
            // 使用HashSet存储arr2中已匹配的索引，避免重复比较
            // 注意：由于JToken.DeepEquals可能比较耗时，我们仍然需要遍历比较
            // 但可以通过提前退出和缓存结果来优化

            JArray diffArr = new JArray();
            HashSet<int> matchedIndices = new HashSet<int>();

            for (int i = 0; i < arr1.Count; i++)
            {
                JToken item1 = arr1[i];
                bool found = false;

                // 只检查未匹配的元素
                for (int j = 0; j < arr2.Count; j++)
                {
                    if (matchedIndices.Contains(j))
                    {
                        continue;
                    }

                    if (JToken.DeepEquals(item1, arr2[j]))
                    {
                        matchedIndices.Add(j);
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    diffArr.Add(item1);
                }
            }

            return diffArr.Count > 0 ? diffArr : null;
        }
        private void MergeConfigJson(JToken diffJson, JToken pConfigJson)
        {
            if (diffJson == null || pConfigJson == null || diffJson.Type != pConfigJson.Type)
            {
                return;
            }
            switch (diffJson.Type)
            {
                case JTokenType.Object:
                    foreach (var p in ((JObject)diffJson).Properties().ToArray())
                    {
                        string propertyName = p.Name;
                        if (!((JObject)pConfigJson).ContainsKey(propertyName))
                        {
                            ((JObject)pConfigJson).Add(p);
                            continue;
                        }
                        MergeConfigJson(((JObject)diffJson)[propertyName], ((JObject)pConfigJson)[propertyName]);
                    }
                    break;
                case JTokenType.Array:
                    JArray obj1Arr = (JArray)diffJson;
                    JArray obj2Arr = (JArray)pConfigJson;
                    for (int i = 0; i < obj1Arr.Count; i++)
                    {
                        if (!obj2Arr.Any(item => JToken.DeepEquals(obj1Arr[i], item)))
                        {
                            obj2Arr.Add(obj1Arr[i]);
                        }
                    }
                    break;
                case JTokenType.None:
                    break;
                case JTokenType.Constructor:
                    break;
                case JTokenType.Property:
                    break;
                case JTokenType.Comment:
                    break;
                case JTokenType.Integer:
                    break;
                case JTokenType.Float:
                    break;
                case JTokenType.String:
                    break;
                case JTokenType.Boolean:
                    break;
                case JTokenType.Null:
                    break;
                case JTokenType.Undefined:
                    break;
                case JTokenType.Date:
                    break;
                case JTokenType.Raw:
                    break;
                case JTokenType.Bytes:
                    break;
                case JTokenType.Guid:
                    break;
                case JTokenType.Uri:
                    break;
                case JTokenType.TimeSpan:
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region 原子拷贝与 private 配置恢复（lastgood）

        private static string GetLastGoodBackupPath(string configFilePath) =>
            string.IsNullOrEmpty(configFilePath) ? null : (configFilePath + SUFFIX_LAST_GOOD);

        /// <summary>
        /// 与 LoadConfigInfo 相同的文本预处理规则下，能否解析为 JSON 对象。
        /// </summary>
        private bool IsConfigJsonFileSyntaxValid(string configFilePath)
        {
            try
            {
                if (string.IsNullOrEmpty(configFilePath) || !File.Exists(configFilePath))
                {
                    return false;
                }
                var lines = File.ReadLines(configFilePath, Encoding.UTF8).ToList();
                lines.RemoveAll(s => Regex.IsMatch(s, @"^\s*\/{2}[\s\S]*$"));
                string configStr = string.Join("", lines);
                if (string.IsNullOrWhiteSpace(configStr))
                {
                    return false;
                }
                JObject.Parse(configStr);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 写入 private 成功后调用：校验磁盘上的 JSON；无效则从 .lastgood 原子还原（升级路径低频，不在启动时全量扫目录）。
        /// </summary>
        private bool EnsureWrittenPrivateConfigJsonValidOrRestore(string privateConfigPath)
        {
            if (string.IsNullOrEmpty(privateConfigPath) || !File.Exists(privateConfigPath))
            {
                return false;
            }
            if (IsConfigJsonFileSyntaxValid(privateConfigPath))
            {
                return true;
            }
            return TryRestorePrivateConfigFromLastGood(privateConfigPath);
        }

        /// <summary>
        /// 当前文件不可解析时，从 .lastgood 还原（带重试）。返回还原后是否已通过语法校验。
        /// </summary>
        private bool TryRestorePrivateConfigFromLastGood(string privateConfigPath)
        {
            string lastGood = GetLastGoodBackupPath(privateConfigPath);
            if (string.IsNullOrEmpty(lastGood) || !File.Exists(lastGood) || !IsConfigJsonFileSyntaxValid(lastGood))
            {
                logger.LogError($"private 配置文件无效且缺少可用的 {SUFFIX_LAST_GOOD} 备份: {privateConfigPath}");
                return false;
            }
            logger.LogWarning($"private 配置语法校验未通过，尝试从 {SUFFIX_LAST_GOOD} 还原: {privateConfigPath}");
            int retry = 0;
            Exception lastEx = null;
            while (retry < MAX_RETRY_COUNT)
            {
                try
                {
                    CopyFileAtomicVerified(lastGood, privateConfigPath);
                    if (IsConfigJsonFileSyntaxValid(privateConfigPath))
                    {
                        logger.LogInformation($"已从 {SUFFIX_LAST_GOOD} 成功还原 private 配置: {privateConfigPath}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"从 {SUFFIX_LAST_GOOD} 还原 private 配置失败，重试 {retry + 1}/{MAX_RETRY_COUNT}: {privateConfigPath}");
                }
                retry++;
                if (retry < MAX_RETRY_COUNT)
                {
                    Thread.Sleep(RETRY_DELAY_MS * retry);
                }
            }
            Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, lastEx, $"从 {SUFFIX_LAST_GOOD} 还原 private 配置在重试后仍失败: {privateConfigPath}");
            return false;
        }

        /// <summary>
        /// 在修改 private 配置前写入 .lastgood；失败则返回 false，调用方应放弃本次写入（接受本轮升级无效）。
        /// </summary>
        private bool TryEnsureLastGoodBackupBeforeWrite(string privateConfigPath)
        {
            try
            {
                if (string.IsNullOrEmpty(privateConfigPath) || !File.Exists(privateConfigPath))
                {
                    return true;
                }
                if (!IsConfigJsonFileSyntaxValid(privateConfigPath))
                {
                    logger.LogWarning($"private 配置当前无法通过语法校验，跳过 lastgood 备份: {privateConfigPath}");
                    return false;
                }
                string lastGood = GetLastGoodBackupPath(privateConfigPath);
                CopyFileAtomicVerified(privateConfigPath, lastGood);
                return IsConfigJsonFileSyntaxValid(lastGood);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"写入 {SUFFIX_LAST_GOOD} 失败，本次放弃变更 private 配置: {privateConfigPath}");
                return false;
            }
        }

        private void TryDeleteFileQuiet(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return;
            }
            try
            {
                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"删除临时/备份文件失败（可忽略）: {filePath}");
            }
        }

        /// <summary>
        /// 将已通过完整性校验的临时文件原子提交为目标路径（先 Replace 再删 .bak，避免原地写坏目标）。
        /// </summary>
        private void CommitVerifiedTempToTargetPath(string verifiedTempPath, string targetPath)
        {
            if (string.IsNullOrEmpty(verifiedTempPath) || !File.Exists(verifiedTempPath))
            {
                throw new FileNotFoundException(Localize($"临时文件不存在，无法提交: {verifiedTempPath}", "ConfigFileManager_CommitVerifiedTemp"));
            }
            string bakPath = targetPath + ".bak";
            if (File.Exists(targetPath))
            {
                File.Replace(verifiedTempPath, targetPath, bakPath, ignoreMetadataErrors: false);
                TryDeleteFileQuiet(bakPath);
            }
            else
            {
                File.Move(verifiedTempPath, targetPath);
            }
        }

        /// <summary>
        /// 单次尝试：拷贝到同目录随机临时文件 → 校验 → 原子替换目标（非原地覆盖）。
        /// </summary>
        private void CopyFileAtomicVerified(string sourcePath, string targetPath)
        {
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            {
                throw new FileNotFoundException(Localize($"源文件不存在: {sourcePath}", "ConfigFileManager_CopyAtomicSource"));
            }
            if (string.IsNullOrEmpty(targetPath))
            {
                throw new ArgumentException(nameof(targetPath));
            }
            string targetDir = Path.GetDirectoryName(targetPath);
            if (string.IsNullOrEmpty(targetDir))
            {
                throw new ArgumentException(nameof(targetPath));
            }
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }
            FileInfo srcInfo = new FileInfo(sourcePath);
            CheckDiskSpace(targetPath, srcInfo.Length);
            string tempPath = Path.Combine(targetDir, Path.GetRandomFileName());
            try
            {
                File.Copy(sourcePath, tempPath, overwrite: false);
                VerifyFileCopyIntegrity(sourcePath, tempPath);
                CommitVerifiedTempToTargetPath(tempPath, targetPath);
                tempPath = null;
            }
            finally
            {
                if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
                {
                    TryDeleteFileQuiet(tempPath);
                }
            }
        }

        #endregion

        #region 拷贝标准文件

        private string CopyStandardConfigFile(PluginInfo pluginInfo)
        {
            if (pluginInfo == null || string.IsNullOrEmpty(pluginInfo.ConfigFilePath))
            {
                logger.LogWarning("插件信息为空或配置文件路径为空，无法拷贝标准配置文件");
                return null;
            }
            if (!File.Exists(pluginInfo.ConfigFilePath))
            {
                logger.LogWarning($"源配置文件不存在: {pluginInfo.ConfigFilePath}，无法拷贝标准配置文件");
                return null;
            }

            string standardConfigFilePath = this.GetStandardConfigFilePath(pluginInfo.Name);
            //拷贝一份文件到Standard目录
            string standardPath = Path.GetDirectoryName(standardConfigFilePath);
            if (!Directory.Exists(standardPath))
            {
                try
                {
                    Directory.CreateDirectory(standardPath);
                }
                catch (UnauthorizedAccessException ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"创建标准配置目录失败，无权限访问: {standardPath}");
                    throw new ApplicationException(Localize($"创建标准配置目录失败，无权限访问: {standardPath}", "ConfigFileManager_A70936BD"), ex);
                }
                catch (IOException ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"创建标准配置目录失败，IO异常: {standardPath}");
                    throw new ApplicationException(Localize($"创建标准配置目录失败: {standardPath}", "ConfigFileManager_6E51949F"), ex);
                }
            }

            try
            {
                FileInfo sourceFileInfo = new FileInfo(pluginInfo.ConfigFilePath);
                CheckDiskSpace(standardConfigFilePath, sourceFileInfo.Length);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"检查磁盘空间时发生异常，继续执行: {standardConfigFilePath}");
            }

            CopyFileWithRetry(pluginInfo.ConfigFilePath, standardConfigFilePath);
            return standardConfigFilePath;
        }

        /// <summary>
        /// 验证文件拷贝的完整性
        /// </summary>
        private void VerifyFileCopyIntegrity(string sourceFilePath, string targetFilePath)
        {
            try
            {
                if (!File.Exists(targetFilePath))
                {
                    throw new FileNotFoundException(Localize($"目标文件不存在，拷贝验证失败: {targetFilePath}", "controlType_ConfigFileManager_EB3F27E3"));
                }

                FileInfo sourceInfo = new FileInfo(sourceFilePath);
                FileInfo targetInfo = new FileInfo(targetFilePath);

                // 验证文件大小
                if (sourceInfo.Length != targetInfo.Length)
                {
                    throw new InvalidDataException(Localize($"文件大小不匹配，拷贝验证失败。源文件: {sourceInfo.Length} 字节，目标文件: {targetInfo.Length} 字节", "controlType_ConfigFileManager_99DA8C71"));
                }

                // 对于小文件，可以进一步验证内容
                if (sourceInfo.Length < 1024 * 1024) // 小于1MB的文件
                {
                    byte[] sourceBytes = File.ReadAllBytes(sourceFilePath);
                    byte[] targetBytes = File.ReadAllBytes(targetFilePath);

                    if (!sourceBytes.SequenceEqual(targetBytes))
                    {
                        throw new InvalidDataException(Localize($"文件内容不匹配，拷贝验证失败: {targetFilePath}", "controlType_ConfigFileManager_E19C2816"));
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"文件拷贝完整性验证失败，源文件: {sourceFilePath}，目标文件: {targetFilePath}");
                throw;
            }
        }

        #endregion

        #region 拷贝配置文件，从运行时目录拷贝至流程目录的private

        public string FirstCopyConfigFile(string pluginName)
        {
            string runtimeConfigFilePath = this.GetRuntimeConfigFilePath(pluginName);
            string privateConfigFilePath = this.GetPrivateConfigFilePath(pluginName);
            string standardConfigFilePath = this.GetStandardConfigFilePath(pluginName);
            if (File.Exists(privateConfigFilePath))
            {
                return privateConfigFilePath;
            }
            if (!File.Exists(runtimeConfigFilePath))
            {
                return null;
            }
            string privateConfigDir = Path.GetDirectoryName(privateConfigFilePath);
            if (!Directory.Exists(privateConfigDir))
            {
                try
                {
                    Directory.CreateDirectory(privateConfigDir);
                }
                catch (UnauthorizedAccessException ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"创建私有配置目录失败，无权限访问: {privateConfigDir}");
                    throw new ApplicationException(Localize($"创建私有配置目录失败，无权限访问: {privateConfigDir}", "controlType_ConfigFileManager_E1CD159A"), ex);
                }
                catch (IOException ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"创建私有配置目录失败，IO异常: {privateConfigDir}");
                    throw new ApplicationException(Localize($"创建私有配置目录失败: {privateConfigDir}", "controlType_ConfigFileManager_39043780"), ex);
                }
            }

            // 检查磁盘空间
            try
            {
                FileInfo sourceFileInfo = new FileInfo(runtimeConfigFilePath);
                CheckDiskSpace(privateConfigFilePath, sourceFileInfo.Length);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"检查磁盘空间时发生异常，继续执行: {privateConfigFilePath}");
            }

            // 带重试的文件拷贝
            CopyFileWithRetry(runtimeConfigFilePath, privateConfigFilePath);
            if (!EnsureWrittenPrivateConfigJsonValidOrRestore(privateConfigFilePath))
            {
                logger.LogWarning($"首次拷贝 private 后 JSON 校验失败，尝试从运行时重新拷贝一次: {privateConfigFilePath}");
                CopyFileWithRetry(runtimeConfigFilePath, privateConfigFilePath);
                if (!EnsureWrittenPrivateConfigJsonValidOrRestore(privateConfigFilePath))
                {
                    throw new ApplicationException(Localize($"首次拷贝私有配置文件后校验失败: {privateConfigFilePath}", "ConfigFileManager_FirstCopyValidate"));
                }
            }

            // 确保RuntimeVersion已初始化
            if (this.RuntimeVersion == null)
            {
                this.RuntimeVersion = LoadRuntimeVersion(out this.RuntimeBuildNumber);
            }

            if (RuntimeVersion < MinRuntimeVersion)
            {
                return privateConfigFilePath;
            }

            string standardConfigDir = Path.GetDirectoryName(standardConfigFilePath);
            if (!Directory.Exists(standardConfigDir))
            {
                try
                {
                    Directory.CreateDirectory(standardConfigDir);
                }
                catch (UnauthorizedAccessException ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"创建标准配置目录失败，无权限访问: {standardConfigDir}");
                    throw new ApplicationException(Localize($"创建标准配置目录失败，无权限访问: {standardConfigDir}", "controlType_ConfigFileManager_784AF295"), ex);
                }
                catch (IOException ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"创建标准配置目录失败，IO异常: {standardConfigDir}");
                    throw new ApplicationException(Localize($"创建标准配置目录失败: {standardConfigDir}", "controlType_ConfigFileManager_CBEA3CDB"), ex);
                }
            }

            // 检查磁盘空间
            try
            {
                FileInfo sourceFileInfo = new FileInfo(runtimeConfigFilePath);
                CheckDiskSpace(standardConfigFilePath, sourceFileInfo.Length);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"检查磁盘空间时发生异常，继续执行: {standardConfigFilePath}");
            }

            // 带重试的文件拷贝
            CopyFileWithRetry(runtimeConfigFilePath, standardConfigFilePath);
            var configUpdateLogs = GetOrLoadConfigUpdateLogs();
            ConfigUpdateLog updateLog = new ConfigUpdateLog()
            {
                PluginName = pluginName,
                CurrentVersion = "1.0.0.0",
                RuntimeBuildNumber = 0
            };
            //更新完成之后，记录更新数据
            if (updateLog.UpdateHistoryList == null)
            {
                updateLog.UpdateHistoryList = new List<UpdateHistoryInfo>();
            }
            updateLog.UpdateHistoryList.Add(new UpdateHistoryInfo
            {
                LogTime = updateLog.LogTime,
                UpdateVersion = updateLog.CurrentVersion,
                RuntimeBuildNumber = updateLog.RuntimeBuildNumber
            });
            updateLog.PreviousVersion = updateLog.CurrentVersion;
            updateLog.RuntimeBuildNumber = this.RuntimeBuildNumber;
            updateLog.CurrentVersion = this.RuntimeVersion.ToString();
            updateLog.LogTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            configUpdateLogs[pluginName] = updateLog;
            this.SaveConfigUpdateLogs(configUpdateLogs);
            return privateConfigFilePath;
        }
        #endregion

        #region 加载配置文件

        private JObject LoadConfigInfo(string configFilePath)
        {
            if (string.IsNullOrEmpty(configFilePath) || !File.Exists(configFilePath))
            {
                return null;
            }
            try
            {
                var lines = File.ReadLines(configFilePath, System.Text.Encoding.UTF8).ToList();
                lines.RemoveAll(s => Regex.IsMatch(s, @"^\s*\/{2}[\s\S]*$"));
                string configStr = string.Join("", lines);
                if (string.IsNullOrWhiteSpace(configStr))
                {
                    logger.LogWarning($"配置文件为空: {configFilePath}");
                    return null;
                }
                return JObject.Parse(configStr);
            }
            catch (JsonException ex)
            {
                logger.LogError($"解析配置文件失败: {configFilePath}, 错误: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError($"读取配置文件失败: {configFilePath}, 错误: {ex.Message}");
                throw;
            }
        }
        #endregion

        #region 保存配置文件更新日志
        private void SaveConfigUpdateLogs(Dictionary<string, ConfigUpdateLog> configUpdateLogs)
        {
            lock (_configUpdateLogsLock)
            {
                SaveConfigUpdateLogsCore(configUpdateLogs);
            }
        }
        #endregion

        #region 加载运行时版本（当前进程程序集）

        private Version LoadRuntimeVersion(out int buildNumber)
        {
            buildNumber = 0;
            try
            {
                Assembly asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                Version v = asm?.GetName()?.Version;
                if (v == null)
                {
                    logger.LogWarning("无法从入口或当前程序集获取版本号，使用默认版本 1.0.0.0");
                    return new Version("1.0.0.0");
                }

                buildNumber = v.Revision >= 0 ? v.Revision : v.Build;

                logger.LogDebug($"运行时版本取自程序集 {asm.FullName}: {v}, BuildNumber={buildNumber}");
                return v;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "从程序集加载运行时版本失败，使用默认版本 1.0.0.0");
                return new Version("1.0.0.0");
            }
        }

        #endregion

        #region 保存文件信息
        public void SaveTextFile(string targetFilePath, string content)
        {
            if (string.IsNullOrEmpty(targetFilePath))
            {
                throw new ArgumentException("目标文件路径不能为空".I18n("controlType_ConfigFileManager_413A0EC8"), nameof(targetFilePath));
            }
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content), "文件内容不能为null".I18n("controlType_ConfigFileManager_17E417"));
            }

            // 检查磁盘空间
            CheckDiskSpace(targetFilePath, content.Length);

            string targetDirectory = Path.GetDirectoryName(targetFilePath);
            if (!Directory.Exists(targetDirectory))
            {
                try
                {
                    Directory.CreateDirectory(targetDirectory);
                }
                catch (UnauthorizedAccessException ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"创建目录失败，无权限访问: {targetDirectory}");
                    throw new ApplicationException(Localize($"创建目录失败，无权限访问: {targetDirectory}", "controlType_ConfigFileManager_E26C6741"), ex);
                }
                catch (IOException ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"创建目录失败，IO异常: {targetDirectory}");
                    throw new ApplicationException(Localize($"创建目录失败: {targetDirectory}", "controlType_ConfigFileManager_EB7B8D6F"), ex);
                }
            }

            string tempFileName = Path.GetRandomFileName(); // 生成一个随机文件名
            string tempFilePath = Path.Combine(targetDirectory, tempFileName);
            string bakFilePath = $"{targetFilePath}.bak";

            int retryCount = 0;
            Exception lastException = null;

            while (retryCount < MAX_RETRY_COUNT)
            {
                try
                {
                    // 写入临时文件
                    WriteFileWithRetry(tempFilePath, content);

                    // 验证临时文件完整性
                    VerifyFileIntegrity(tempFilePath, content);

                    // 替换或移动文件
                    if (File.Exists(targetFilePath))
                    {
                        File.Replace(tempFilePath, targetFilePath, bakFilePath, ignoreMetadataErrors: false);
                    }
                    else
                    {
                        File.Move(tempFilePath, targetFilePath);
                    }

                    // 验证目标文件完整性
                    VerifyFileIntegrity(targetFilePath, content);

                    // 删除备份文件
                    if (File.Exists(targetFilePath) && File.Exists(bakFilePath))
                    {
                        try
                        {
                            File.Delete(bakFilePath);
                        }
                        catch (Exception ex)
                        {
                            Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"删除备份文件失败: {bakFilePath}，但不影响主文件保存");
                        }
                    }

                    // 保存成功，退出重试循环
                    return;
                }
                catch (UnauthorizedAccessException ex)
                {
                    lastException = ex;
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"保存文件失败（无权限），重试 {retryCount + 1}/{MAX_RETRY_COUNT}，目标文件路径: {targetFilePath}");
                    RollbackFiles(tempFilePath, targetFilePath, bakFilePath);
                }
                catch (IOException ex)
                {
                    lastException = ex;
                    // 检查是否是磁盘空间不足
                    if (IsDiskFullException(ex))
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"保存文件失败（磁盘空间不足），目标文件路径: {targetFilePath}");
                        RollbackFiles(tempFilePath, targetFilePath, bakFilePath);
                        throw new ApplicationException(Localize($"保存文件失败，磁盘空间不足: {targetFilePath}", "controlType_ConfigFileManager_10DF72F4"), ex);
                    }
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"保存文件失败（IO异常），重试 {retryCount + 1}/{MAX_RETRY_COUNT}，目标文件路径: {targetFilePath}");
                    RollbackFiles(tempFilePath, targetFilePath, bakFilePath);
                }
                catch (OutOfMemoryException ex)
                {
                    lastException = ex;
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"保存文件失败（内存不足），目标文件路径: {targetFilePath}");
                    RollbackFiles(tempFilePath, targetFilePath, bakFilePath);
                    throw new ApplicationException(Localize($"保存文件失败，系统内存不足: {targetFilePath}", "controlType_ConfigFileManager_A00C3518"), ex);
                }
                catch (SecurityException ex)
                {
                    lastException = ex;
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"保存文件失败（安全异常），目标文件路径: {targetFilePath}");
                    RollbackFiles(tempFilePath, targetFilePath, bakFilePath);
                    throw new ApplicationException(Localize($"保存文件失败，权限不足: {targetFilePath}", "controlType_ConfigFileManager_44FB3570"), ex);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"保存文件失败（未知异常），重试 {retryCount + 1}/{MAX_RETRY_COUNT}，目标文件路径: {targetFilePath}");
                    RollbackFiles(tempFilePath, targetFilePath, bakFilePath);
                }

                retryCount++;
                if (retryCount < MAX_RETRY_COUNT)
                {
                    Thread.Sleep(RETRY_DELAY_MS * retryCount); // 递增延迟
                }
            }

            // 所有重试都失败
            Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, lastException, $"保存文件失败，已重试 {MAX_RETRY_COUNT} 次，目标文件路径: {targetFilePath}");
            throw new ApplicationException(Localize($"保存文件失败，已重试 {MAX_RETRY_COUNT} 次: {targetFilePath}", "controlType_ConfigFileManager_91BE360F"), lastException);
        }

        /// <summary>
        /// 检查磁盘空间是否足够
        /// </summary>
        private void CheckDiskSpace(string filePath, long requiredBytes)
        {
            try
            {
                string drive = Path.GetPathRoot(filePath);
                if (string.IsNullOrEmpty(drive))
                {
                    return; // 无法确定驱动器，跳过检查
                }

                DriveInfo driveInfo = new DriveInfo(drive);
                if (driveInfo.IsReady)
                {
                    long availableSpace = driveInfo.AvailableFreeSpace;
                    // 需要额外保留一些空间（例如文件大小的2倍，用于临时文件和备份文件）
                    long requiredSpace = requiredBytes * 3;

                    if (availableSpace < requiredSpace)
                    {
                        string errorMsg = Localize($"磁盘空间不足，需要 {requiredSpace / 1024 / 1024} MB，可用 {availableSpace / 1024 / 1024} MB，驱动器: {drive}", "ConfigFileManager_A7CC37C9");
                        logger.LogError(errorMsg);
                        throw new IOException(errorMsg);
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"检查磁盘空间时发生异常，继续执行: {filePath}");
                // 检查失败不影响主流程，继续执行
            }
        }

        /// <summary>
        /// 判断是否是磁盘空间不足异常
        /// </summary>
        private bool IsDiskFullException(IOException ex)
        {
            // Windows错误代码: 112 (ERROR_DISK_FULL) = 0x70
            // 或者通过异常消息判断
            string message = ex.Message ?? string.Empty;
            return message.Contains("磁盘空间不足")
                || message.IndexOf("disk full", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("not enough space", StringComparison.OrdinalIgnoreCase) >= 0
                || ex.HResult == unchecked((int)0x80070070);
        }

        /// <summary>
        /// 带重试的文件写入
        /// </summary>
        private void WriteFileWithRetry(string filePath, string content)
        {
            int retryCount = 0;
            Exception lastException = null;

            while (retryCount < MAX_RETRY_COUNT)
            {
                try
                {
                    File.WriteAllText(filePath, content, Encoding.UTF8);
                    return; // 成功写入
                }
                catch (UnauthorizedAccessException ex)
                {
                    lastException = ex;
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"写入文件失败（无权限），重试 {retryCount + 1}/{MAX_RETRY_COUNT}: {filePath}");
                }
                catch (IOException ex)
                {
                    lastException = ex;
                    if (IsDiskFullException(ex))
                    {
                        throw; // 磁盘空间不足，直接抛出
                    }
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"写入文件失败（IO异常），重试 {retryCount + 1}/{MAX_RETRY_COUNT}: {filePath}");
                }
                catch (OutOfMemoryException ex)
                {
                    throw; // 内存不足，直接抛出
                }

                retryCount++;
                if (retryCount < MAX_RETRY_COUNT)
                {
                    Thread.Sleep(RETRY_DELAY_MS * retryCount);
                }
            }

            throw new ApplicationException(Localize($"写入文件失败，已重试 {MAX_RETRY_COUNT} 次: {filePath}", "controlType_ConfigFileManager_6B5FB58B"), lastException);
        }

        /// <summary>
        /// 验证文件完整性
        /// </summary>
        private void VerifyFileIntegrity(string filePath, string expectedContent)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException(Localize($"文件不存在，完整性验证失败: {filePath}", "controlType_ConfigFileManager_28F50FF2"));
                }

                // 读取文件内容进行验证
                string actualContent = File.ReadAllText(filePath, Encoding.UTF8);
                if (actualContent != expectedContent)
                {
                    throw new InvalidDataException(Localize($"文件内容不匹配，完整性验证失败: {filePath}", "controlType_ConfigFileManager_718E973E"));
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"文件完整性验证失败: {filePath}");
                throw;
            }
        }
        /// <summary>
        /// 带重试的文件拷贝
        /// </summary>
        private void CopyFileWithRetry(string sourceFilePath, string targetFilePath)
        {
            int retryCount = 0;
            Exception lastException = null;

            while (retryCount < MAX_RETRY_COUNT)
            {
                try
                {
                    CopyFileAtomicVerified(sourceFilePath, targetFilePath);
                    return;
                }
                catch (UnauthorizedAccessException ex)
                {
                    lastException = ex;
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"拷贝文件失败（无权限），重试 {retryCount + 1}/{MAX_RETRY_COUNT}，源文件: {sourceFilePath}，目标文件: {targetFilePath}");
                }
                catch (IOException ex)
                {
                    lastException = ex;
                    if (IsDiskFullException(ex))
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"拷贝文件失败（磁盘空间不足），源文件: {sourceFilePath}，目标文件: {targetFilePath}");
                        throw new ApplicationException(Localize($"拷贝文件失败，磁盘空间不足: {targetFilePath}", "controlType_ConfigFileManager_C357839D"), ex);
                    }
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"拷贝文件失败（IO异常），重试 {retryCount + 1}/{MAX_RETRY_COUNT}，源文件: {sourceFilePath}，目标文件: {targetFilePath}");
                }
                catch (OutOfMemoryException ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"拷贝文件失败（内存不足），源文件: {sourceFilePath}，目标文件: {targetFilePath}");
                    throw new ApplicationException(Localize($"拷贝文件失败，系统内存不足: {targetFilePath}", "controlType_ConfigFileManager_3C8BBF15"), ex);
                }
                catch (SecurityException ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"拷贝文件失败（安全异常），源文件: {sourceFilePath}，目标文件: {targetFilePath}");
                    throw new ApplicationException(Localize($"拷贝文件失败，权限不足: {targetFilePath}", "controlType_ConfigFileManager_89A9EE99"), ex);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"拷贝文件失败（未知异常），重试 {retryCount + 1}/{MAX_RETRY_COUNT}，源文件: {sourceFilePath}，目标文件: {targetFilePath}");
                }

                retryCount++;
                if (retryCount < MAX_RETRY_COUNT)
                {
                    Thread.Sleep(RETRY_DELAY_MS * retryCount);
                }
            }

            Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, lastException, $"拷贝文件失败，已重试 {MAX_RETRY_COUNT} 次，源文件: {sourceFilePath}，目标文件: {targetFilePath}");
            throw new ApplicationException(Localize($"拷贝文件失败，已重试 {MAX_RETRY_COUNT} 次: {targetFilePath}", "controlType_ConfigFileManager_FCB86E78"), lastException);
        }

        private void RollbackFiles(string tempFilePath, string targetFilePath, string bakFilePath)
        {
            try
            {
                // 删除临时文件
                if (File.Exists(tempFilePath))
                {
                    try
                    {
                        File.Delete(tempFilePath);
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"删除临时文件失败: {tempFilePath}");
                    }
                }
                // 如果存在备份文件，尝试恢复
                if (File.Exists(bakFilePath))
                {
                    try
                    {
                        // 如果目标文件存在，先删除
                        if (File.Exists(targetFilePath))
                        {
                            File.Delete(targetFilePath);
                        }
                        // 恢复备份文件
                        File.Move(bakFilePath, targetFilePath);
                        logger.LogInformation($"已从备份文件恢复: {targetFilePath}");
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"恢复备份文件失败: {bakFilePath} -> {targetFilePath}");
                    }
                }
                else if (File.Exists(targetFilePath))
                {
                    // 如果没有备份文件但目标文件存在，说明是新文件，删除它
                    try
                    {
                        File.Delete(targetFilePath);
                        logger.LogInformation($"已删除损坏的目标文件: {targetFilePath}");
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"删除损坏的目标文件失败: {targetFilePath}");
                    }
                }
            }
            catch (Exception ex2)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex2, $"执行文件回滚动作发生异常");
                logger.LogInformation($"临时文件路径：{tempFilePath}");
                logger.LogInformation($"目标文件路径：{targetFilePath}");
                logger.LogInformation($"备份文件路径：{bakFilePath}");
            }
        }
        #endregion
    }

    public class ConfigUpdateLog
    {
        public string PluginName { get; set; } = string.Empty;
        public string CurrentVersion { get; set; } = string.Empty;
        public int RuntimeBuildNumber { get; set; } = 0;
        public string PreviousVersion { get; set; } = string.Empty;
        public string LogTime { get; set; } = string.Empty;
        public List<UpdateHistoryInfo> UpdateHistoryList { get; set; } = new List<UpdateHistoryInfo>();
    }
    public class UpdateHistoryInfo
    {
        public string LogTime { get; set; } = string.Empty;
        public string UpdateVersion { get; set; } = string.Empty;
        public int RuntimeBuildNumber { get; set; } = 0;
        public string MergeInfo { get; set; } = string.Empty;
    }
}


