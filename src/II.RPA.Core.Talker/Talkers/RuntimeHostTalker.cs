using Grpc.Core;
using II.RPA.Communication.Endpoints;
using II.RPA.Communication.Hosting;
using II.RPA.Communication.Options;
using II.RPA.Core.Log;
using II.RPA.Core.Models;
using II.RPA.Core.Contracts.Talker;
using II.RPA.Core.Runtime;
using II.RPA.Core.Talker.Processes;
using II.RPA.Core.Talker.Timing;
using II.RPA.Core.Talker.RuntimeHost.Protos.Runtime;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text;
using static II.RPA.Core.Talker.RuntimeHost.Protos.Runtime.RunTimeService;

namespace II.RPA.Core.Talker.Talkers
{
    public class RuntimeHostTalker : BaseTalker
    {
        private readonly RuntimeHostProcess _runtimeHostProcess;
        private readonly ITalkerManager _talkerManager;
        private readonly ConcurrentDictionary<string, ResettableTimeout> m_CtsTimeoutMap;
        private readonly Metadata _headers;
        private readonly int _minTimeout = 200;
        private static readonly TimeSpan RuntimeHostControlCallTimeout = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan LongRunningRuntimeHostCallTimeout = TimeSpan.FromHours(24);
        private readonly bool _startProcess;
        private readonly IRpaRuntimeContext _runtimeContext;
        private readonly SemaphoreSlim _initializationGate = new(1, 1);
        private readonly SemaphoreSlim _runGate = new(1, 1);
        private readonly object _runtimeClientGate = new();
        private TaskCompletionSource<bool> _runtimeClientReady = NewRuntimeClientCompletion();

        private GrpcClient? _namePipeGrpcClient;
        private bool _isParamSet;

        public RuntimeHostTalker(ITalkerManager talkerManager, ILogger logger, IRpaRuntimeContext runtimeContext, string? pipeName = null, bool startProcess = true) : base(logger)
        {
            _talkerManager = talkerManager;
            _runtimeContext = runtimeContext ?? throw new ArgumentNullException(nameof(runtimeContext));
            _runtimeHostProcess = new RuntimeHostProcess(logger, runtimeContext, pipeName);
            _runtimeHostProcess.OnProcessStatusChangedEvent += (_, status) =>
            {
                if (status == Models.ProcessStatus.Stopped)
                {
                    ResetInitializationState();
                }
            };
            _startProcess = startProcess;

            m_CtsTimeoutMap = new ConcurrentDictionary<string, ResettableTimeout>();
            byte[] bytes1 = Encoding.UTF8.GetBytes(_runtimeHostProcess.PipeName);
            byte[] bytes2 = BitConverter.GetBytes(_runtimeContext.CurrentProcessId);
            var tokenbyte = ConcatBytes(bytes1, bytes2);
            _headers = new Metadata
            {
                { "token-bin", tokenbyte },
                { "area1-bin", BitConverter.GetBytes(bytes1.Length) },
                { "area2-bin", BitConverter.GetBytes(bytes2.Length) },
                { "area3-bin", BitConverter.GetBytes(tokenbyte.Length) },
            };
        }

        public RunTimeServiceClient RunTimeServiceClient { get; private set; }

        public string? CurrentPackagePath { get; private set; }

        public bool IsInited { get; private set; }

        public override string Name => "runtimehost";

