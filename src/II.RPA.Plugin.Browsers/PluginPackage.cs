using II.RPA.Logging;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.Plugin.GUI.Commons.WindowMatch.Entities;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.MultiInstance;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Browsers
{
    public sealed class PluginPackage : PluginPackageBase
    {
        private static readonly ILogger Logger = Logging.Logger.OfContext();
        private static readonly IDictionary<string, MatchConfig> ChromiumConfigurations = new Dictionary<string, MatchConfig>();

        public override string PackageName => PluginProtocol.PackageName;
        protected override void Configure(PluginPackageBuilder builder) { }

        protected override Task InitializePackageAsync(IPluginInvoker pluginInvoker)
        {
            RegisterConfiguredBrowsers(Config.Configurations.Info.MatchConfigMap);
            InitializeGenericChromium();
            return Task.CompletedTask;
        }

        internal static void RegisterConfiguredBrowsers(IDictionary<string, MatchConfig> configurations)
        {
            if (configurations == null) return;
            foreach (var group in configurations.Values.Where(config => config != null && config.WindowType != WindowType.None).GroupBy(config => config.WindowType))
            {
                try { RegisterConfiguredBrowser(group.Key, group.ToList()); }
                catch (Exception ex) { Microsoft.Extensions.Logging.LoggingExtensions.LogError(Logger, ex, $"注册浏览器配置失败：{group.Key}"); }
            }
        }

        private static void RegisterConfiguredBrowser(WindowType windowType, List<MatchConfig> configurations)
        {
            BrowserSpec browserSpec;
            switch (windowType)
            {
                case WindowType.Chrome:
                    browserSpec = BrowserSpec.Create<II.RPA.Plugin.Browsers.Chrome.ChromeBrowser, WebDataProvider, II.RPA.Plugin.Browsers.Chrome.ChromeGuiOperator, II.RPA.Plugin.Browsers.Chrome.ChromeBrowserAction>(new[] { ProcessNames.NativeMessagingHost });
                    break;
                case WindowType.Edge:
                    browserSpec = BrowserSpec.Create<II.RPA.Plugin.Browsers.Edge.EdgeBrowser, WebDataProvider, II.RPA.Plugin.Browsers.Edge.GuiOperator, II.RPA.Plugin.Browsers.Edge.BrowserAction>(new[] { ProcessNames.NativeMessagingHost, ProcessNames.EdgeNativeMessagingHost });
                    break;
                case WindowType.FireFox:
                    browserSpec = BrowserSpec.Create<II.RPA.Plugin.Browsers.FireFox.FireFoxBrowser, WebDataProvider, II.RPA.Plugin.Browsers.FireFox.GuiOperator, II.RPA.Plugin.Browsers.FireFox.BrowserAction>(new[] { ProcessNames.FireFoxNativeMessagingHost });
                    break;
                case WindowType.Sogou:
                    browserSpec = BrowserSpec.Create<II.RPA.Plugin.Browsers.Sogou.SogouBrowser, WebDataProvider, II.RPA.Plugin.Browsers.Sogou.GuiOperator, II.RPA.Plugin.Browsers.Sogou.BrowserAction>(new[] { ProcessNames.NativeMessagingHost, ProcessNames.SogouNativeMessagingHost });
                    break;
                case WindowType.SuperBrowser:
                    browserSpec = BrowserSpec.Create<II.RPA.Plugin.Browsers.SuperBrowser.SuperBrowser, WebDataProvider, II.RPA.Plugin.Browsers.SuperBrowser.GuiOperator, II.RPA.Plugin.Browsers.SuperBrowser.BrowserAction>(new[] { ProcessNames.NativeMessagingHost });
                    break;
                case WindowType.Web360se:
                    browserSpec = BrowserSpec.Create<II.RPA.Plugin.Browsers.Web360se.Web360seBrowser, WebDataProvider, II.RPA.Plugin.Browsers.Web360se.GuiOperator, II.RPA.Plugin.Browsers.Web360se.BrowserAction>(new[] { ProcessNames.NativeMessagingHost, ProcessNames.Web360seNativeMessagingHost });
                    break;
                case WindowType.Web360X:
                    browserSpec = BrowserSpec.Create<II.RPA.Plugin.Browsers.Web360X.Web360XBrowser, WebDataProvider, II.RPA.Plugin.Browsers.Web360X.GuiOperator, II.RPA.Plugin.Browsers.Web360X.BrowserAction>(new[] { ProcessNames.NativeMessagingHost, ProcessNames.Web360XNativeMessagingHost });
                    break;
                default:
                    return;
            }
            browserSpec.WindowType = windowType;
            BrowserWindowProvider.AddWindowMatcher(new WindowMatcher(windowType, configurations));
            foreach (var configuration in configurations) BrowserSpecRepository.TryAddSpec(configuration.ConfigKey, browserSpec);
        }

        private static void InitializeGenericChromium()
        {
            foreach (var configuration in LoadGenericChromiumConfigurations().Values) ActivateConfiguredChromiumBrowser(configuration);
            ChromiumBrowserDiscovery.OnBrowserDiscovered += ActivateDiscoveredBrowser;
        }

        private static IDictionary<string, MatchConfig> LoadGenericChromiumConfigurations()
        {
            var configurations = new Dictionary<string, MatchConfig>();
            try
            {
                var localApplication = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var configDirectory = Path.Combine(localApplication, ConfigFileNames.DirectoryName);
                var oldPath = Path.Combine(configDirectory, ConfigFileNames.LegacyChromiumBrowserConfig);
                var path = Path.Combine(configDirectory, ConfigFileNames.ChromiumBrowserConfig);
                if (!File.Exists(path) && File.Exists(oldPath)) File.Copy(oldPath, path);
                if (File.Exists(path))
                {
                    configurations = JObject.Parse(File.ReadAllText(path))["BrowserConfigMap"]?.ToObject<Dictionary<string, MatchConfig>>() ?? configurations;
                }
            }
            catch (Exception ex) { Microsoft.Extensions.Logging.LoggingExtensions.LogError(Logger, ex, "读取Chromium浏览器配置信息异常，需要检查配置文件格式"); }
            return configurations;
        }

        public static void ActivateDiscoveredBrowser(MatchConfig matchConfig)
        {
            ActivateChromiumBrowser(matchConfig, new[] { ProcessNames.NativeMessagingHost });
        }

        internal static void ActivateConfiguredChromiumBrowser(MatchConfig matchConfig)
        {
            ActivateChromiumBrowser(matchConfig, new[] { ProcessNames.NativeMessagingHost, ProcessNames.EdgeNativeMessagingHost });
        }

        private static void ActivateChromiumBrowser(MatchConfig matchConfig, string[] nmhProcessNames)
        {
            if (matchConfig == null) return;
            try
            {
                matchConfig.WindowType = WindowType.Chromium;
                var browserSpec = BrowserSpec.Create<II.RPA.Plugin.Browsers.Chromium.ChromiumBrowser, WebDataProvider, II.RPA.Plugin.Browsers.Chromium.ChromiumGuiOperator, II.RPA.Plugin.Browsers.Chromium.ChromiumBrowserAction>(nmhProcessNames);
                browserSpec.WindowType = WindowType.Chromium;
                BrowserSpecRepository.TryAddSpec(matchConfig.ConfigKey, browserSpec);
                BrowserWindowProvider.AddWindowMatcher(new WindowMatcher(WindowType.Chromium, new List<MatchConfig> { matchConfig }));
                ChromiumConfigurations[matchConfig.ProductName ?? matchConfig.ConfigKey] = matchConfig;
                Logger.LogInformation($"动态浏览器配置已激活: {matchConfig.ProductName}");
            }
            catch (Exception ex) { Microsoft.Extensions.Logging.LoggingExtensions.LogError(Logger, ex, $"激活动态浏览器配置失败: {matchConfig?.ProductName}"); }
        }

        private static class ProcessNames
        {
            internal const string NativeMessagingHost = "II.RPA.NativeMessagingHost";
            internal const string EdgeNativeMessagingHost = "II.RPA.NativeMessagingHostEdge";
            internal const string FireFoxNativeMessagingHost = "II.RPA.NativeMessagingHostFireFox";
            internal const string SogouNativeMessagingHost = "II.RPA.NativeMessagingHostSogou";
            internal const string Web360seNativeMessagingHost = "II.RPA.NativeMessagingHost360";
            internal const string Web360XNativeMessagingHost = "II.RPA.NativeMessagingHost360X";
        }

        private static class ConfigFileNames
        {
            internal const string DirectoryName = "IIRPA";
            internal const string ChromiumBrowserConfig = "ChromiumBrowserConfig.json";
            internal const string LegacyChromiumBrowserConfig = "ChromiunBrowserConfig.json";
        }
    }

    internal static class PluginProtocol
    {
        internal const string PackageName = "Browsers";
    }
}
