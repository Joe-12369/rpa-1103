using II.RPA.Logging;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.Plugin.GUI.Commons.WindowMatch.Entities;
using II.RPA.Plugin.GUI.Commons.WindowMatch.MatchRule;
using II.RPA.Plugin.IE.V2;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.MatchBrowser;
using II.RPA.WebBrowser.Common.MultiInstance;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace II.RPA.Plugin.IE
{
    public sealed class PluginPackage : PluginPackageBase
    {
        private static ILogger logger = Logger.OfContext();

        public override string PackageName => PluginProtocol.PackageName;

        protected override void Configure(PluginPackageBuilder builder)
        {
        }

        protected override System.Threading.Tasks.Task InitializePackageAsync(II.RPA.Plugin.Abstractions.IPluginInvoker pluginInvoker)
        {
            try
            {
                SetWindowMatcher();
                SeEmbeddedIEMatcher();
                SetBrowserSpec();
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "初始化包异常");
            }
            return System.Threading.Tasks.Task.CompletedTask;
        }

        #region 设置浏览器窗口 命中匹配器
        public void SetWindowMatcher()
        {
            try
            {
                WindowMatcher windowMatcher = new WindowMatcher(WindowType.IE, Config.Configurations.Info.MatchConfigMap.Values.ToList());
                BrowserWindowProvider.AddWindowMatcher(windowMatcher);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "设置IE浏览器窗口 命中匹配器");
            }
        }
        #endregion

        #region 设置应用内嵌IE窗口 命中匹配器
        public void SeEmbeddedIEMatcher()
        {
            try
            {
                var appEmbeddedIEConfigs = Config.Configurations.Info.MatchConfigMap.Values.Where(config => config.ConfigType == MatchConfigType.AppEmbeddedIE).ToList();
                WindowMatcher embedded_windowMatcher = new WindowMatcher(WindowType.IE | WindowType.Windows, appEmbeddedIEConfigs);
                foreach (var appEmbeddedIEConfig in appEmbeddedIEConfigs)
                {
                    var embeddedIEMatchHandler = new EmbeddedIEMatchHandler();
                    var windowMatchHandlers = new MatchHandler[] {
                            new ExcludeProcessMatchHandler(),
                            embeddedIEMatchHandler
                    };
                    embedded_windowMatcher.SetWindowMatcherItem(appEmbeddedIEConfig, embeddedIEMatchHandler, windowMatchHandlers);
                }
                EmbeddedIEWindowProvider.AddAppEmbeddedIEMatcher(embedded_windowMatcher);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "设置IE浏览器窗口 命中匹配器");
            }
        }
        #endregion

        public void SetBrowserSpec()
        {
            BrowserSpec browserSpec = BrowserSpec.Create<V2.IEBrowser, WebDataProvider, GuiOperator, BrowserAction>(new string[0]);
            browserSpec.WindowType = WindowType.IE;
            foreach (var matchConfig in Config.Configurations.Info.MatchConfigMap)
            {
                BrowserSpecRepository.TryAddSpec(matchConfig.Value.ConfigKey, browserSpec);
            }
        }
    }

    internal static class PluginProtocol
    {
        internal const string PackageName = "GUIIE";
    }
}