        public override Task Start()
        {
            if (_startProcess)
            {
                _runtimeHostProcess.Start();
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// 仅用于基座灰盒验证：在不依赖 Worker 注册流的情况下建立命名管道客户端。
        /// 生产运行仍由 RegisterTalker 生命周期调用同一连接逻辑。
        /// </summary>
        public Task ConnectForVerificationAsync()
        {
            if (!EstablishConnection())
            {
                throw LastFault ?? new InvalidOperationException("RuntimeHost 命名管道连接失败。");
            }
            return Task.CompletedTask;
        }

        public override async Task Stop()
        {
            try
            {
                if (_startProcess)
                {
                    await _runtimeHostProcess.StopAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                await MarkRuntimeClientUnavailableAsync(
                    new OperationCanceledException("RuntimeHost 已停止。"))
                    .ConfigureAwait(false);
                await base.Stop().ConfigureAwait(false);
            }
        }

        protected override async Task HandleFrameStream(FrameMessage frameMessage)
        {
            if (!_talkerManager.TryGetTalker<HostTalker>(out var hostTalker))
            {
                hostTalker = _talkerManager.GetTalkers<HostTalker>(readyOnly: false).FirstOrDefault()
                    ?? throw new InvalidOperationException("HostTalker 未注册");
            }
            await hostTalker.Notify(frameMessage);
        }

        protected override bool ChangeTalkerStatusToReady()
        {
            GrpcClient namePipeGrpcClient;
            RunTimeServiceClient runtimeServiceClient;
            try
            {
                namePipeGrpcClient = new GrpcClient(new ClientOptions()
                {
                    Endpoint = new NamedPipeEndpoint(_runtimeHostProcess.PipeName),
                    NamedPipeDiagnosticLog = message => Logger.LogDebug($"[RuntimeHostPipe] {message}")
                });

                runtimeServiceClient = namePipeGrpcClient.CreateClient(
                    invoker => new RunTimeServiceClient(invoker));
            }
            catch (Exception ex)
            {
                Logger.LogWarning("创建 RuntimeHost 命名管道客户端失败", ex);
                RunTimeServiceClient = null!;
                return false;
            }

            lock (_runtimeClientGate)
            {
                _namePipeGrpcClient = namePipeGrpcClient;
                RunTimeServiceClient = runtimeServiceClient;
                _runtimeClientReady.TrySetResult(true);
            }

            return true;
        }

        protected override void OnConnectionClosed(Exception? exception)
        {
            _ = MarkRuntimeClientUnavailableAsync(
                exception ?? new IOException("RuntimeHost Worker 连接已终止。"));
        }

        public async Task<string> InitUnaryAsync(
            string packagePath,
            string installationPath = "",
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);
            await EnsureRuntimeClientAsync(cancellationToken).ConfigureAwait(false);

            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                CancellationToken);
            await _initializationGate.WaitAsync(linkedCancellation.Token).ConfigureAwait(false);
            try
            {
                if (IsInited && string.Equals(CurrentPackagePath, packagePath, StringComparison.OrdinalIgnoreCase))
                {
                    return string.Empty;
                }

                var response = await RunTimeServiceClient.InitUnaryAsync(
                    new InitRequestMessage
                    {
                        ClientId = "self",
                        IsAsync = false,
                        NeedUiThread = true,
                        Payload = new InitRequestPayload
                        {
                            PackagePath = packagePath,
                            InstallDirPath = installationPath ?? string.Empty
                        }
                    },
                    headers: _headers,
                    deadline: DateTime.UtcNow.Add(RuntimeHostControlCallTimeout),
                    cancellationToken: linkedCancellation.Token).ResponseAsync.ConfigureAwait(false);

                CurrentPackagePath = packagePath;
                IsInited = true;
                _isParamSet = false;
                return response.Payload;
            }
            finally
            {
                _initializationGate.Release();
            }
        }

        public bool ResetPluginLoaderRunTimeout(string payloadMessage)
        {
            if (string.IsNullOrWhiteSpace(payloadMessage))
            {
                return false;
            }

            RuntimeHostTimeoutMessage? message;
            try
            {
                message = JsonConvert.DeserializeObject<RuntimeHostTimeoutMessage>(payloadMessage);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("解析 RuntimeHost 超时续期请求失败", ex);
                return false;
            }

            if (message == null)
            {
                return false;
            }

            return ResetPluginLoaderRunTimeout(new RuntimeHostTimeoutExtension
            {
                RunUniqueId = message.RunUniqueID,
                NewTimeout = message.NewTimeout,
                TimeoutMessage = message.TimeoutMsg
            });
        }

        public bool ResetPluginLoaderRunTimeout(RuntimeHostTimeoutExtension extension)
        {
            ArgumentNullException.ThrowIfNull(extension);
            if (string.IsNullOrWhiteSpace(extension.RunUniqueId) ||
                !m_CtsTimeoutMap.TryGetValue(extension.RunUniqueId, out var timeout))
            {
                return false;
            }

            var remaining = Math.Max(
                0,
                (long)timeout.Timeout.TotalMilliseconds - timeout.ElapsedMilliseconds);
            timeout.Reset(
                Math.Max(Math.Max(0, extension.NewTimeout), remaining),
                extension.TimeoutMessage);
            return true;
        }

        public async Task RestartRuntimeAsync(
            bool dueToTimeout = false,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _runGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await RestartRuntimeCoreAsync(dueToTimeout).ConfigureAwait(false);
            }
            finally
            {
                _runGate.Release();
            }
        }

