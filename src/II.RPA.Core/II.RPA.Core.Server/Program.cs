using CommonServiceLocator;
using II.RPA.Core.Abstract;
using II.RPA.Core.Common;
using II.RPA.Core.Log;
using II.RPA.Core.Provider.EngineProxyProvider;
using II.RPA.Core.Provider.PluginLoaderProvider.Command;
using II.RPA.Core.Contracts.Talker;
using II.RPA.Core.Provider.ZBrainProxyProvider;
using II.RPA.Core.Runtime;
using II.RPA.Core.Services;
using II.RPA.Core.Talker;
using II.RPA.Logging;
using II.RPA.Logging.Definitions;
using II.RPA.Logging.Formatting;
using Microsoft.Practices.Unity.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Unity;
using Unity.ServiceLocation;

namespace II.RPA.Core.Server
{
    static class Program
    {
        private static ILogger s_Logger;
        private static IWorkerTalkerRegistrar s_WorkerTalkerRegistrar;
        private static DateTime currentLogDate = DateTime.Now.Date;
        [STAThread]
        static void Main(string[] aStartUpParams)
        {
            Utilities.PowerStatusReporter powerStatusReporter = null;
            try
            {
                SystemInfoUtility.StartPowerSavingMonitor();
                AttachConsole(ATTACH_PARENT_PROCESS);
                var role = Global.Client == ClientID.Factory ? LoggingRoles.Factory :
                    Global.Client == ClientID.Bot ? LoggingRoles.Bot :
                    LoggingRoles.Default;

                //根据GRPC的启动角色来确定日志的纪录角色
                Logger.ConfigCoreServerWithRole(role);
                Task.Run(() =>
                {
                    while (true)
                    {
                        if (DateTime.Now.Date != currentLogDate)
                        {
                            currentLogDate = DateTime.Now.Date;
                            Logger.ConfigCoreServerWithRole(role);
                        }
                        Thread.Sleep(1000 * 60);
                    }
                });

                var coreServerSettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Z-Core", "core_server_settings.json");
                if (File.Exists(coreServerSettingsPath))
                {
                    try
                    {
                        var jObject = JObject.Parse(File.ReadAllText(coreServerSettingsPath));
                        if (jObject.ContainsKey("force_restart_runtimehost"))
                        {
                            Global.ForceRestartHost = jObject.Value<bool>("force_restart_runtimehost");
                        }
                    }
                    catch
                    { }
                }

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                Application.ThreadException += Application_ThreadException;
                TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

                IUnityContainer BootContainer = new UnityContainer();
                UnityServiceLocator locator = new UnityServiceLocator(BootContainer);
                ServiceLocator.SetLocatorProvider(() => locator);
                ConfigureContainer(BootContainer);

                ExceptionHandler.Default = BootContainer.Resolve<ExceptionHandler>();
                s_Logger = BootContainer.Resolve<ILogger>();

                Application.Idle += Application_Idle;

                var argsPythonEnv = CommandArgHelper.GetArgumentValue("--python_env");
                s_Logger.LogInfo($"argsPythonEnv: {argsPythonEnv}");
                Global.PythonEnvVersion = !string.IsNullOrEmpty(argsPythonEnv) ? argsPythonEnv : "3.6.8";
                s_Logger.LogInfo($"启动参数: {string.Join(" ", aStartUpParams)}");
                s_Logger.LogInfo($"IOC container has been initialized and took {stopwatch.ElapsedMilliseconds} ms.");

                stopwatch.Stop();

                BootContainer.RegisterSingleton<CommandConsumer>();
                BootContainer.RegisterSingleton<FuncTaskConsumer>();

                // The refactored Core Talker is the only worker lifecycle implementation.
                // Resolve it once and publish its explicit controller boundaries before any
                // gRPC service is constructed.
                var coreTalkerManager = BootContainer.Resolve<TalkerManager>();
                s_WorkerTalkerRegistrar = coreTalkerManager;
                powerStatusReporter = new Utilities.PowerStatusReporter(coreTalkerManager, s_Logger);
                BootContainer.RegisterInstance<IWorkerTalkerRegistrar>(coreTalkerManager);
                BootContainer.RegisterInstance<IRuntimeHostController>(coreTalkerManager.RuntimeHostController);
                BootContainer.RegisterInstance<IZBrainController>(coreTalkerManager.ZBrainController);
                BootContainer.RegisterInstance<IMobileProviderController>(coreTalkerManager.MobileProviderController);
                BootContainer.RegisterInstance<IRuntimeStateAccessor>(coreTalkerManager);
                BootContainer.RegisterInstance<IWorkerControlController>(coreTalkerManager.WorkerControlController);

                BotService.BuildSingleton(s_Logger);

                var endPointProvider = BootContainer.Resolve<IEndPointProvider>();
                endPointProvider.Initial(BootContainer.ResolveAll<IServiceBoot>());

                // Resolving the application context starts every IServiceBoot and waits until
                // the endpoints are actually listening. Worker processes must receive the final
                // port selected by EndPointProvider, not the configured port that may be occupied.
                var appContext = BootContainer.Resolve<ApplicationContext>();
                BootContainer.RegisterInstance(appContext as IServerContext);

                s_Logger.LogInfo($"Global.HostZBrain:{Global.HostZBrain};Client:{Global.Client};CVPort:{Global.CVPort}");
                if (Global.HostZBrain)
                {
                    var zbrain = BootContainer.Resolve<ZBrainProxyProvider>();
                    zbrain.Start();
                }
                // Start workers only after the gRPC endpoint is listening. HTTP proxies are
                // already waiting on the explicit one-time ADB readiness signal.
                s_WorkerTalkerRegistrar.Initialization();
                if (Global.HostEngine)
                {
                    BootContainer.Resolve<EngineProxyProvider>().Start();
                }
                Application.Run(appContext);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                powerStatusReporter?.Dispose();
                try
                {
                    s_WorkerTalkerRegistrar?.StopAsync().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    s_Logger?.LogError("停止 II.RPA.Core.Talker 失败", ex);
                }
                SystemInfoUtility.StopPowerSavingMonitor();
                FreeConsole();
            }
        }

