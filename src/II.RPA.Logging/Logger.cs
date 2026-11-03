using II.RPA.Logging.Configurations;
using II.RPA.Logging.Definitions;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Logging
{
    public static class Logger
    {
        private static SwitchableLogger switchableLogger;
        private static readonly object configurationLock = new object();
        private static AppConfigWatcher configWatcher;
        private static Func<Serilog.Core.Logger> createConfiguredLogger;

        private static ILoggerFactory factory;

        private static int LogHoldDays = 7;

        public static LogLevel Level { get; set; }
        static Logger()
        {
#if DEBUG
            LogHoldDays = 1;
            //调试模式下报告Serilog自身的错误，同名进程启动会产生冲突
            try
            {
                var processName = Process.GetCurrentProcess().ProcessName;
                var appDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var folder = Path.Combine(appDir, LoggingRoles.Default, "Logs", "Dev");
                var errorFallbackFile = Path.Combine(folder, $"{processName}-fallback.txt");

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                var writer = File.CreateText(errorFallbackFile);
                Serilog.Debugging.SelfLog.Enable(writer);
            }
            catch
            {

            }
#endif

            Log.Logger = switchableLogger = new SwitchableLogger();
            factory = new LoggerFactory();
            factory.AddSerilog();
        }

        public static void ConfigWithRole(string productName)
        {
            //Serilog.Log.CloseAndFlush();
            createConfiguredLogger = () => CreateForChange(productName);
            switchableLogger.Set(CreateLogger(productName, out var ex), true);
            Level = LogSettingsHelper.CurrentSettings.Level;

            if (ex != null)
            {
                OfContext().LogWarning(EventIds.WarningDefault, ex, "加载日志配置出错，使用了默认的日志配置");
            }

            //跟踪配置文件变更
            ReplaceConfigWatcher(new AppConfigWatcher(LogSettingsHelper.LogConfigFilePath, idle =>
            {
                if (idle)
                {
                    lock (configurationLock)
                    {
                        //ConfigurationManager.RefreshSection(ConfigKeys.AppSettingsSection);
                        if (!LogSettingsHelper.TryRefresh())
                            throw new IOException("日志配置重载失败，等待下一次重试");
                        switchableLogger.Set(CreateLogger(productName, out var ex2), true);

                        if (ex2 != null)
                        {
                            OfContext().LogWarning(EventIds.WarningDefault, ex2, "加载日志配置出错，使用了默认的日志配置");
                        }
                        Level = LogSettingsHelper.CurrentSettings.Level;
                    }
                }
            }));
        }

        public static void ConfigWithRole(
            string productName,
            string rootDir)
        {
            //var rootDirConfig = ConfigurationHelper.TryReadConfiguration(ConfigKeys.RootDir);
            var rootDirConfig = LogSettingsHelper.CurrentSettings.RootPath;


            if (string.IsNullOrEmpty(rootDirConfig))
            {
                rootDirConfig = rootDir;
            }

            createConfiguredLogger = () => CreateForChange(productName, rootDirConfig);
            switchableLogger.Set(CreateLogger(productName, out var ex, rootDirConfig), true);
            Level = LogSettingsHelper.CurrentSettings.Level;

            if (ex != null)
            {
                OfContext().LogWarning(EventIds.WarningDefault, ex, "加载日志配置出错，使用了默认的日志配置");
            }

            //跟踪配置文件变更
            ReplaceConfigWatcher(new AppConfigWatcher(LogSettingsHelper.LogConfigFilePath, idle =>
            {
                if (idle)
                {
                    lock (configurationLock)
                    {
                        if (!LogSettingsHelper.TryRefresh())
                            throw new IOException("日志配置重载失败，等待下一次重试");
                        var holdDaysConfigvar = LogSettingsHelper.CurrentSettings.RemainDay;
                        switchableLogger.Set(CreateLogger(productName, out var ex2, rootDirConfig), true);

                        if (ex2 != null)
                        {
                            OfContext().LogWarning(EventIds.WarningDefault, ex2, "加载日志配置出错，使用了默认的日志配置");
                        }
                        Level = LogSettingsHelper.CurrentSettings.Level;
                    }
                }
            }));
        }

        public static void ConfigCoreServerWithRole(string productName)
        {
            //Serilog.Log.CloseAndFlush();
            createConfiguredLogger = () => CreateForChange(productName, coreServer: true);
            switchableLogger.Set(CreateCoreServerLogger(productName, out var ex), true);
            Level = LogSettingsHelper.CurrentSettings.Level;

            if (ex != null)
            {
                OfContext().LogWarning(EventIds.WarningDefault, ex, "加载日志配置出错，使用了默认的日志配置");
            }

            //跟踪配置文件变更
            ReplaceConfigWatcher(new AppConfigWatcher(LogSettingsHelper.LogConfigFilePath, idle =>
            {
                if (idle)
                {
                    lock (configurationLock)
                    {
                        if (!LogSettingsHelper.TryRefresh())
                            throw new IOException("日志配置重载失败，等待下一次重试");
                        switchableLogger.Set(CreateCoreServerLogger(productName, out var ex2), true);

                        if (ex2 != null)
                        {
                            OfContext().LogWarning(EventIds.WarningDefault, ex2, "加载日志配置出错，使用了默认的日志配置");
                        }
                        Level = LogSettingsHelper.CurrentSettings.Level;
                    }
                }
            }));
        }

        private static void ReplaceConfigWatcher(AppConfigWatcher watcher)
        {
            AppConfigWatcher previous;
            lock (configurationLock)
            {
                previous = configWatcher;
                configWatcher = watcher;
            }
            previous?.Dispose();
        }

        // Explicit requests must report failure; startup keeps its original fallback behavior.
        private static Serilog.Core.Logger CreateForChange(
            string productName, string rootDir = null, bool coreServer = false)
        {
            Exception exception;
            var logger = coreServer
                ? CreateCoreServerLogger(productName, out exception)
                : CreateLogger(productName, out exception, rootDir);
            if (exception == null) return logger;
            OfContext().LogWarning(EventIds.WarningDefault, exception,
                "加载日志配置出错，当前日志配置未应用");
            logger.Dispose();
            throw new InvalidOperationException("日志配置应用失败", exception);
        }

        private static void ApplyCurrentSettings()
        {
            if (createConfiguredLogger == null) return;
            switchableLogger.Set(createConfiguredLogger(), true);
            Level = LogSettingsHelper.CurrentSettings.Level;
        }

        private static Serilog.Core.Logger CreateLogger(
            string productName,
            out Exception exception,
            string rootDir = null)
        {
            var processName = Process.GetCurrentProcess().ProcessName;

            if (string.IsNullOrEmpty(rootDir))
            {
                rootDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            }
            var settings = LogSettingsHelper.CurrentSettings;

            var remainFileCount = settings.RemainFileCount;
            var remainFileDay = TimeSpan.FromDays(settings.RemainDay);

            var configuration = new LoggerConfiguration()
              .Enrich.FromLogContext()
              .Enrich.WithProcessId()
              .Enrich.WithThreadId()
              .WriteTo.Map(
                e => DateTime.Now.ToString("yyyy-MM-dd"),
                (t, wt) => wt.File(
                    Path.Combine(rootDir, productName, "logs", t, "dll_logs", $"{processName}.txt"),
                    shared: true,
                    rollOnFileSizeLimit: true,
                    /*每个进程每天最多仅允许产生500 * 10M日志文件（默认）
                    以C盘40G空闲空间为例，8日左右就会耗尽*/
                    retainedFileCountLimit: remainFileCount,
                    retainedFileTimeLimit: remainFileDay,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} | {Level:u3} | {ProcessId} | {ThreadId:0000} | {SourceContext} | {Message:lj}{NewLine}{Exception}",
                    fileSizeLimitBytes: 10 * 1024 * 1024,
                    flushToDiskInterval: TimeSpan.FromSeconds(15)
                //rollingInterval: RollingInterval.Day
                ));

            switch (settings.Level)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                    configuration = configuration.MinimumLevel.Verbose();
                    break;
                case LogLevel.Information:
                    configuration = configuration.MinimumLevel.Information();
                    break;
                case LogLevel.Warning:
                    configuration = configuration.MinimumLevel.Warning();
                    break;
                case LogLevel.Error:
                    configuration = configuration.MinimumLevel.Error();
                    break;
            }

            try
            {
                var holdDaysConfig = settings.RemainDay;

                if (holdDaysConfig > 0)
                {
                    LogHoldDays = holdDaysConfig;
                }

                //configuration = configuration.ReadFrom.AppSettings();
                exception = null;
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            var logger = configuration.CreateLogger();

            logger.Information($"当前日志文件数量上限:{remainFileCount}; 保留天数{LogHoldDays}; 日志级别:{settings.Level}");

            return logger;
        }

        private static Serilog.Core.Logger CreateCoreServerLogger(string productName, out Exception exception)
        {
            var processName = Process.GetCurrentProcess().ProcessName;
            var appDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var settings = LogSettingsHelper.CurrentSettings;
            var remainFileCount = settings.RemainFileCount;

            var configuration = new LoggerConfiguration()
              .Enrich.FromLogContext()
              .Enrich.WithProcessId()
              .Enrich.WithThreadId()
              .MinimumLevel.Verbose()
              .WriteTo.Logger(sub =>
                  sub.Filter.ByIncludingOnly(e => e.Properties.Any(p => p.Key == StreamScopeDefs.LogNameKey && p.Value.ToString() == "\"FFmpegLog\""))
                  .WriteTo.Map(
                      e => DateTime.Now.ToString("yyyy-MM-dd"),
                      (t, wt) => wt.File(
                          Path.Combine(appDir, productName, "logs", t, "dll_logs", $"FFmpeg.txt"),
                          shared: true,
                          rollOnFileSizeLimit: true,
                          retainedFileCountLimit: remainFileCount,
                          retainedFileTimeLimit: TimeSpan.FromDays(settings.RemainDay),
                          outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} | {Message:lj}{NewLine}",
                          fileSizeLimitBytes: 10 * 1024 * 1024,
                          flushToDiskInterval: TimeSpan.FromSeconds(15)
                      ))
              )
              .WriteTo.Logger(sub =>
                  sub.Filter.ByIncludingOnly(e => e.Properties.Any(p => p.Key == StreamScopeDefs.LogNameKey && p.Value.ToString() == "\"WorkerLog\""))
                  .WriteTo.Map(
                      e =>
                      {
                          e.Properties.TryGetValue(StreamScopeDefs.WorkerNameKey, out var value);
                          var scalarValue = (value as ScalarValue)?.Value as string;
                          return $"{DateTime.Now.ToString("yyyy-MM-dd")}|{scalarValue}";
                      },
                      (tag, wt) =>
                      wt.File(
                          Path.Combine(appDir, productName, "logs", tag.Split('|')[0], "dll_logs", "workers", $"{tag.Split('|')[1]}.txt"),
                          shared: true,
                          rollOnFileSizeLimit: true,
                          retainedFileCountLimit: remainFileCount,
                          retainedFileTimeLimit: TimeSpan.FromDays(settings.RemainDay),
                          outputTemplate: "{Message:lj}{NewLine}",
                          fileSizeLimitBytes: 10 * 1024 * 1024,
                          flushToDiskInterval: TimeSpan.FromSeconds(15)
                      ))
              )
              .WriteTo.Logger(sub =>
                  sub.Filter.ByExcluding(e => e.Properties.ContainsKey(StreamScopeDefs.LogNameKey))
                  .WriteTo.Map(
                    e => DateTime.Now.ToString("yyyy-MM-dd"),
                    (t, wt) => wt.File(
                        Path.Combine(appDir, productName, "logs", t, "dll_logs", $"{processName}.txt"),
                        shared: true,
                        rollOnFileSizeLimit: true,
                        /*每个进程每天最多仅允许产生500 * 10M日志文件（默认）
                        以C盘40G空闲空间为例，8日左右就会耗尽*/
                        retainedFileCountLimit: remainFileCount,
                        retainedFileTimeLimit: TimeSpan.FromDays(settings.RemainDay),
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} | {Level:u3} | {ProcessId} | {ThreadId:0000} | {SourceContext} | {RequestId} | {Client} | {Action} | {Message:lj}{NewLine}{Exception}",
                        fileSizeLimitBytes: 10 * 1024 * 1024,
                        flushToDiskInterval: TimeSpan.FromSeconds(15)
                    //rollingInterval: RollingInterval.Day
                    )
                ));

            try
            {
                var holdDaysConfig = LogSettingsHelper.CurrentSettings.RemainDay;

                if (holdDaysConfig > 0)
                {
                    LogHoldDays = holdDaysConfig;
                }

                switch (settings.Level)
                {
                    case LogLevel.Trace:
                    case LogLevel.Debug:
                        configuration = configuration.MinimumLevel.Verbose();
                        break;
                    case LogLevel.Information:
                        configuration = configuration.MinimumLevel.Information();
                        break;
                    case LogLevel.Warning:
                        configuration = configuration.MinimumLevel.Warning();
                        break;
                    case LogLevel.Error:
                        configuration = configuration.MinimumLevel.Error();
                        break;
                }

                exception = null;
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            var logger = configuration.CreateLogger();

            logger.Information($"当前日志文件数量上限:{remainFileCount}; 保留天数:{LogHoldDays}; 日志级别:{settings.Level}");

            return logger;
        }

        private static LogEventLevel GetLogEventLevel(LogLevel level)
        {
            var sLevel =
                level == LogLevel.Trace ? LogEventLevel.Verbose :
                level == LogLevel.Debug ? LogEventLevel.Debug :
                level == LogLevel.Information ? LogEventLevel.Information :
                level == LogLevel.Warning ? LogEventLevel.Warning :
                level == LogLevel.Error ? LogEventLevel.Error :
                LogEventLevel.Fatal;

            return sLevel;
        }

        /// <summary>
        /// 构造一个logger，注意，在使用模板语法的时候，不要传入固定的字段命名，例如：level
        /// </summary>
        /// <typeparam name="TC"></typeparam>
        /// <returns></returns>
        public static Microsoft.Extensions.Logging.ILogger OfContext([CallerFilePath] string filePath = null)
        {
            var fileInfo = new FileInfo(filePath);
            var name = fileInfo.Name;
            return factory.CreateLogger(name);
        }

        public static void ChangeMingLogLevelConfig(LogLevel level, string exePath = null)
        {
            TryChangeMinLogLevel(level, exePath);
        }

        public static bool TryChangeMinLogLevel(LogLevel level, string exePath = null)
        {
            var logger = OfContext();
            logger.LogInformation($"将配置文件{exePath ?? "默认"}的日志级别调整为{level}");
            lock (configurationLock)
            {
                var settings = LogSettingsHelper.CurrentSettings;
                var updated = new LogSettingModel
                {
                    Level = level,
                    RemainFileCount = settings.RemainFileCount,
                    RemainDay = settings.RemainDay,
                    RootPath = settings.RootPath
                };
                if (!LogSettingsHelper.TrySaveSettings(updated)) return false;
                LogSettingsHelper.CurrentSettings = updated;
                try
                {
                    ApplyCurrentSettings();
                    logger.LogInformation("日志配置已保存并应用，路径:{configPath}，级别:{level}",
                        LogSettingsHelper.LogConfigFilePath, level);
                    return createConfiguredLogger != null;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(EventIds.WarningDefault, ex,
                        "日志配置已保存，但应用到当前进程失败");
                    LogSettingsHelper.CurrentSettings = settings;
                    return false;
                }
            }
        }

        public static void ChangeMinLogLevel(LogLevel level)
        {
            var logger = OfContext();

            logger.LogInformation($"用户请求将日志级别调整为:{level}");
            ChangeMingLogLevelConfig(level);
        }

        public static Task StartLogMaintainTask(params string[] roles)
        {
            var logger = OfContext();
            var appDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            return Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    logger.LogInformation($"检查日志文件，日志文件最多保留{LogHoldDays}天");

                    foreach (var role in roles)
                    {
                        var rootFolder = Path.Combine(appDir, role, "logs");

                        if (!Directory.Exists(rootFolder))
                        {
                            // Z-Default 路径下的日志是存储在Logs文件夹下的，此处需要添加对Logs路径的判断，避免Z-Default下的日志清理被跳过
                            rootFolder = Path.Combine(appDir, role, "Logs");
                            if (!Directory.Exists(rootFolder))
                            {
                                continue;
                            }
                        }

                        foreach (var dir in Directory.GetDirectories(rootFolder))
                        {
                            var name = Path.GetFileName(dir);

                            if (DateTime.TryParse(name, out var date))
                            {
                                if ((DateTime.Now - date).Days > LogHoldDays)
                                {
                                    try
                                    {
                                        logger.LogInformation($"删除过期的日志目录:{dir}");
                                        Directory.Delete(dir, true);
                                    }
                                    catch (Exception ex)
                                    {
                                        Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "删除日志文件夹的过程中发生了错误");
                                    }
                                }
                            }
                        }
                    }

                    await Task.Delay(TimeSpan.FromHours(1));
                }

            }, TaskCreationOptions.LongRunning);
        }
        /// <summary>
        /// debug日志快，仅debug、trace模式下执行action；日志中包含需要逻辑处理结果的，建议使用此方法
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="action"></param>
        public static void LogDebugBlock(this Microsoft.Extensions.Logging.ILogger logger, Action action)
        {
            if (Level != LogLevel.Debug && Logger.Level != LogLevel.Trace)
            {
                return;
            }
            try
            {
                action();
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Error from LogDebugBlock: {ex}");
            }
        }
        /// <summary>
        /// debug日志 仅debug、trace模式下执行,用于打印日志内容需要执行表达式或者函数场景，降低额外表达式计算开销
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="messageFunc">Func 返回打印文案</param>
        public static void LogDebugLazy(this Microsoft.Extensions.Logging.ILogger logger, Func<string> messageFunc)
        {
            if (Level != LogLevel.Debug && Logger.Level != LogLevel.Trace)
            {
                return;
            }
            logger.LogDebug(messageFunc());
        }
    }
}