        private async Task<bool> EnsureInitializedAndConfiguredAsync(
            string packagePath,
            bool autoInit,
            CancellationToken cancellationToken)
        {
            await EnsureRuntimeClientAsync(cancellationToken).ConfigureAwait(false);
            if ((!IsInited || !string.Equals(CurrentPackagePath, packagePath, StringComparison.OrdinalIgnoreCase)) && autoInit)
            {
                Logger.LogInfo($"RuntimeHost 自动初始化流程包：{packagePath}");
                await InitUnaryAsync(packagePath, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            if (!IsInited || !string.Equals(CurrentPackagePath, packagePath, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!_isParamSet && !string.IsNullOrWhiteSpace(_runtimeContext.UserInfoParameter))
            {
                await SetParam(_runtimeContext.UserInfoParameter, packagePath, cancellationToken).ConfigureAwait(false);
            }
            return true;
        }

        private async Task EnsureRuntimeClientAsync(CancellationToken cancellationToken)
        {
            Task readyTask;
            lock (_runtimeClientGate)
            {
                if (RunTimeServiceClient != null)
                {
                    return;
                }
                readyTask = _runtimeClientReady.Task;
            }

            await readyTask
                .WaitAsync(TimeSpan.FromSeconds(30), cancellationToken)
                .ConfigureAwait(false);
            if (RunTimeServiceClient == null)
            {
                throw new InvalidOperationException("RuntimeHost 尚未建立命名管道连接。");
            }
        }

        private void ResetInitializationState()
        {
            CurrentPackagePath = null;
            IsInited = false;
            _isParamSet = false;
        }

        public async Task<string> DisposeUnaryAsync(
            string packagePath,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);
            await EnsureRuntimeClientAsync(cancellationToken).ConfigureAwait(false);

            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                CancellationToken);
            await _runGate.WaitAsync(linkedCancellation.Token).ConfigureAwait(false);
            try
            {
                var response = await RunTimeServiceClient.DisposeUnaryAsync(
                    new DisposeRequestMessage
                    {
                        ClientId = "self",
                        IsAsync = false,
                        NeedUiThread = false,
                        Payload = new DisposeRequestPayload
                        {
                            PackagePath = packagePath
                        }
                    },
                    cancellationToken: linkedCancellation.Token,
                    deadline: DateTime.UtcNow.Add(RuntimeHostControlCallTimeout),
                    headers: _headers).ResponseAsync.ConfigureAwait(false);
                return response.Payload;
            }
            finally
            {
                _runGate.Release();
            }
        }

        public async Task<object> RunUnaryAsync(
            string aPackagePath,
            string aPluginName,
            string anAction,
            string aJsonParam,
            RuntimeRunParameters runParamModel,
            bool anIsNeedCancel = true,
            bool anIsNeedAutoInit = true,
            string aRunMode = "",
            PluginRunOptions pluginRunOptions = null,
            CancellationToken cancellationToken = default)
        {
            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                CancellationToken);
            await _runGate.WaitAsync(linkedCancellation.Token).ConfigureAwait(false);
            try
            {
                if (!await EnsureInitializedAndConfiguredAsync(
                    aPackagePath,
                    anIsNeedAutoInit,
                    linkedCancellation.Token).ConfigureAwait(false))
                {
                    return JsonConvert.SerializeObject(new
                    {
                        code = $"{(int)eGuiType.CoreServer}000{(int)eErrorMessageType.Error_PluginRun_ServerStatusInvalid}",
                        message = "RuntimeHost 正在初始化，请稍后重试",
                        info = $"{_runtimeHostProcess.Status}",
                        extend = ""
                    });
                }

                return await RunUnaryCoreAsync(
                    aPackagePath,
                    aPluginName,
                    anAction,
                    aJsonParam,
                    runParamModel,
                    anIsNeedCancel,
                    anIsNeedAutoInit,
                    aRunMode,
                    pluginRunOptions,
                    linkedCancellation.Token).ConfigureAwait(false);
            }
            finally
            {
                _runGate.Release();
            }
        }