        private static void Application_Idle(object sender, EventArgs e)
        {
            Application.Idle -= Application_Idle;
            #region Async output system and hardware infomation
            ///获取系统信息方法大约有1000ms的耗时、异步输出避免增加启动耗时
            Task.Run(() =>
            {
                try
                {
                    s_Logger?.LogInfo(LogBlock.Rows(
                        LogBlock.DefaultValueFormatter,
                        SystemInfoUtility.CpuInfo().Tag("CPU信息"),
                        SystemInfoUtility.OSInfo().Tag("操作系统信息"),
                        SystemInfoUtility.GetMemoryNumber().Tag("内存序列号"),
                        SystemInfoUtility.GetHardDiskNumber().Tag("磁盘序列号"),
                        SystemInfoUtility.MemoryInfo().Tag("内存信息")));

                    var ips = IPAddressUtils.GetLocalHostIps();
                    if (ips != null && ips.Any())
                    {
                        s_Logger?.LogInfo($"localhost-ip:{Environment.NewLine}{string.Join(Environment.NewLine, ips.Select(ip => ip.ToString()))}");
                    }
                    var vars = Environment.GetEnvironmentVariables();
                    s_Logger?.LogInfo("--Begin EnvironmentVariables--");
                    foreach (var key in vars.Keys)
                    {
                        if (key != null)
                        {
                            if (vars[key] != null)
                            {
                                s_Logger?.LogInfo($"{key} : {vars[key]}");
                            }
                            else
                            {
                                s_Logger?.LogInfo($"{key} : null");
                            }
                        }
                    }
                    s_Logger?.LogInfo("--End EnvironmentVariables--");
                    SystemInfoUtility.Dispose();
                }
                catch (Exception ex)
                {
                    s_Logger?.LogError(ex);
                }

            }).ConfigureAwait(false);
            #endregion
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            ExceptionHandler.Default?.Handle(e.Exception, "TaskScheduler_UnobservedTaskException");
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            ExceptionHandler.Default?.Handle(e.Exception, "Application_ThreadException");
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ExceptionHandler.Default?.Handle((Exception)e.ExceptionObject, "CurrentDomain_UnhandledException");
        }

        /// <summary>
        /// 使用配置文件完成容器初始化 - 推荐
        /// </summary>
        /// <param name="aContainer"></param>
        /// <returns></returns>
        private static IUnityContainer ConfigureContainer(IUnityContainer aContainer)
        {
            ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap
            {
                ExeConfigFilename = Path.Combine(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).FullName + "\\container.config")
            };
            Configuration configuration = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
            UnityConfigurationSection configSection = (UnityConfigurationSection)configuration.GetSection(UnityConfigurationSection.SectionName);
            return configSection.Configure(aContainer, "ServiceBootContainer");
        }

        internal const uint ATTACH_PARENT_PROCESS = 4294967295U;

        [DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        static extern bool AttachConsole(uint dwProcessId);

        [DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        static extern bool FreeConsole();
    }
}
