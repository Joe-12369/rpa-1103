using System;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace II.Framework.Resource
{
    public class LocalizationService
    {
        private static ResourceManager _resourceManager;
        private static ResourceManager _hashResourceManager;
        private static readonly object _initializationLock = new object();

        private static CultureInfo _culture = new CultureInfo("zh-CN");

        private static void Log(string message)
        {
            Debug.WriteLine(message);
        }

        static LocalizationService()
        {         
          
        }
        public static void Initialize()
        {
            II.Framework.Resource.i18n_Resource.Culture = _culture;
            _resourceManager = II.Framework.Resource.i18n_Resource.ResourceManager;
            _hashResourceManager = II.Framework.Resource.i18n_ResourceHash.ResourceManager;
        }

        private static void EnsureInitialized()
        {
            if (_resourceManager == null)
            {
                lock (_initializationLock)
                {
                    if (_resourceManager == null)
                    {
                        Initialize();
                    }
                }
            }
        }

        public static CultureInfo GetCurrentLanguage()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                int rpaEngineIndex = baseDir.IndexOf(@"\rpa-engine\", StringComparison.OrdinalIgnoreCase);

                if (rpaEngineIndex != -1)
                {
                    string rpaEnginePath = baseDir.Substring(0, rpaEngineIndex + @"\rpa-engine".Length);
                    var configPath = Path.Combine(rpaEnginePath, "config", "config.json");
                    if (File.Exists(configPath))
                    {
                        var configContent = File.ReadAllText(configPath);
                        var configJson = Newtonsoft.Json.Linq.JObject.Parse(configContent);
                        var languageNode = configJson["useLang"];
                        if (languageNode != null)
                        {
                            var language = languageNode.ToString();
                            Console.WriteLine($"RPA-Engine Language:{language}");
                            return new CultureInfo(language.Trim());
                        }
                    }
                }
                Console.WriteLine("RPA-Engine Language:Not Found,Use System Language");
                return System.Globalization.CultureInfo.CurrentCulture;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return System.Globalization.CultureInfo.CurrentCulture;
            }
        }

        public static void SetCulture(CultureInfo culture)
        {
            _culture = culture;
            Initialize();
        }
        public static void SetCulture(string cultureName)
        {
            Log($"LocalizationService initialized with culture: {_culture.Name}");
            _culture = new CultureInfo(cultureName);
            Initialize();
        }

        public static string GetString(string key,bool useHash=false)
        {
            try
            {
                EnsureInitialized();
                if(useHash)
                {
                    string hashedKey = key.GetHash();
                    Log($"Looking up resource with hashed key: {hashedKey}");
                    
                    string mappedKeys = _hashResourceManager.GetString(hashedKey, _culture);
                    if (!string.IsNullOrEmpty(mappedKeys))
                    {
                        var keys = mappedKeys.Split(',');
                        foreach (var k in keys)
                        {
                            string result = _resourceManager.GetString(k, _culture);
                            if (!string.IsNullOrEmpty(result))
                            {
                                return result;
                            }
                        }
                    }
                    return $"[Missing: {key}]";
                }
                return _resourceManager.GetString(key, _culture);
            }
            catch (MissingManifestResourceException)
            {
                Log($"Resource for key '{key}' not found.");
                return null;
            }
        }
    }

    /// <summary>
    /// 用于标记枚举，指定生成 i18n Key 的前缀。
    /// </summary>
    [AttributeUsage(AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
    public sealed class I18nAttribute : Attribute
    {
        /// <summary>
        /// Key 前缀
        /// </summary>
        public string Prefix { get; }

        public I18nAttribute(string prefix)
        {
            Prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
        }
    }

    public static class I18nExtensions
    {
        public static string Localize(FormattableString str, string key)
        {
            var template = LocalizationService.GetString(key);
            if (string.IsNullOrEmpty(template))
            {
                template = str.Format;
            }
            return string.Format(template, str.GetArguments());
        }
        public static string I18n(this string str, string key)
        {
            string value = LocalizationService.GetString(key);

            if (string.IsNullOrEmpty(value))
            {
                return str;
            }
            return value;
        }
    }

    public static class HashHelper
    {
        public static string GetFileHash(string filePath)
        {
            using (var sha256 = SHA256.Create())
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    byte[] hashBytes = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }
        }
        public static string GetHash(this string input)
        {
            const uint fnvPrime = 0x01000193;
            uint hash = 0x811C9DC5;

            foreach (var c in input)
            {
                hash ^= c;
                hash *= fnvPrime;
            }

            return hash.ToString("X");
        }
    }
}