        private async Task<object> RunUnaryCoreAsync(
            string aPackagePath,
            string aPluginName,
            string anAction,
            string aJsonParam,
            RuntimeRunParameters runParamModel,
            bool anIsNeedCancel,
            bool anIsNeedAutoInit,
            string aRunMode,
            PluginRunOptions pluginRunOptions,
            CancellationToken operationToken)
        {
            await EnsureRuntimeClientAsync(operationToken).ConfigureAwait(false);
            pluginRunOptions ??= new PluginRunOptions { IsAsync = false, NeedUiThread = true };
            Logger.LogInfo($"PluginLoaderRunRaw:{aPluginName}.{anAction};RemainTime:{runParamModel.RemainTime};TotalTime:{runParamModel.TotalTime};NeedAutoInit:{anIsNeedAutoInit}");
            try
            {
                var jsonParam = aJsonParam;
                var isInteractiveCapture = InteractiveCapturePolicy.IsPluginAction(aPluginName, anAction);
                var aTimeOut = isInteractiveCapture ? -1 : runParamModel.RemainTime;
                if (isInteractiveCapture)
                {
                    runParamModel.RemainTime = -1;
                    runParamModel.TotalTime = -1;
                }
                var overHead = DateTime.Now - runParamModel.EntryTime;
                Logger.LogInfo($"Pre-execution cost:{overHead}");
                runParamModel.StartTimeStamp = Stopwatch.GetTimestamp().ToString();
                var ctsTimeout = new ResettableTimeout(aTimeOut, Logger);
                //增加运行唯一标识参数
                jsonParam = AddUniqueIDIntoJsonParam(jsonParam, ctsTimeout, runParamModel);

                ///无限超时
                if (aTimeOut == -1)
                {
                    try
                    {
                        var runRequest = new RunRequestMessage()
                        {
                            IsAsync = pluginRunOptions.IsAsync,
                            NeedUiThread = pluginRunOptions.NeedUiThread,
                            Payload = new RunRequestPayload
                            {
                                PackagePath = aPackagePath,
                                PluginName = aPluginName,
                                PluginAction = anAction,
                                PluginParam = jsonParam,
                                PluginVersion = "",
                                RunMode = aRunMode
                            }
                        };
                        
                        var runRet = await RunTimeServiceClient.RunUnaryAsync(
                            runRequest,
                            headers: _headers,
                            deadline: isInteractiveCapture ? DateTime.MaxValue : DateTime.UtcNow.Add(LongRunningRuntimeHostCallTimeout),
                            cancellationToken: operationToken);
                        return runRet.Payload;
                    }
                    catch (OperationCanceledException) when (operationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"RunAsync Error", ex);
                        return JsonConvert.SerializeObject(new
                        {
                            code = $"{(int)eGuiType.CoreServer}000{(int)eErrorMessageType.Error_PluginRunError}",
                            message = $"系统异常，请重试。若仍未解决，请联系运营人员。",
                            info = $"{_runtimeHostProcess.Status}",
                            extend = ""
                        });
                    }
                }
                ///普通情况
                else
                {
                    if (aTimeOut < _minTimeout &&
                        (runParamModel.RemainTime == runParamModel.TotalTime || !runParamModel.EnableAIRepair))
                    {
                        Logger.LogError($"{aTimeOut} is too short. Timeout directly.");
                        return JsonConvert.SerializeObject(new
                        {
                            code = $"{(int)eGuiType.CoreServer}000{(int)eErrorMessageType.Error_PluginRun_TimeOut}",
                            message = "设置的超时时间过短，请重新调整超时时间",
                            info = $"{_runtimeHostProcess.Status}",
                            extend = $"{ctsTimeout.HasReset}"
                        });
                    }

                    if (aTimeOut < _minTimeout)
                    {
                        runParamModel.EnableNormal = false;
                    }
                    if (aTimeOut >= 10000)
                    {
                        //超时时间足够大的情况 +1000ms buffer
                        aTimeOut += 1000;
                    }
                    aTimeOut += _minTimeout;
                    Logger.LogDebug($"Timeout Formated:{aTimeOut}");
                    ctsTimeout.Timeout = TimeSpan.FromMilliseconds(aTimeOut);

                    try
                    {
                        m_CtsTimeoutMap.TryAdd(ctsTimeout.UniqueId, ctsTimeout);
                        //组件执行委托
                        Func<CancellationToken, Task<string>> runFunc = async runToken =>
                        {
                            try
                            {
                                Stopwatch sw = Stopwatch.StartNew();
                                RunRequestMessage runRequest = new RunRequestMessage
                                {
                                    IsAsync = false,
                                    NeedUiThread = true,
                                    Payload = new RunRequestPayload
                                    {
                                        PackagePath = aPackagePath,
                                        PluginName = aPluginName,
                                        PluginAction = anAction,
                                        PluginParam = jsonParam,
                                        PluginVersion = "",
                                        RunMode = aRunMode
                                    }
                                };
                                // The resettable business timeout owns this call's lifetime; a fixed RPC
                                // deadline would still expire at the original budget after a renewal.
                                using var runCall = RunTimeServiceClient.RunUnaryAsync(
                                    runRequest,
                                    headers: _headers,
                                    deadline: DateTime.MaxValue,
                                    cancellationToken: runToken);
                                var runRet = await runCall.ResponseAsync.ConfigureAwait(false);
                                sw.Stop();
                                Logger.LogDebug($"RunAsync cost:{sw.ElapsedMilliseconds}ms!");
                                return runRet.Payload;
                            }
                            catch (OperationCanceledException) when (runToken.IsCancellationRequested)
                            {
                                throw;
                            }
                            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled &&
                                                          runToken.IsCancellationRequested)
                            {
                                throw new OperationCanceledException(
                                    "RuntimeHost execution was cancelled.",
                                    ex,
                                    runToken);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError($"RunAsync Error", ex);
                                return JsonConvert.SerializeObject(new
                                {
                                    code = $"{(int)eGuiType.CoreServer}000{(int)eErrorMessageType.Error_PluginRunError}",
                                    message = $"执行失败，请重试。若仍未解决，请联系运营人员。",
                                    info = $"{_runtimeHostProcess.Status}",
                                    extend = ""
                                });
                            }
                        };

                        //超时回调委托
                        Func<TimeSpan, Task> onTimeoutAsync = async time =>
                        {
                            Logger.LogWarning($"onTimeoutAsync Timeout: {time.TotalMilliseconds} ms");
                            try
                            {
                                var shouldRestart = ShouldRestartAfterTimeout(
                                    _runtimeContext,
                                    anIsNeedCancel,
                                    aPackagePath);
                                if (shouldRestart)
                                {
                                    Logger.LogWarning("Restart process");
                                    await RestartRuntimeCoreAsync(dueToTimeout: true).ConfigureAwait(false);
                                }
                                else
                                {
                                    Logger.LogWarning($"Skip RuntimeHost restart: needCancel={anIsNeedCancel}, package={aPackagePath}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError("TimeOutRestart Error", ex);
                            }
                        };

                        //开始执行
                        var retExec = await ctsTimeout.ExecuteAsync(
                            runFunc,
                            operationToken,
                            onTimeoutAsync).ConfigureAwait(false);
                        Logger.LogInfo("RunAsync End");

                        return retExec;
                    }
                    catch (TimeoutException)
                    {
                        Logger.LogWarning($"Catch TimeoutException!");
                        // 这里刻意不使用 operationToken：真超时后必须把"执行超时"结果回给调用方。
                        // 引擎自身的超时往往同时取消了本次请求，若用已取消的 token 等待，
                        // Task.Delay 会抛 TaskCanceledException，把"超时"降级成"任务被取消"
                        // （错误码会从 Error_PluginRun_TimeOut 变成系统取消，重试语义也跟着错）。
                        // 这 500ms 只是等 RuntimeHost 重启落地的宽限期，与调用方取消无关。
                        await Task.Delay(500).ConfigureAwait(false);
                        return JsonConvert.SerializeObject(new
                        {
                            code = $"{(int)eGuiType.CoreServer}000{(int)eErrorMessageType.Error_PluginRun_TimeOut}",
                            message = string.IsNullOrEmpty(ctsTimeout.TimeoutMessage) ? $"执行超时，请尝试增加超时时间 。" : ctsTimeout.TimeoutMessage,
                            info = $"{_runtimeHostProcess.Status}",
                            extend = $"{ctsTimeout.HasReset}"
                        });
                    }

                    finally
                    {
                        m_CtsTimeoutMap.TryRemove(ctsTimeout.UniqueId, out _);
                    }
                }
            }
            catch (OperationCanceledException) when (operationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogWarning("PluginLoaderRunRaw Error", ex);
                return JsonConvert.SerializeObject(new
                {
                    code = $"{(int)eGuiType.CoreServer}000{(int)eErrorMessageType.Error_PluginRunError}",
                    message = $"系统异常，请重试。若仍未解决，请联系运营人员。",
                    info = $"{_runtimeHostProcess.Status}",
                    extend = ""
                });
            }
        }

