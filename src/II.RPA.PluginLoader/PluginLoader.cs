using II.RPA.Logging.Formatting;
using II.RPA.Plugin.Core;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Windows;
using System.Linq;
using System.Windows.Interop;
using System.Windows.Media;

namespace II.RPA.PluginLoader
{
    public class PluginLoader : MarshalByRefObject
    {
        private static ILogger logger = Logger.OfContext();
        private readonly object _assemblyResolveLock = new object();
        private readonly HashSet<string> _assemblySearchPaths =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private AsyncPluginRuntime _asyncRuntime;

        public PluginLoader() : this(null)
        {
        }

        internal PluginLoader(AsyncPluginRuntime asyncRuntime)
        {
            _asyncRuntime = asyncRuntime;
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

            //GlobleConfigurations.ZBrainServicePath = "http://127.0.0.1:6440";
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logger.LogError(e.ExceptionObject as Exception);
        }

        private AutoResetEvent _waitInit = new AutoResetEvent(false);
        public bool IsInitialized { get; private set; }

        public async Task<string> InitAsync()
        {
            logger.LogDebug($"runtime异步初始化——开始");
            Stopwatch stopwatch = Stopwatch.StartNew();
            IsInitialized = false;
            try
            {
                string pluginPath = Path.Combine(
                    Directory.GetParent(GetType().Assembly.Location).FullName,
                    "runtime");
                GlobleConfigurations.PluginPath = pluginPath;

                if (_asyncRuntime == null)
                {
                    logger.LogInformation($"pluginPath:{pluginPath}");

                    if (string.IsNullOrWhiteSpace(GlobleConfigurations.SupportDirectory))
                    {
                        GlobleConfigurations.SupportDirectory = ResolveSupportDirectory(
                            AppDomain.CurrentDomain.BaseDirectory);
                        logger.LogDebug($"SupportDirectory:{GlobleConfigurations.SupportDirectory}");
                    }

                    ConfigureAssemblySearchPaths(pluginPath);
                    PreloadingAssembly();
                    _asyncRuntime = AsyncPluginRuntime.FromDirectory(pluginPath);
                }

                await _asyncRuntime.InitAsync().ConfigureAwait(false);
                StartSetWinEventHook();
                IsInitialized = true;

                return new JMessage
                {
                    Message = "初始化完成"
                }.ToJson();
            }
            catch (Exception ex)
            {
                IsInitialized = false;
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(
                    logger,
                    ex,
                    "加载DLL信息异常");
                throw;
            }
            finally
            {
                logger.LogInformation(
                    $"runtime异步初始化——结束，耗时：{stopwatch.ElapsedMilliseconds}ms");
            }
        }

        public async Task<string> RunAsync(string pluginName, string action, string jsonParam)
        {
            Stopwatch sw = Stopwatch.StartNew();
            JMessage msg = new JMessage();
            NativeMethods.SetProcessDPIAware();
            try
            {
                pluginName = InterceptorFactory.Interceptor(pluginName, jsonParam);
                if (_encrptyList.Contains(pluginName))
                {
                    logger.LogInformation($@"========Start RunAsync(""{pluginName}"",""{action}"",""{EncryptUtils.Base64Encrypt(jsonParam)}"")");
                }
                else if (_ignoreParamList.Contains(pluginName))
                {
                    logger.LogInformation($@"========Start RunAsync(""{pluginName}"",""{action}"")");
                }
                else
                {
                    logger.LogInformation($@"========Start RunAsync(""{pluginName}"",""{action}"",""{jsonParam}"")");
                }

                if (_asyncRuntime == null)
                {
                    throw new RPAException(
                        eErrorMessageType.Error_PluginRun_ServerStatusUninit);
                }

                string result = await _asyncRuntime
                    .RunAsync(pluginName, action, jsonParam)
                    .ConfigureAwait(false);
                logger.LogInformation($@"========End RunAsync {pluginName} 返回：{result}");
                return result;
            }
            catch (RPAException e)
            {
                msg.Code = e.ErrorCode;
                msg.UnifyCode = e.UnifyErrorCode;
                msg.Message = e.ErrorMessage;
                logger.LogInformation($@"========End RunAsync Error：{pluginName} 返回：{LogBlock.Object(msg)}");
                return msg.ToJson();
            }
            catch (Exception ex)
            {
                var rpaEx = new RPAException(
                    eErrorMessageType.Error_PluginRunError,
                    eComponentType.None,
                    ex);
                msg.Code = rpaEx.ErrorCode;
                msg.UnifyCode = rpaEx.UnifyErrorCode;
                msg.Message = rpaEx.ErrorMessage;
                logger.LogInformation($@"========End RunAsync Error：{pluginName} 返回：{LogBlock.Object(msg)}");
                return msg.ToJson();
            }
            finally
            {
                logger.LogInformation($@"========End RunAsync：{pluginName} 耗时：{sw.ElapsedMilliseconds}");
            }
        }

