using II.RPA.Plugin.Core.PluginBase;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace II.RPA.Plugin.Excel.Config
{
    public class ExcelConfigurations : ConfigurationBase<ExcelConfigurations>
    {
        public ExcelConfigurations()
        {

        }
        [JsonProperty("GlobalSetting")]
        public Globalsetting GlobalSetting { get; set; }
        [JsonProperty("ActionSetting")]
        public Dictionary<string, JObject> ActionSetting { get; set; }
        public dynamic GetSetting<T>(string section, string key)
        {
            try
            {
                var valObj = Info.ActionSetting[GetActionName(section)];
                var value = valObj.GetValue(key, StringComparison.OrdinalIgnoreCase).Value<T>();
                ExcelPlugin.logger.LogInformation($"get action setting {section}.{key}:{value}");
                return value;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(ExcelPlugin.logger, ex, $"get action setting {section}.{key}");
                return default(T);
            }
        }
        public string GetActionName(string action)
        {
            if (ActionSetting.ContainsKey(action))
            {
                return action;
            }
            else
            {
                var _version = Regex.Match(action, "V\\d+");
                if (_version.Success)
                {
                    return action.Replace(_version.Value, "");
                }
                return action;
            }
        }
    }

    public class Globalsetting
    {
        public int? ExecuteTimeout { get; set; }
        public int? DelayBefore { get; set; }=0;
        public int? UseLegacyReader { get; set; } = 0;
        public int? UseLegacyRange {  get; set; } = 0;
        public string Application { get; set; }
        public int BatchSize { get; set; } = 1000;
        public string Debug { get; set; }
        public string GC { get; set; }
        public string WPSClass { get; set; }
        public string SaveInstance { get; set; } = "true";
        public string Key4 { get; set; } = "INPUT_PARAM_KEY4";
        public string OpenMode { get; set; }
    }
}

