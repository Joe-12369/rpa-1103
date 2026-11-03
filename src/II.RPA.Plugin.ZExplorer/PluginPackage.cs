using II.RPA.Logging;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.Plugin.ZExplorer.Config;
using II.RPA.Plugin.ZExplorer.V2;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.MultiInstance;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;

namespace II.RPA.Plugin.ZExplorer
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
                WindowMatcher windowMatcher = new WindowMatcher(WindowType.ZExplorer, ZExplorerConfigurations.Info.MatchConfigMap.Values.ToList());

                BrowserWindowProvider.AddWindowMatcher(windowMatcher);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "设置实在浏览器窗口 命中匹配器");
            }
        }
        #endregion

        public void SetBrowserSpec()
        {
            BrowserSpec browserSpec = BrowserSpec.Create<V2.ZExplorer, WebDataProvider, GuiOperator, BrowserAction>(new string[0]);
            browserSpec.WindowType = WindowType.ZExplorer;
            foreach (var matchConfig in ZExplorerConfigurations.Info.MatchConfigMap)
            {
                BrowserSpecRepository.TryAddSpec(matchConfig.Value.ConfigKey, browserSpec);
            }
        }
    }

    internal static class PluginProtocol
    {
        internal const string PackageName = "GUIZExplorer";
    }
}