        public async Task SetParam(
            string aParam,
            string? packagePath = null,
            CancellationToken cancellationToken = default)
        {
            await EnsureRuntimeClientAsync(cancellationToken).ConfigureAwait(false);
            packagePath ??= CurrentPackagePath;
            if (!string.IsNullOrWhiteSpace(packagePath))
            {
                aParam = AddPackagePath(aParam, packagePath);
            }

            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                CancellationToken);
            Logger.LogDebug($"SetParamsAsync start");
            SetParamRequestMessage setParamsRequest = new SetParamRequestMessage
            {
                IsAsync = false,
                NeedUiThread = true,
                Payload = new SetParamRequestPayload
                {
                    JsonParam = aParam
                }
            };
            await RunTimeServiceClient.SetParamUnaryAsync(
                setParamsRequest,
                cancellationToken: linkedCancellation.Token,
                deadline: DateTime.UtcNow.Add(RuntimeHostControlCallTimeout),
                headers: _headers).ResponseAsync.ConfigureAwait(false);
            _isParamSet = true;
            Logger.LogDebug($"SetParamsAsync over");
        }

        private async Task RestartRuntimeCoreAsync(bool dueToTimeout)
        {
            await MarkRuntimeClientUnavailableAsync(
                new OperationCanceledException("RuntimeHost 正在重启。"))
                .ConfigureAwait(false);
            await _runtimeHostProcess.RestartAsync(dueToTimeout).ConfigureAwait(false);
        }

