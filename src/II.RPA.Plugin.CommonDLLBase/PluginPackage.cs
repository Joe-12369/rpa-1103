using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.GUI.Commons.WindowMatch.WindowCache;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace II.RPA.Plugin.CommonDLLBase
{
    public sealed class PluginPackage : PluginPackageBase
    {
        private static readonly ILogger Logger = II.RPA.Logging.Logger.OfContext();

        public override string PackageName => PluginProtocol.PackageName;

        protected override void Configure(PluginPackageBuilder builder)
        {
        }

        protected override Task InitializePackageAsync(IPluginInvoker pluginInvoker)
        {
            Logger.LogInformation(
                $"ServiceHubPort：{Configurations.Info.ServiceHubPort}，BrowserConnMode：{Configurations.Info.BrowserConnMode}, InitConnectTimeout:{Configurations.Info.InitConnectTimeout}, TransportConnectTimeout:{Configurations.Info.TransportConnectTimeout}");
            Logger.LogInformation("初始化CommonDLLBase");

            var assemblyPath = GetType().Assembly.Location;
            Logger.LogDebug($"当前公共基础包的位置：{assemblyPath}");
            PreloadAssemblies(assemblyPath);
            Logger.LogInformation($"CommonBase 当前应用域：{AppDomain.CurrentDomain.FriendlyName}");
            Logger.LogInformation($"CommonBase 应用程序域寻址:{AppDomain.CurrentDomain.BaseDirectory}");

            StartDesktopWindowMonitor();
            OpenSapScript();
            return Task.CompletedTask;
        }

        private static void PreloadAssemblies(string packageAssemblyPath)
        {
            var packageDirectory = Path.GetDirectoryName(packageAssemblyPath);
            if (string.IsNullOrWhiteSpace(packageDirectory))
            {
                throw new DirectoryNotFoundException("无法确定 CommonDLLBase 程序集目录。");
            }

            foreach (var filePath in Directory.GetFiles(packageDirectory, "*.dll"))
            {
                if (string.Equals(filePath, packageAssemblyPath, StringComparison.OrdinalIgnoreCase)) continue;

                try
                {
                    Assembly.LoadFrom(filePath);
                    Logger.LogDebug($"预加载{filePath}成功");
                }
                catch (Exception exception)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(
                        Logger,
                        exception,
                        $"预加载 {Path.GetFileNameWithoutExtension(filePath)} 失败，在程序运行时会再次尝试加载。");
                }
            }
        }

        private static void OpenSapScript()
        {
            try
            {
                using (var registryKey = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\SAP\SAPGUI Front\SAP Frontend Server\Security",
                    true))
                {
                    if (registryKey == null) return;
                    registryKey.SetValue("UserScripting", 1);
                    registryKey.SetValue("WarnOnAttach", 0);
                    registryKey.SetValue("WarnOnConnection", 0);
                }
            }
            catch (Exception exception)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(Logger, exception, "SAP脚本运行开启失败");
            }
        }

        private static void StartDesktopWindowMonitor()
        {
            _ = DesktopWindowMonitor.Shared.StartAsync().ContinueWith(task =>
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(
                    Logger,
                    task.Exception,
                    "Desktop window monitor encountered an error.");
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }

    internal static class PluginProtocol
    {
        internal const string PackageName = "CommonDLLBase";
    }
}
