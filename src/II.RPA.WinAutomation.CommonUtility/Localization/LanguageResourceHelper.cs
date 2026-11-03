using II.RPA.Logging;
using II.RPA.WinAutomation.CommonUtility.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace II.RPA.WinAutomation.CommonUtility.XamlResource
{
    public class LanguageResourceHelper : SingleInstance<LanguageResourceHelper>
    {
        private static ILogger logger = Logger.OfContext();
        public LanguageCultureEnum CurrentLanguage
        {
            get
            {
                return RPAEnvironment.GetInstance().CurrentLanguage;
            }
        }

        private readonly string defaultXamlResource = "Pack://application:,,,/II.RPA.WinAutomation.CommonUtility;component/Config/xamlResource.xaml";

        ResourceDictionary defaultResourceDictionary = new ResourceDictionary();

        private readonly Dictionary<LanguageCultureEnum, ResourceDictionary> _cachedDictionaries = new Dictionary<LanguageCultureEnum, ResourceDictionary>();
        private bool HasLoadResource { get; set; } = false;

        public void SetAppInstance(ContentControl app)
        {
            InitLanguageResource();
            SetAppLanguageResource(app);
        }

        private void InitLanguageResource()
        {
            if (!HasLoadResource)
            {
                try
                {
                    defaultResourceDictionary.Source = new Uri(defaultXamlResource, UriKind.RelativeOrAbsolute);
                    HasLoadResource = true;
                }
                catch (Exception ex)
                {
                    logger.LogError($"Load XAML resource file Exception:{ex.ToString()}");
                }
            }
        }

        private void SetAppLanguageResource(ContentControl app)
        {
            try
            {
                if (app == null) return;

                foreach (var dict in _cachedDictionaries.Values)
                {
                    if (app.Resources.MergedDictionaries.Contains(dict))
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
                if (CurrentLanguage == LanguageCultureEnum.zh_CN)
                {
                    if (!app.Resources.MergedDictionaries.Contains(defaultResourceDictionary))
                    {
                        app.Resources.MergedDictionaries.Add(defaultResourceDictionary);
                    }
                    return;
                }

                var culture = new CultureInfo(CurrentLanguage.ToString().Replace("_", "-"));
                ResourceDictionary resourceDict = null;

                if (!_cachedDictionaries.TryGetValue(CurrentLanguage, out resourceDict))
                {
                    resourceDict = CreateResourceDictionaryFromResx(culture);
                    if (resourceDict.Count == 0)
                    {
                        logger.LogWarning($"Failed to load resources for culture: {culture.Name}. Falling back to default XAML resources.");
                        resourceDict = defaultResourceDictionary;
                    }
                    else
                    {
                        _cachedDictionaries[CurrentLanguage] = resourceDict;
                    }
                }

                if (resourceDict != null && !app.Resources.MergedDictionaries.Contains(resourceDict))
                {
                    app.Resources.MergedDictionaries.Add(resourceDict);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"SetAppLanguageResource failed: {ex}");
            }
        }

        private ResourceDictionary CreateResourceDictionaryFromResx(CultureInfo culture)
        {
            var dict = new ResourceDictionary();
            try
            {
                var resourceManager = Resource.II_RPA_Resource.ResourceManager;
                var resourceSet = resourceManager.GetResourceSet(culture, true, true);

                if (resourceSet == null)
                {
                    logger.LogWarning($"No resource set found for culture: {culture.Name}. " +
                        $"Ensure that 'Resource.{culture.Name}.resx' exists and is set to 'Public'.");
                    return dict;
                }

                int loadedCount = 0;
                foreach (System.Collections.DictionaryEntry entry in defaultResourceDictionary)
                {
                    var key = $"xamlResource_{entry.Key}";
                    var value = LocalizationService.GetString(key) ?? entry.Value;

                    dict.Add(entry.Key, value);
                    loadedCount++;
                }

                logger.LogInformation($"Loaded {loadedCount}xaml resources for culture: {culture.Name}");
                return dict;
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to load xmal resources from .resx for culture '{culture.Name}': {ex}");
                return new ResourceDictionary();
            }
        }

    }
}
