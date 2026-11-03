using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Logging.Configurations
{
    public static class LogSettingsHelper
    {
        public static string LogConfigFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LogSettings.json");
        private static readonly ILogger logger = Logger.OfContext();

        public static LogSettingModel CurrentSettings
        {
            get;
            set;
        } = LoadSettings();

        public static void Refresh()
        {
            CurrentSettings = LoadSettings();
        }

        public static bool TryRefresh()
        {
            try
            {
                using (var stream = File.Open(LogConfigFilePath, FileMode.Open,
                    FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var reader = new StreamReader(stream))
                using (var json = new JsonTextReader(reader))
                {
                    var settings = new JsonSerializer().Deserialize<LogSettingModel>(json);
                    if (settings == null) return false;
                    CurrentSettings = settings;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex,
                    "日志配置尚不可用，保留当前配置");
                return false;
            }
        }

        private static LogSettingModel LoadSettings()
        {
            try
            {
                if (File.Exists(LogConfigFilePath))
                {
                    using (FileStream fileStream = File.Open(LogConfigFilePath, 
                        FileMode.Open, 
                        FileAccess.Read,
                        FileShare.ReadWrite))
                    {
                        using (StreamReader sw = new StreamReader(fileStream))
                        {
                            using (JsonTextReader jw = new JsonTextReader(sw))
                            {
                                JsonSerializer serializer = new JsonSerializer();
                                var model = serializer.Deserialize<LogSettingModel>(jw);
                                if (model != null)
                                {
                                    return model;
                                }
                                else
                                {
                                    logger.LogWarning("日志配置文件内容无效，已重置为默认配置!");
                                }
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "日志配置文件加载失败!");
            }
            //已默认设置重置配置文件
            SaveSettings(LogSettingModel.Default);

            return LogSettingModel.Default;
        }

        public static void SaveSettings(LogSettingModel logSettingModel)
        {
            TrySaveSettings(logSettingModel);
        }

        public static bool TrySaveSettings(LogSettingModel logSettingModel)
        {
            try
            {
                if (logSettingModel == null)
                {
                    logSettingModel = LogSettingModel.Default;
                }
                var json = JsonConvert.SerializeObject(logSettingModel, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(LogConfigFilePath, json);
                return true;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "日志配置文件保存失败!");
                return false;
            }
        }

    }
}