        private async Task MarkRuntimeClientUnavailableAsync(Exception exception)
        {
            GrpcClient? client;
            TaskCompletionSource<bool> previousCompletion;
            lock (_runtimeClientGate)
            {
                client = _namePipeGrpcClient;
                _namePipeGrpcClient = null;
                RunTimeServiceClient = null!;
                previousCompletion = _runtimeClientReady;
                _runtimeClientReady = NewRuntimeClientCompletion();
                ResetInitializationState();
            }

            previousCompletion.TrySetException(exception);
            if (client != null)
            {
                try
                {
                    await client.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception disposeException)
                {
                    Logger.LogWarning("释放 RuntimeHost 命名管道客户端失败", disposeException);
                }
            }
        }

        private static TaskCompletionSource<bool> NewRuntimeClientCompletion() =>
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private static string AddPackagePath(string parameterJson, string packagePath)
        {
            var parameter = string.IsNullOrWhiteSpace(parameterJson)
                ? new JObject()
                : JObject.Parse(parameterJson);
            if (!parameter.ContainsKey("PackagePath"))
            {
                parameter.Add("PackagePath", packagePath);
            }
            return parameter.ToString(Formatting.None);
        }

        private string AddUniqueIDIntoJsonParam(string jsonParam, ResettableTimeout aCtsTimeout, RuntimeRunParameters runParamModel)
        {
            try
            {
                JObject obj = JObject.Parse(jsonParam);
                obj["runUniqueID"] = aCtsTimeout.UniqueId;
                // Do not serialize RuntimeRunParameters directly: its computed Timeout getter can
                // throw while serialization is inspecting public properties.
                obj["runParam"] = JObject.FromObject(new
                {
                    start = runParamModel.StartTimeStamp,
                    total = runParamModel.TotalTime,
                    remain = runParamModel.RemainTime,
                    attempt = runParamModel.CurrentAttempt
                });
                var matchType = RuntimeElementMatchType.None;
                if (runParamModel.EnableAIRepair)
                {
                    matchType |= RuntimeElementMatchType.AiRepair;
                }
                else
                {
                    matchType &= ~RuntimeElementMatchType.AiRepair;
                }
                if (runParamModel.EnableNormal)
                {
                    matchType |= RuntimeElementMatchType.Normal;
                }
                else
                {
                    matchType &= ~RuntimeElementMatchType.Normal;
                }
                //已有 matchType 是上游组件显式指定的配置，保留它；否则传递当前运行策略。
                if (obj["matchType"] == null)
                {
                    obj["matchType"] = JToken.FromObject(matchType);
                }
                return obj.ToString();
            }
            catch (Exception ex)
            {
                Logger.LogError($"AddUniqueIDIntoJsonParam Error", ex);
                return jsonParam;
            }
        }

        private static byte[] ConcatBytes(byte[] array1, byte[] array2)
        {
            byte[] result = new byte[array1.Length + array2.Length];
            Buffer.BlockCopy(array1, 0, result, 0, array1.Length);
            Buffer.BlockCopy(array2, 0, result, array1.Length, array2.Length);
            return result;
        }

        internal static bool ShouldRestartAfterTimeout(
            IRpaRuntimeContext runtimeContext,
            bool needCancel,
            string packagePath)
        {
            ArgumentNullException.ThrowIfNull(runtimeContext);
            return runtimeContext.ForceRestartHost ||
                (needCancel && !runtimeContext.IsMobileProject(packagePath));
        }

        private sealed class RuntimeHostTimeoutMessage
        {
            [JsonProperty("runUniqueID")]
            public string RunUniqueID { get; set; } = string.Empty;

            [JsonProperty("newTimeout")]
            public long NewTimeout { get; set; } = 20000;

            [JsonProperty("timeoutMsg")]
            public string? TimeoutMsg { get; set; }
        }

        [Flags]
        private enum RuntimeElementMatchType
        {
            None = 0,
            Normal = 1 << 1,
            AiRepair = 1 << 2
        }
    }
}
