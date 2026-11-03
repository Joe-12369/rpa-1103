using II.RPA.Logging;
using II.RPA.WinAutomation.CommonUtility.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;

namespace II.RPA.WinAutomation.CommonUtility
{
    public class ConfResourceLoader
    {
        static ConfResourceLoader()
        {
            RPAEnvironment.GetInstance().LanguageChanged += (s, e) =>
            {
                logger.LogInformation($"language changed:{e.OldLanguage}-->{e.NewLanguage}");
                _cache.Clear();
                CultureName = e.NewLanguage.ToString().ToString().Replace("_", "-");
                LoadConfResource(e.NewLanguage);
            };
            LoadConfResource(RPAEnvironment.GetInstance().CurrentLanguage);
        }

        private static string CultureName = RPAEnvironment.GetInstance().CurrentLanguage.ToString().Replace("_", "-");
        private static ILogger logger = Logger.OfContext();

        private static readonly ConcurrentDictionary<string, Dictionary<string, string>> _cache
            = new ConcurrentDictionary<string, Dictionary<string, string>>();

        private static void LoadConfResource(LanguageCultureEnum language)
        {
            LoadResourceDictionary(typeof(eCommonMessageType).Name);
            LoadResourceDictionary(typeof(eComponentType).Name);
            LoadResourceDictionary(typeof(eErrorMessageType).Name);
        }
        private static Dictionary<string, string> LoadConfResource(string confName)
        {
            var dict = new Dictionary<string, string>();

            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();

                string resourceName = asm.GetName().Name + ".Config." + confName + ".conf";

                using (Stream manifestStream = asm.GetManifestResourceStream(resourceName))
                {
                    if (manifestStream == null)
                    {
                        logger.LogError($"resource not found: {resourceName}");
                        return dict;
                    }
                    using (StreamReader sr = new StreamReader(manifestStream, Encoding.UTF8))
                    {
                        while (!sr.EndOfStream)
                        {
                            string line = sr.ReadLine();
                            if (string.IsNullOrEmpty(line)) continue;

                            line = line.TrimStart();
                            if (line.StartsWith("#")) continue;

                            int index = line.IndexOf("=");
                            if (index <= 0) continue;

                            string code = line.Substring(0, index).Trim();
                            if (string.IsNullOrEmpty(code)) continue;

                            string msg = line.Substring(index + 1).Trim();
                            dict[code] = msg;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"加载 {confName}.conf 失败: {ex.Message}");
            }
            return dict;
        }

        private static void LoadLocalizeResx(Dictionary<string, string> dict, string confName, string culture)
        {
            try
            {
                var rm = Resource.II_RPA_Resource.ResourceManager;
                var ci = new CultureInfo(culture);

                foreach (var code in new List<string>(dict.Keys))
                {
                    string key = $"{confName}_{code}";
                    string val = rm.GetString(key, ci);
                    if (!string.IsNullOrEmpty(val))
                    {
                        dict[code] = val;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"加载 {confName} {culture} 资源失败: {ex.Message}");
            }
        }

        public static Dictionary<string, string> LoadResourceDictionary(string confName)
        {
            logger.LogInformation($"load {confName} resource");
            string cacheKey = $"{CultureName}:{confName}";
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            var dict = LoadConfResource(confName);

            if (CultureName.Equals("zh-CN", StringComparison.OrdinalIgnoreCase))
            {
                _cache[cacheKey] = dict;
                return dict;
            }

            LoadLocalizeResx(dict, confName, CultureName);

            _cache[cacheKey] = dict;
            return dict;
        }

        public static string GetMessage<T>(T configType, string code)
        {
            var config = typeof(T).Name;
            logger.LogDebug($"get {config} message,code:{code}");

            try
            {
                var confDic = LoadResourceDictionary(config);

                if (confDic != null && confDic.TryGetValue(code, out string message))
                {
                    return message;
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                return string.Empty;
            }
        }
    }
}
