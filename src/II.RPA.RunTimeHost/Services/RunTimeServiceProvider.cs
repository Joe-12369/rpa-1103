using Grpc.Core;
using II.RPA.Core.Common;
using GrpcDotNetNamedPipes;
using II.RPA.Communication.Hosting;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.PluginLoader;
using II.RPA.RuntimeHost.Configs;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rpa.Core.Runtimehost;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace II.RPA.RuntimeHost.Services
{
    /// <summary>
    /// RunTimeServiceProvider. 目前设计仅允许一个客户端连接.
    /// </summary>
    internal class RunTimeServiceProvider : RunTimeService.RunTimeServiceBase
    {
        private readonly static ILogger _logger = Logger.OfContext();
        private readonly PluginLoader.PluginLoader _pluginLoader;
        private readonly SynchronizationContext _uiSyncContext;
        
        private readonly ProcessServiceClientProvider _clientProvider;
        private readonly string[] _requireUiContextActions;
        private string _beepPeer;

        private bool _isRunning;
        private bool _isInited;
        private bool _isSetParam;
        private readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);
        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="anApplication"></param>
        /// <param name="aServer"></param>
        public RunTimeServiceProvider(IApplication anApplication,
            ProcessServiceClientProvider aClientProvider,
            bool anIsRestart)
        {
            _pluginLoader = new PluginLoader.PluginLoader();
            _clientProvider = aClientProvider;
            _uiSyncContext = anApplication.MainUISynchronizationContext;

            _pluginLoader.RegisterMessageServiceSendMessage(_clientProvider.SendMessageHandler);
            _pluginLoader.RegisterMessageServicePostMessage(_clientProvider.PostMessageHandler);
            _pluginLoader.RegisterMessageService_SendMessageToCoreServer(_clientProvider.SendToCoreserverHandler);
            _pluginLoader.SetIsReStart(anIsRestart);
            _requireUiContextActions = LoadRequireUiContextActions();
        }

        /// <summary>
        /// 当前客户端连接信息
        /// </summary>
        internal string CurrentPeer
        {
            get => _beepPeer;
        }
        /// <summary>
        /// BeepUnary
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task<BeepMessage> BeepUnary(BeepMessage request,
            ServerCallContext context)
        {
            try
            {
                if (string.IsNullOrEmpty(_beepPeer))
                {
                    _beepPeer = context.Peer;
                    _logger.LogInformation($"ConnectionBeeping: From - {_beepPeer}");
                }
                else
                {
                    _logger.LogDebug($"ExtraBeeping: From - {context.Peer}; Current - {_beepPeer} ");
                }
                if (!IsValidHeader(context.RequestHeaders))
                {
                    _logger.LogError("Token is invalid!");
                }
                return await Task.FromResult(new BeepMessage
                {
                    ProcessId = CommandParamsHelper.CurrentProcessId,
                    ClientId = CommandParamsHelper.ClientType.ToString(),
                    Payload = request.Payload,
                });
            }
            catch (Exception ex)
            {
                LoggingExtensions.LogError(_logger, ex, "BeepUnary Error");
                throw;
            }
        }
        /// <summary>
        /// InitUnary
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task<ReturnMessage> InitUnary(InitRequestMessage request,
            ServerCallContext context)
        {
            int requestId = request.GetHashCode();
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation($"=============InitUnary-{requestId}:start=============");
            try
            {
                PeerUniquenessCheck(context);
                var payload = request?.Payload ?? new InitRequestPayload();
                string initRet = await ExecuteInitAsync(payload, "GrpcInit").ConfigureAwait(false);
                return await Task.FromResult(new ReturnMessage
                {
                    Payload = initRet
                });

            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "InitUnary");
                _isInited = false;
                throw;
            }
            finally
            {
                _clientProvider.PackagePath = request?.Payload?.PackagePath;
                stopwatch.Stop();
                _logger.LogInformation($"=============InitUnary-{requestId}:end[{stopwatch.ElapsedMilliseconds}ms]=============");
            }
        }

        internal async Task AutoInitOnStartupAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("=============InitUnary-AutoStartup:start=============");
            try
            {
                await ExecuteInitAsync(new InitRequestPayload(), "AutoStartup").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "AutoInitOnStartup");
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogInformation($"=============InitUnary-AutoStartup:end[{stopwatch.ElapsedMilliseconds}ms]=============");
            }
        }

        private async Task<string> ExecuteInitAsync(InitRequestPayload payload, string source)
        {
            await _initLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_isInited)
                {
                    _logger.LogInformation($"Init skipped. Already initialized. source={source}");
                    return "{}";
                }

                _logger.LogDebug($"Init in CurrentThread; source={source}");
                var initRet = await _pluginLoader.InitAsync().ConfigureAwait(false);

                _clientProvider.PackagePath = payload?.PackagePath;
                _isInited = true;
                return initRet;
            }
            catch
            {
                _isInited = false;
                throw;
            }
            finally
            {
                _initLock.Release();
            }
        }
        /// <summary>
        /// SetParamUnary
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task<ReturnMessage> SetParamUnary(SetParamRequestMessage request,
            ServerCallContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            int requestId = request.GetHashCode();
            _logger.LogInformation($"=============SetParamUnary-{requestId}:start=============");
            try
            {
                PeerUniquenessCheck(context);
                _pluginLoader.SetSharedVariable(request.Payload.JsonParam);

                return await Task.FromResult(new ReturnMessage
                {
                    Payload = "{}"
                });
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "SetParamUnary");
                _isSetParam = false;
                throw;
            }
            finally
            {
                _isSetParam = true;
                stopwatch.Stop();
                _logger.LogInformation($"=============SetParamUnary-{requestId}:end-[{stopwatch.ElapsedMilliseconds}ms]=============");
            }
        }
        /// <summary>
        /// RunUnary
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task<ReturnMessage> RunUnary(RunRequestMessage request, ServerCallContext context)
        {
            using (WindowRpcDiagnostic.Begin(request.Payload.PluginName, context.RequestHeaders.FirstOrDefault(h => h.Key == WindowRpcDiagnostic.Header)?.Value))
            {
                long start = Stopwatch.GetTimestamp();
                if (WindowRpcDiagnostic.RequestId != null)
                    GrpcDotNetNamedPipes.NamedPipeRpcDiagnostic.Observer = WindowRpcDiagnostic.TransportMark;
                WindowRpcDiagnostic.Mark("runtime_request_enter");
                try
                {
                    return await RunUnaryTraced(request, context);
                }
                finally
                {
                    WindowRpcDiagnostic.Mark("runtime_method_exit", start);
                }
            }
        }

        private async Task<ReturnMessage> RunUnaryTraced(RunRequestMessage request, ServerCallContext context)
        {
            long diagnosticStart = Stopwatch.GetTimestamp();
            var stopwatch = Stopwatch.StartNew();
            int requestId = request.GetHashCode();
            var action = $"{request.Payload.PluginName}.{request.Payload.PluginAction}";
            _isRunning = true;
            _logger.LogInformation($"=============RunUnary-{requestId}-{action}:start=============");
            try
            {
                PeerUniquenessCheck(context);
                GlobleConfigurations.RunMode = request.Payload.RunMode;
                if (!string.IsNullOrWhiteSpace(GlobleConfigurations.RunMode))
                {
                    _logger.LogDebug($"RunMode:{GlobleConfigurations.RunMode}");
                    GlobleConfigurations.UseSZBrowser = GetUsesZBrowserValue();
                }
                _logger.LogInformation($"Agent 运行浏览器设置参数：{GlobleConfigurations.UseSZBrowser}");

                if (IsActionRequireUiContext(action))
                {
                    _logger.LogDebug($"Run in UIThread");
                    string runRet = await SynchronizationContextDispatcher.InvokeAsync(
                        _uiSyncContext,
                        () => _pluginLoader.RunAsync(
                            request.Payload.PluginName,
                            request.Payload.PluginAction,
                            request.Payload.PluginParam)).ConfigureAwait(false);
                    WindowRpcDiagnostic.Mark("runtime_plugin_completed", diagnosticStart);
                    return new ReturnMessage
                    {
                        Payload = runRet
                    };
                }
                else
                {
                    _logger.LogDebug($"Run in RequestThread");
                    string runRet = await _pluginLoader.RunAsync(
                        request.Payload.PluginName,
                        request.Payload.PluginAction,
                        request.Payload.PluginParam).ConfigureAwait(false);
                    WindowRpcDiagnostic.Mark("runtime_plugin_completed", diagnosticStart);
                    return new ReturnMessage
                    {
                        Payload = runRet
                    };
                }



            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "RunUnary");
                throw;
            }
            finally
            {
                stopwatch.Stop();
                if (Logger.Level <= LogLevel.Debug)
                {
                    ThreadPool.GetAvailableThreads(out int workerThreads, out int completionPortThreads);
                    ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCompletionPortThreads);
                    _logger.LogDebug($"#### ThreadPoolInfo ####"
                        + $"{Environment.NewLine}workerThreads           :{workerThreads}"
                        + $"{Environment.NewLine}completionPortThreads   :{completionPortThreads}"
                        + $"{Environment.NewLine}maxWorkerThreads        :{maxWorkerThreads}"
                        + $"{Environment.NewLine}maxCompletionPortThreads:{maxCompletionPortThreads}");
                }
                _logger.LogInformation($"=============RunUnary-{requestId}-{action}:end-[{stopwatch.ElapsedMilliseconds}ms]=============");
                _isRunning = false;
            }
        }
        /// <summary>
        /// ShutDownUnary
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task<ReturnMessage> ShutDownUnary(ShutDownRequestMessage request,
            ServerCallContext context)
        {
            int requestId = request.GetHashCode();
            _logger.LogInformation($"=============ShutDownUnary-{requestId}:start=============");
            try
            {
                PeerUniquenessCheck(context);
                await SaveAndCleanCoreAsync().ConfigureAwait(false);
                _logger.LogInformation($"=============ShutDownUnary-{requestId}:end=============");
                return new ReturnMessage
                {
                    Payload = "ShutDown Over!"
                };
            }
            catch (Exception ex)
            {
                LoggingExtensions.LogError(_logger, ex, "ShutDownUnary");
                throw;
            }
        }
        /// <summary>
        /// DisposeUnary
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task<ReturnMessage> DisposeUnary(DisposeRequestMessage request,
            ServerCallContext context)
        {
            int requestId = request.GetHashCode();
            _logger.LogInformation($"=============DisposeUnary-{requestId}:start=============");
            try
            {
                PeerUniquenessCheck(context);
                _logger.LogInformation($"=============DisposeUnary-{requestId}:end=============");
                return await Task.FromResult(new ReturnMessage
                {
                    Payload = "dispose done!"
                });
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "ShutDownUnary");
                throw;
            }
        }

        /// <summary>
        /// ServiceDebugUnary
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task<ReturnMessage> ServiceDebugUnary(ServiceDebugRequest request, ServerCallContext context)
        {
            ThreadPool.GetAvailableThreads(out int availableWorkerThreads, out _);
            JObject debugInfo = new JObject
            {
                { nameof(_beepPeer), _beepPeer },
                { nameof(_isInited), _isInited },
                { nameof(_isSetParam), _isSetParam },
                { nameof(_isRunning), _isRunning },
                { nameof(availableWorkerThreads), availableWorkerThreads },
            };
            if (request.Payload.NeedLog)
            {
                _logger.LogDebug($"{nameof(debugInfo)}:{debugInfo}");
            }
            return await Task.FromResult(new ReturnMessage
            {
                Payload = debugInfo.ToString()
            });
        }


        /// <summary>
        /// RunTimeMessageStreaming
        /// </summary>
        /// <param name="request"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task RunTimeMessageStreaming(RunTimeMessage request,
            IServerStreamWriter<RunTimeMessage> responseStream,
            ServerCallContext context)
        {
            int requestId = request.GetHashCode();
            _logger.LogInformation($"=============RunTimeMessageStreaming-{requestId}:start=============");
            try
            {
                PeerUniquenessCheck(context);
                //await base.RunTimeMessageStreaming(request, responseStream, context);
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    var response = request;
                    await responseStream.WriteAsync(response);
                    await Task.Delay(1000);
                }
                _logger.LogInformation($"=============RunTimeMessageStreaming-{requestId}:end=============");
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "RunTimeMessageStreaming");
                throw;
            }
        }
        private void PeerUniquenessCheck(ServerCallContext context)
        {
            // Peer reads the live pipe handle; finish the check before the request is disposed.
            try
            {
                if (context == null) return;
                var peer = context.Peer;
                if (!peer.EndsWith($"{CommandParamsHelper.Pid}"))
                    _logger.LogError($"{peer} can not match with {CommandParamsHelper.Pid}");
                if (string.IsNullOrEmpty(_beepPeer))
                {
                    _beepPeer = peer;
                    _logger.LogDebug($"_beepPeer is empty.Set it with {peer}");
                }
                else if (_beepPeer != peer)
                    _logger.LogWarning($"BeepPeer:{_beepPeer};IncomingPeer:{peer}");
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "PeerUniquenessCheck");
            }
        }

        private bool IsValidHeader(Metadata aHeaders)
        {
            try
            {
                byte[] token_bytes = null;
                int area1_int = 0;
                int area2_int = 0;
                int area3_int = 0;
                foreach (var entry in aHeaders)
                {
                    if (entry.IsBinary)
                    {
                        if (entry.Key == "token-bin")
                        {
                            _logger.LogDebug($"Header {entry.Key}");
                            token_bytes = entry.ValueBytes;
                            continue;
                        }
                        if (entry.Key == "area1-bin")
                        {
                            _logger.LogDebug($"Header {entry.Key}");
                            area1_int = BitConverter.ToInt32(entry.ValueBytes, 0);
                            continue;
                        }
                        if (entry.Key == "area2-bin")
                        {
                            _logger.LogDebug($"Header {entry.Key}");
                            area2_int = BitConverter.ToInt32(entry.ValueBytes, 0);
                            continue;
                        }
                        if (entry.Key == "area3-bin")
                        {
                            _logger.LogDebug($"Header {entry.Key}");
                            area3_int = BitConverter.ToInt32(entry.ValueBytes, 0);
                            continue;
                        }
                        _logger.LogDebug($"Invalid Header {entry.Key}");
                        break;
                    }
                    else
                    {
                        _logger.LogDebug($"Invalid Header {entry.Key} : {entry.Value}");
                        break;
                    }
                }

                if (token_bytes == null)
                {
                    _logger.LogWarning($"token-bin is null!");
                    return false;
                }
                if (area1_int <= 0 || area2_int <= 0 || area3_int <= 0)
                {
                    _logger.LogWarning($"area error:{area1_int};{area2_int};{area3_int}");
                    return false;
                }

                var token_part1 = Encoding.UTF8.GetString(token_bytes.Take(area1_int).ToArray());
                var token_part2 = BitConverter.ToInt32(token_bytes.Skip(Math.Max(0, area1_int)).ToArray(), 0);

                if (token_part1.Equals(CommandParamsHelper.PipeName)
                    && token_part2 == CommandParamsHelper.Pid)
                {
                    _logger.LogInformation($"Token is verified");
                    return true;
                }
                else
                {
                    _logger.LogError($"Token is not verified: {token_part1} {token_part2}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                return false;
            }
        }
        private bool GetUsesZBrowserValue()
        {
            try
            {
                _logger.LogDebug($"Agent Run Mode:{GlobleConfigurations.RunMode}");
                if (GlobleConfigurations.RunMode?.Equals("normal") == false)
                {
                    return false;
                }
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string filePath = Path.Combine(appDataPath, "z-factory", "storage.json");

                if (!File.Exists(filePath))
                {
                    return false;
                }
                string jsonContent = File.ReadAllText(filePath);
                JObject jsonObject = JObject.Parse(jsonContent);
                if (jsonObject.ContainsKey("useSZBrowser"))
                {
                    return jsonObject["useSZBrowser"].ToObject<bool>();
                }
                return true;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, $"读取静默运行浏览器参数异常");
                return false;
            }
        }
        internal async Task SaveAndCleanCoreAsync()
        {
            //重启前执行Excel文件保存操作
            await _pluginLoader.RunAsync("Excel.SaveInstance", "Excute", new
            {               
            }.ToJson()).ConfigureAwait(false);
        }

        private string[] LoadRequireUiContextActions()
        {
            try
            {
                string configPath = GetConfigFilePath();
                var defaultActions = GetDefaultUiContextActions().ToList();

                if (!File.Exists(configPath))
                {
                    _logger.LogInformation($"action config was not found: {configPath}, use default instead!");
                    return defaultActions.ToArray();
                }

                string jsonContent = File.ReadAllText(configPath);
                var config = JsonConvert.DeserializeObject<UiContextActionsConfig>(jsonContent);

                if (config?.RequireUiContextActions == null || config.RequireUiContextActions.Count == 0)
                {
                    _logger.LogWarning("RequireUiContextActions was not found, use default instead.");
                    return defaultActions.ToArray();
                }

                var mergedActions = defaultActions
                    .Union(config.RequireUiContextActions)
                    .Distinct()
                    .ToArray();

                var addedFromConfig = config.RequireUiContextActions.Except(defaultActions).ToArray();
                var missingFromConfig = defaultActions.Except(config.RequireUiContextActions).ToArray();

                if (addedFromConfig.Length > 0)
                {
                    _logger.LogDebug($"Added from config: {string.Join(", ", addedFromConfig)}");
                }
                _logger.LogInformation($"Loaded {mergedActions.Length} UI context actions " +
                                      $"(default: {defaultActions.Count}, config: {config.RequireUiContextActions.Count}, merged: {mergedActions.Length})");

                return mergedActions;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "Fail to read RequireUiContextActions. use default instead.");
                return GetDefaultUiContextActions();
            }
        }


        private string GetConfigFilePath()
        {
            string appDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDirectory, "Z-Core", "ui_actions.json");
        }

        private string[] GetDefaultUiContextActions()
        {
            return Array.Empty<string>();
        }
        private bool IsActionRequireUiContext(string action)
        {
            if (string.IsNullOrEmpty(action))
                return false;

            foreach (var pattern in _requireUiContextActions)
            {
                if (IsPatternMatch(action, pattern))
                {
                    _logger.LogDebug($"Action '{action}' matched pattern '{pattern}', requires UI context");
                    return true;
                }
            }

            return false;
        }

        private bool IsPatternMatch(string action, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return false;


            if (action.Equals(pattern, StringComparison.OrdinalIgnoreCase))
                return true;

            if (pattern.Contains('*') || pattern.Contains('?'))
            {
                string regexPattern = "^" + Regex.Escape(pattern)
                    .Replace("\\*", ".*")
                    .Replace("\\?", ".") + "$";

                try
                {
                    return Regex.IsMatch(action, regexPattern, RegexOptions.IgnoreCase);
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, ex, $"Invalid pattern '{pattern}', fallback to exact match");
                    return action.Equals(pattern, StringComparison.OrdinalIgnoreCase);
                }
            }
            if (pattern.StartsWith("^") || pattern.EndsWith("$") || pattern.Contains("[") || pattern.Contains("("))
            {
                try
                {
                    return Regex.IsMatch(action, pattern, RegexOptions.IgnoreCase);
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, ex, $"Invalid regex pattern '{pattern}', fallback to exact match");
                    return action.Equals(pattern, StringComparison.OrdinalIgnoreCase);
                }
            }

            return false;
        }
    }
}