        private readonly List<string> _encrptyList = new List<string> { "Database.TestDbConnection", "Database.CreateDbConnection", "RPASystem.RDPLock", "RPASystem.RDPUnlock", "RPASystem.RemoteUnLock", "RPASystem.LocalRemoteConnection" };
        private readonly List<string> _ignoreParamList = new List<string> { "Element.ProcessCreator" };
        public string Dispose(string pluginName)
        {
            return "";
        }
        public void SetSharedVariable(string jsonParam)
        {
            try
            {
                logger.LogInformation($"SetSharedVariable：{jsonParam}");

                // Installed deployments place RuntimeHost under <root>\rpa-engine, while local
                // builds run from <workspace>\bin\II.RPA.PluginLoader. Resolve both layouts before
                // parsing the payload so a non-standard executable path cannot silently discard
                // all shared variables.
                GlobleConfigurations.SupportDirectory = ResolveSupportDirectory(
                    AppDomain.CurrentDomain.BaseDirectory);

                Dictionary<string, object> dic = jsonParam.ToObject<Dictionary<string, object>>();
                ///671开始engine端传入的ClientMac等信息可能为null
                dic.Where(item => item.Value == null).ToList().ForEach(item => dic.Remove(item.Key));
                if (dic.ContainsKey("CLIENT -USER"))
                {
                    logger.LogDebug($"原 ClientUser：{GlobleConfigurations.ClientUser}");
                    GlobleConfigurations.ClientUser = System.Web.HttpUtility.UrlEncode(dic["CLIENT-USER"].ToString(), System.Text.Encoding.UTF8);
                    GlobleClintInfo.ClientUser = GlobleConfigurations.ClientUser;
                    logger.LogDebug($"ClientUser：{GlobleConfigurations.ClientUser}");
                }
                if (dic.ContainsKey("CLIENT-MAC"))
                {
                    GlobleConfigurations.ClientMac = System.Web.HttpUtility.UrlEncode(dic["CLIENT-MAC"].ToString(), System.Text.Encoding.UTF8);
                    GlobleClintInfo.ClientMac = GlobleConfigurations.ClientMac;
                    logger.LogDebug($"ClientMac：{GlobleConfigurations.ClientMac}");

                }
                if (dic.ContainsKey("IsRemoteCaptureServer"))
                {
                    bool.TryParse(dic["IsRemoteCaptureServer"].ToString(), out bool isRemoteCapture);
                    GlobleConfigurations.IsRemoteCaptureServer = isRemoteCapture;
                }
                if (dic.ContainsKey("token"))
                {
                    GlobleConfigurations.UserToken = dic["token"].ToString();
                }
                if (dic.ContainsKey("engineCode"))
                {
                    GlobleConfigurations.EngineCode = System.Web.HttpUtility.UrlEncode(dic["engineCode"].ToString(), System.Text.Encoding.UTF8);
                    logger.LogDebug($"engineCode：{GlobleConfigurations.EngineCode}");
                }
                if (dic.ContainsKey("ZBrainServicePath"))
                {
                    GlobleConfigurations.ZBrainServicePath = dic["ZBrainServicePath"].ToString();
                    logger.LogDebug($"ZBrainServicePath：{GlobleConfigurations.ZBrainServicePath}");
                }
                if (dic.ContainsKey("CoreServerPort"))
                {
                    GlobleConfigurations.CoreServerPort = Convert.ToInt32(dic["CoreServerPort"]);
                    logger.LogDebug($"CoreServerPort：{GlobleConfigurations.CoreServerPort}");
                }
                if (dic.ContainsKey("ClientType"))
                {
                    GlobleConfigurations.ClientType = (ClientType)Convert.ToInt32(dic["ClientType"]);
                    logger.LogDebug($"ClientType：{GlobleConfigurations.ClientType.ToString()}");
                }
                if (dic.ContainsKey("PackagePath"))
                {
                    logger.LogDebug($"Old PackageDirectory：{GlobleConfigurations.PackageDirectory}");
                    var packagePath = dic["PackagePath"].ToString();
                    bool isSamePackage = !string.IsNullOrEmpty(GlobleConfigurations.PackageDirectory)
                        && GlobleConfigurations.PackageDirectory == packagePath;
                    GlobleConfigurations.PackageDirectory = packagePath;
                    NeedResetObjectManager.ResetAllObjects(isSamePackage);
                    if (!isSamePackage)
                    {
                        //数据重置之后要进行依赖包配置检测升级判断
                        Plugin.Core.PluginBase.PluginConfiguration.ConfigFileManager.Instance.CheckUpgradeConfigFiles(null);
                        GlobleConfigurations.IsSyncProjectPathToZBrain = false;
                    }
                    logger.LogDebug($"New PackageDirectory：{GlobleConfigurations.PackageDirectory}");
                }
                if (dic.ContainsKey("RunningState"))
                {
                    GlobleConfigurations.RunningStateManager.SetRunningState((RunningState)Convert.ToInt32(dic["RunningState"]));
                    logger.LogDebug($"RunningState：{GlobleConfigurations.RunningStateManager.CurRunningState.ToString()}");
                }
                if (dic.ContainsKey("UseSZBrowser"))
                {
                    if (dic["UseSZBrowser"] != null
                        && bool.TryParse(dic["UseSZBrowser"].ToString(), out bool isUseSzBrowser))
                    {
                        GlobleConfigurations.UseSZBrowser = isUseSzBrowser;
                    }
                    else
                    {
                        GlobleConfigurations.UseSZBrowser = false;
                    }
                    logger.LogDebug($"UseSZBrowser：{GlobleConfigurations.UseSZBrowser}");
                }
                if (dic.ContainsKey("CLIENT-USERID"))
                {
                    GlobleConfigurations.ClientUserId = dic["CLIENT-USERID"].ToString();
                    logger.LogDebug($"{nameof(GlobleConfigurations.ClientUserId)}:{GlobleConfigurations.ClientUserId}");
                }
                if (dic.ContainsKey("CLIENT-TENANTCODE"))
                {
                    GlobleConfigurations.ClientTenantCode = dic["CLIENT-TENANTCODE"].ToString();
                    logger.LogDebug($"{nameof(GlobleConfigurations.ClientTenantCode)}:{GlobleConfigurations.ClientTenantCode}");
                }
                if (dic.ContainsKey("TarsAgentServiceUrl"))
                {
                    GlobleConfigurations.TarsAgentServiceUrl = dic["TarsAgentServiceUrl"].ToString();
                    logger.LogDebug($"{nameof(GlobleConfigurations.TarsAgentServiceUrl)}:{GlobleConfigurations.TarsAgentServiceUrl}");
                }
                if (dic.ContainsKey("TarsAgentServiceToken"))
                {
                    GlobleConfigurations.TarsAgentServiceToken = dic["TarsAgentServiceToken"].ToString();
                    logger.LogDebug($"{nameof(GlobleConfigurations.TarsAgentServiceToken)}:{GlobleConfigurations.TarsAgentServiceToken}");
                }
                if (dic.ContainsKey("EnableAiEnhancedScreenshot"))
                {
                    if (dic["EnableAiEnhancedScreenshot"] != null
                       && bool.TryParse(dic["EnableAiEnhancedScreenshot"].ToString(), out bool enableAiEnhancedScreenshot))
                    {
                        GlobleConfigurations.EnableAiEnhancedScreenshot = enableAiEnhancedScreenshot;
                    }
                    else
                    {
                        GlobleConfigurations.EnableAiEnhancedScreenshot = false;
                    }
                    logger.LogDebug($"EnableAiEnhancedScreenshot：{GlobleConfigurations.EnableAiEnhancedScreenshot}");
                }
                if (dic.TryGetValue("packageInfo", out object pkgObj) && pkgObj != null)
                {
                    GlobleConfigurations.PackageInfo = pkgObj;
                    logger.LogDebug($"{nameof(GlobleConfigurations.PackageInfo)}:{pkgObj}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
        }

        private static string ResolveSupportDirectory(string executePath)
        {
            var current = new DirectoryInfo(Path.GetFullPath(executePath));
            DirectoryInfo binDirectory = null;
            while (current != null)
            {
                if (string.Equals(current.Name, "rpa-engine", StringComparison.OrdinalIgnoreCase))
                {
                    return Path.Combine(current.Parent?.FullName ?? current.FullName, "support");
                }

                if (binDirectory == null &&
                    string.Equals(current.Name, "bin", StringComparison.OrdinalIgnoreCase))
                {
                    binDirectory = current;
                }

                current = current.Parent;
            }

            if (binDirectory != null)
            {
                return Path.Combine(
                    binDirectory.Parent?.FullName ?? binDirectory.FullName,
                    "support");
            }

            var baseDirectory = new DirectoryInfo(Path.GetFullPath(executePath));
            return Path.Combine(baseDirectory.Parent?.FullName ?? baseDirectory.FullName, "support");
        }

        public void RegisterMessageServiceSendMessage(Func<TerminalType, string, object, string> func)
        {
            MessageService.SendMessageToTerminal = func;
        }
        public void RegisterMessageServicePostMessage(Func<TerminalType, string, object, Action<object>, bool> func)
        {
            MessageService.PostMessageToTerminal = func;
        }
        public void RegisterMessageService_SendMessageToCoreServer(Func<SendMessageToCoeServerAction, object, string> func)
        {
            logger.LogDebug($"RegisterMessageService_SendMessageToCoreServer");
            MessageService.SendMessageToCoeServerFunc = func;
        }

        public void SetIsReStart(bool anIsReStart)
        {
            GlobleConfigurations.IsReStart = anIsReStart;
            logger.LogInformation($"restart flag:{anIsReStart}");
        }
        private void PreloadingAssembly()
        {
            logger.LogDebug($"预加载DLL信息");
            string basePath = Path.GetDirectoryName(this.GetType().Assembly.Location);
            // Init may be called again after a runtime restart.  Do not register
            // the same resolver more than once in the AppDomain.
            AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            string[] assemblyArray = {
                "II.RPA.IPC.dll",
                    "SuperSocket.ProtoBase.dll",
                    "SuperSocket.ClientEngine.dll",
                    "Gma.UserActivityMonitor.dll"
                };
            try
            {
                AppDomain.CurrentDomain.AppendPrivatePath(basePath);
                logger.LogInformation($"pluginLoader 应用程序域:{AppDomain.CurrentDomain.FriendlyName}");
                logger.LogInformation($"pluginLoader 应用程序域寻址:{AppDomain.CurrentDomain.BaseDirectory}");
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "最佳流程基础路径异常");
            }
            foreach (var dllName in assemblyArray)
            {
                try
                {
                    string dllPath = Path.Combine(basePath, dllName);
                    if (File.Exists(dllPath))
                    {
                        Assembly.LoadFrom(dllPath);
                    }
                    else
                    {
                        logger.LogDebug($"未预加载 {dllName} ,文件不存在");
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"预加载 {dllName}发生异常,在程序运行时会再次尝试加载。");
                }
            }
            logger.LogDebug($"预加载DLL信息完成");
        }

        private void ConfigureAssemblySearchPaths(string pluginPath)
        {
            lock (_assemblyResolveLock)
            {
                _assemblySearchPaths.Clear();
                AddAssemblySearchPath(Path.GetDirectoryName(GetType().Assembly.Location));
                AddAssemblyRootAndChildren(pluginPath);
            }
        }

        private void AddAssemblyRootAndChildren(string path)
        {
            AddAssemblySearchPath(path);
            if (!Directory.Exists(path))
            {
                return;
            }

            try
            {
                foreach (string directory in Directory.GetDirectories(
                    path,
                    "*",
                    SearchOption.TopDirectoryOnly))
                {
                    AddAssemblySearchPath(directory);
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(
                    logger,
                    ex,
                    $"扫描程序集目录失败:{path}");
            }
        }

        private void AddAssemblySearchPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                if (File.Exists(path))
                {
                    path = Path.GetDirectoryName(path);
                }

                if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                {
                    _assemblySearchPaths.Add(Path.GetFullPath(path));
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(
                    logger,
                    ex,
                    $"登记程序集目录失败:{path}");
            }
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName requestedName;
            try
            {
                requestedName = new AssemblyName(args.Name);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(
                    logger,
                    ex,
                    $"程序集依赖名称解析失败:{args.Name}");
                return null;
            }

            if (requestedName.Name.EndsWith(
                    ".resources",
                    StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string requestingAssemblyPath = args.RequestingAssembly?.Location;
            logger.LogDebug(
                $"触发程序集解析:{requestedName.FullName},请求程序集:{requestingAssemblyPath}");

            Assembly loadedAssembly = FindLoadedAssembly(requestedName);
            if (loadedAssembly != null)
            {
                logger.LogDebug(
                    $"程序集解析命中已加载程序集:{loadedAssembly.FullName},路径:{GetAssemblyLocation(loadedAssembly)}");
                return loadedAssembly;
            }

            string requestedSimpleName = requestedName.Name;
            string devExpressSimpleName = null;
            if (args.Name.Contains("DevExpress") && args.Name.Contains("v21.1"))
            {
                devExpressSimpleName = requestedSimpleName.Replace("v21.1", "v22.1");
            }

            foreach (string candidatePath in GetAssemblyCandidatePaths(
                requestedSimpleName,
                devExpressSimpleName,
                requestingAssemblyPath))
            {
                try
                {
                    AssemblyName candidateName = AssemblyName.GetAssemblyName(candidatePath);
                    bool isDevExpressRedirect =
                        devExpressSimpleName != null &&
                        string.Equals(
                            candidateName.Name,
                            devExpressSimpleName,
                            StringComparison.OrdinalIgnoreCase);

                    if (!isDevExpressRedirect &&
                        !IsAssemblyNameCompatible(candidateName, requestedName))
                    {
                        continue;
                    }

                    lock (_assemblyResolveLock)
                    {
                        loadedAssembly = FindLoadedAssembly(requestedName);
                        if (loadedAssembly != null)
                        {
                            return loadedAssembly;
                        }

                        Assembly resolvedAssembly = Assembly.LoadFrom(candidatePath);
                        logger.LogDebug(
                            $"程序集解析成功:{requestedName.FullName},路径:{candidatePath}");
                        return resolvedAssembly;
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(
                        logger,
                        ex,
                        $"程序集候选文件加载失败:{candidatePath}");
                }
            }

            if (IsOptionalAssembly(requestedName))
            {
                // Grpc.Core carries compile-time references to mobile platform
                // assemblies.  They are intentionally absent from the desktop
                // RuntimeHost and must not be reported as startup errors.
                logger.LogDebug(
                    $"可选程序集未找到:{requestedName.FullName},请求程序集:{requestingAssemblyPath}");
                return null;
            }

            logger.LogError(
                $"程序集解析失败:{requestedName.FullName},请求程序集:{requestingAssemblyPath}");
            return null;
        }

        private static bool IsOptionalAssembly(AssemblyName assemblyName)
        {
            return string.Equals(
                       assemblyName.Name,
                       "Xamarin.iOS",
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       assemblyName.Name,
                       "Mono.Android",
                       StringComparison.OrdinalIgnoreCase);
        }

        private IEnumerable<string> GetAssemblyCandidatePaths(
            string requestedSimpleName,
            string devExpressSimpleName,
            string requestingAssemblyPath)
        {
            var searchPaths = new List<string>();

            if (!string.IsNullOrWhiteSpace(requestingAssemblyPath))
            {
                string requestingDirectory =
                    Path.GetDirectoryName(requestingAssemblyPath);
                searchPaths.Add(requestingDirectory);

                try
                {
                    DirectoryInfo parentDirectory =
                        Directory.GetParent(requestingDirectory);
                    if (parentDirectory != null)
                    {
                        searchPaths.Add(parentDirectory.FullName);
                        searchPaths.AddRange(Directory.GetDirectories(
                            parentDirectory.FullName,
                            "*",
                            SearchOption.TopDirectoryOnly));
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(
                        logger,
                        ex,
                        $"扫描请求程序集同级目录失败:{requestingDirectory}");
                }
            }

            lock (_assemblyResolveLock)
            {
                searchPaths.AddRange(_assemblySearchPaths);
            }

            var candidateNames = new List<string> { requestedSimpleName };
            if (!string.IsNullOrWhiteSpace(devExpressSimpleName))
            {
                candidateNames.Add(devExpressSimpleName);
            }

            var yieldedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string directory in searchPaths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                foreach (string candidateName in candidateNames)
                {
                    string candidatePath = Path.Combine(
                        directory,
                        candidateName + ".dll");
                    if (yieldedPaths.Add(candidatePath) &&
                        File.Exists(candidatePath))
                    {
                        yield return candidatePath;
                    }
                }
            }
        }

        private static Assembly FindLoadedAssembly(AssemblyName requestedName)
        {
            foreach (Assembly loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (IsAssemblyNameCompatible(
                        loadedAssembly.GetName(),
                        requestedName))
                    {
                        return loadedAssembly;
                    }
                }
                catch (Exception)
                {
                    // Ignore dynamic or partially loaded assemblies.
                }
            }

            return null;
        }

        private static bool IsAssemblyNameCompatible(
            AssemblyName candidate,
            AssemblyName requested)
        {
            if (!string.Equals(
                    candidate.Name,
                    requested.Name,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            byte[] candidateToken = candidate.GetPublicKeyToken() ?? new byte[0];
            byte[] requestedToken = requested.GetPublicKeyToken() ?? new byte[0];
            if (!candidateToken.SequenceEqual(requestedToken))
            {
                return false;
            }

            string candidateCulture = candidate.CultureInfo?.Name ?? string.Empty;
            string requestedCulture = requested.CultureInfo?.Name ?? string.Empty;
            if (!string.Equals(
                    candidateCulture,
                    requestedCulture,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (requestedToken.Length > 0 &&
                requested.Version != null &&
                candidate.Version != null &&
                requested.Version != candidate.Version)
            {
                return false;
            }

            return true;
        }

        private static string GetAssemblyLocation(Assembly assembly)
        {
            try
            {
                return assembly.Location;
            }
            catch (NotSupportedException)
            {
                return "<dynamic>";
            }
        }
        //开启设置系统事件钩子应用程序
        public void StartSetWinEventHook()
        {
            Task.Run(() =>
            {
                try
                {
                    string operatingSystem = "";
                    using (ManagementObjectSearcher win32OperatingSystem = new ManagementObjectSearcher("select * from Win32_OperatingSystem"))
                    {
                        foreach (ManagementObject obj in win32OperatingSystem.Get())
                        {
                            operatingSystem = obj["Caption"].ToString();
                            break;
                        }
                    }
                    //windows server系统唤起此应用程序
                    if (operatingSystem.Contains("Windows Server"))
                    {
                        logger.LogDebug("开始唤起setWinEventHook应用程序");
                        System.Diagnostics.Process.Start("II.RPA.SetWinEventHook.exe");
                        logger.LogDebug("唤起setWinEventHook应用程序成功");
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "唤起setWinEventHook应用程序失败");
                }
            });
        }
    }
}

