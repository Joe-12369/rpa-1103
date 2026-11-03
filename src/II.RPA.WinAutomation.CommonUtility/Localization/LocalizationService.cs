using II.RPA.Logging;
using II.RPA.WinAutomation.CommonUtility.Resource;
using II.RPA.WinAutomation.CommonUtility.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Security.Cryptography;
using System.Windows.Markup;


namespace II.RPA.WinAutomation.CommonUtility
{
    public class LocalizationService
    {
        private static ResourceManager _resourceManager;
        private static ResourceManager _hashResourceManager;

        private static CultureInfo _culture = null;
        private static bool _initialized = false;
        private static ILogger logger = Logger.OfContext();

        static LocalizationService()
        {
            var culture = RPAEnvironment.GetInstance().CurrentLanguage.ToString().Replace("_", "-");
            Initialize(culture);
            logger.LogInformation($"LocalizationService initialized with culture: {culture}");
        }
        public static void Initialize(string cultureName = "zh-CN")
        {
            _resourceManager = Resource.II_RPA_Resource.ResourceManager;
            _hashResourceManager = Resource.II_RPA_ResourceHash.ResourceManager;
            _culture = new CultureInfo(cultureName);
            _initialized = true;
        }

        public static void SetCulture(string cultureName)
        {
            if (!_initialized)
                throw new InvalidOperationException("pleases use LocalizationService.Initialize first");
            _culture = new CultureInfo(cultureName);
        }

        public static string GetString(string key, bool useHash = false)
        {
            try
            {
                if (useHash)
                {
                    string hashedKey = key.GetHash();
                    logger.LogDebug($"Looking up resource with hashed key: {hashedKey}");

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
                }
                    return _resourceManager.GetString(key, _culture);
            }
            catch (MissingManifestResourceException)
            {
                logger.LogWarning($"Resource for key '{key}' not found.");
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


