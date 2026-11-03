using Google.Protobuf.WellKnownTypes;
using II.RPA.Core.Log;
using II.RPA.Core.Runtime;
using II.RPA.Core.Contracts.Talker;
using II.RPA.Core.Contracts.Talker.ZBrain;
using II.RPA.Core.Contracts.Talker.Engine;
using II.RPA.Core.Contracts.Talker.Engine.Message;
using II.RPA.Core.Contracts.Talker.Engine.Message.Action;
using II.RPA.Core.Contracts.Talker.RuntimeHost;
using II.RPA.Core.Contracts.Talker.Settings;
using II.RPA.Core.Contracts.Talker.Zdesktop.Message;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Diagnostics;
using System.IO;
using Grpc.Core;
using II.RPA.Core.Talker.RuntimeHost;
using II.RPA.Core.Talker.Models;
using ZdesktopContract = II.RPA.Core.Contracts.Talker.Zdesktop;

namespace II.RPA.Core.Talker.Talkers
{
    public class HostTalker : BaseTalker
    {
        private static readonly TimeSpan ExecutorWaitTimeout = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan ChildCoreServerWaitTimeout = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan ChildCoreServerRunWaitTimeout = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan ChildCoreServerForwardRetryInterval = TimeSpan.FromSeconds(1);
        private readonly ITalkerManager _talkerManager;
        private readonly IRpaRuntimeContext _runtimeContext;
        private readonly Func<string, ZDeskTopTalker> _zdesktopTalkerFactory;
        private readonly SemaphoreSlim _zdesktopStartSemaphore = new SemaphoreSlim(1, 1);
        private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _operatorWindowRequests =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, (bool IsReady, DateTimeOffset StateChangedAt, int Pid)> _executorStatusCache =
            new(StringComparer.OrdinalIgnoreCase);
        private bool _lastHostReady;

        public HostTalker(ITalkerManager talkerManager, ILogger logger, IRpaRuntimeContext runtimeContext)
            : this(talkerManager, logger, runtimeContext, null)
        {
        }

        internal HostTalker(
            ITalkerManager talkerManager,
            ILogger logger,
            IRpaRuntimeContext runtimeContext,
            Func<string, ZDeskTopTalker>? zdesktopTalkerFactory) : base(logger)
        {
            _talkerManager = talkerManager;
            _runtimeContext = runtimeContext ?? throw new ArgumentNullException(nameof(runtimeContext));
            _zdesktopTalkerFactory = zdesktopTalkerFactory ??
                (commandArgs => new ZDeskTopTalker(_talkerManager, commandArgs, Logger, _runtimeContext));

            _ = WatchExecutorTalkerStatusAsync();
        }

        public override string Name => "host";

        /// <summary>
        /// 监听 ExecutorTalker 可用状态变化，并将状态推送给宿主（Host），
        /// 与 develop-7.3.7 的 WatchTalkerStatus&lt;ExecutorTalker&gt; 行为对齐。
        /// </summary>
        private async Task WatchExecutorTalkerStatusAsync()
        {
            try
            {
                while (LifecycleState is not (TalkerLifecycleState.Stopping or TalkerLifecycleState.Stopped or TalkerLifecycleState.Faulted))
                {
                    var hostReady = IsReady;
                    var hostJustConnected = hostReady && !_lastHostReady;
                    _lastHostReady = hostReady;

                    var talkers = _talkerManager.GetTalkers<ExecutorTalker>(readyOnly: false);
                    var currentIds = new HashSet<string>(talkers.Select(t => t.Id), StringComparer.OrdinalIgnoreCase);
                    foreach (var staleId in _executorStatusCache.Keys.Where(id => !currentIds.Contains(id)).ToArray())
                    {
                        _executorStatusCache.TryRemove(staleId, out _);
                    }

                    foreach (var talker in talkers)
                    {
                        var state = (talker.IsReady, talker.StateChangedAt, talker.Pid);
                        var isNewOrChanged = !_executorStatusCache.TryGetValue(talker.Id, out var last) || last != state;
                        if (isNewOrChanged)
                        {
                            _executorStatusCache[talker.Id] = state;
                        }

                        if (hostReady && (isNewOrChanged || hostJustConnected))
                        {
                            await SendTalkerStatusToHost(
                                new ExecutorStatusChanged(
                                    talker.Pid > 0 ? talker.Pid : null,
                                    talker.IsReady)).ConfigureAwait(false);
                        }
                    }

                    await Task.Delay(500).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Watch ExecutorTalker status failed.", ex);
            }
        }

        private Task SendTalkerStatusToHost(ExecutorStatusChanged status)
        {
            var actionMessage = new ActionMessage
            {
                Action = EngineMessageActions.ExecutorTalkerStatusChanged,
                Payload = EngineMessage.Serialize(new ExecutorStatusChangedMessage(status))
            };

            return Notify(new FrameMessage<ActionMessage>
            {
                Payload = actionMessage,
                Id = Guid.NewGuid(),
                Type = FrameType.EVENT,
            });
        }

        protected virtual TimeSpan ZBrainRequestTimeout => Timeout.InfiniteTimeSpan;

        internal static TimeSpan ResolveZBrainRequestTimeout(
            ZBrainRequest? request,
            TimeSpan defaultTimeout)
        {
            if (string.IsNullOrWhiteSpace(request?.Body))
            {
                return defaultTimeout;
            }

            try
            {
                var timeoutValue = JObject.Parse(request.Body)["timeout"]?.ToString();
                if (!long.TryParse(
                    timeoutValue,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var timeoutMilliseconds) ||
                    timeoutMilliseconds <= 0)
                {
                    return defaultTimeout;
                }

                return TimeSpan.FromMilliseconds(timeoutMilliseconds);
            }
            catch (Exception ex) when (ex is JsonException or ArgumentOutOfRangeException)
            {
                return defaultTimeout;
            }
        }

        public override Task Notify(FrameMessage message)
        {
            if (!IsReady &&
                _runtimeContext.ClientKind == RpaClientKind.Desktop &&
                _talkerManager.TryGetTalker<HostCoreServerClientTalker>(out var rootConnection))
            {
                return rootConnection.Notify(message);
            }

            return base.Notify(message);
        }

        public Task RunAsync(EngineMessage request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ActionRun(request);
        }

        public Task StopRunAsync(string actionId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(actionId);
            cancellationToken.ThrowIfCancellationRequested();
            return ActionStop(new StopMessage(new Stop(id: actionId)));
        }

        public async Task<bool> RequestOperatorWindowAsync(
            int windowType,
            int windowState,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            if (timeout <= TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            var request = new EngineMessage<object>(EngineMessageActions.OperatorClientWin, new
            {
                winType = windowType,
                winState = windowState
            });
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!_operatorWindowRequests.TryAdd(request.RequestId, completion))
            {
                throw new InvalidOperationException(
                    $"Operator-window request [{request.RequestId}] is already pending.");
            }

            try
            {
                await Notify(new FrameMessage<ActionMessage>
                {
                    Type = FrameType.EVENT,
                    Payload = new ActionMessage
                    {
                        Action = EngineMessageActions.OperatorClientWin,
                        Payload = EngineMessage.Serialize(request)
                    }
                }).ConfigureAwait(false);

                using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    CancellationToken);
                try
                {
                    return timeout == Timeout.InfiniteTimeSpan
                        ? await completion.Task.WaitAsync(linkedCancellation.Token).ConfigureAwait(false)
                        : await completion.Task.WaitAsync(timeout, linkedCancellation.Token).ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    return false;
                }
            }
            finally
            {
                _operatorWindowRequests.TryRemove(request.RequestId, out _);
            }
        }

        public override async Task Stop()
        {
            foreach (var pair in _operatorWindowRequests)
            {
                if (_operatorWindowRequests.TryRemove(pair.Key, out var pending))
                {
                    pending.TrySetCanceled();
                }
            }

            await base.Stop().ConfigureAwait(false);
        }

        internal Task HandleIncomingFrameAsync(FrameMessage frameMessage)
        {
            return HandleFrameStream(frameMessage);
        }

        private Task NotifyErrorReplyAsync(Guid requestId, string reason)
            => Notify(new FrameMessage<Reply<string>>
            {
                ReplyId = requestId,
                Type = FrameType.REPLY,
                Code = 504,
                Message = reason,
                Payload = new Reply<string> { Body = string.Empty }
            });

        protected override async Task HandleFrameStream(FrameMessage frameMessage)
        {
            if (frameMessage is FrameMessage<SharpRuntimeMessageProxy<Any>> sharpRuntimeMessage)
            {
                try
                {
                    Logger.LogDebug($"new uia message from upstream: {sharpRuntimeMessage.Id}");
                    var runtimeHostTalker = _talkerManager.GetTalker<RuntimeHostTalker>();
                    await runtimeHostTalker.Notify(sharpRuntimeMessage).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }
                return;
            }

            if (frameMessage.Type == FrameType.EVENT)
            {
                if (frameMessage is FrameMessage<SettingsMessage> settingsMessage)
                {
                    var settings = settingsMessage.Payload?.Settings;
                    if (settings != null)
                    {
                        Logger.LogDebug($"SettingsMessage count: {settings.Count}");
                        foreach (var setting in settings)
                        {
                            _runtimeContext.SetSetting(setting.Key, setting.Value);
                        }

                        await Notify(new FrameMessage<SettingsMessage>
                        {
                            Type = FrameType.EVENT,
                            Payload = new SettingsMessage
                            {
                                Settings = settings
                            }
                        });
                    }
                    else
                    {
                        Logger.LogWarning("SettingsMessage payload is null");
                    }
                }
                else if (frameMessage is FrameMessage<ActionMessage> actionMessage)
                {
                    try
                    {
                        await HandleEngineMessage(actionMessage);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning("HandleEngineMessageError", ex);
                    }
                }
                else if (frameMessage is FrameMessage<ZdesktopContract.ActionMessage> zDesktopActionMessage)
                {
                    await HandleZdesktopMessage(zDesktopActionMessage);
                }
                else if (frameMessage is FrameMessage<UserInfoMessage> userInfoMessage)
                {
                    var userInfo = userInfoMessage.Payload;
                    var userInfoPayload = JsonConvert.DeserializeObject<string>(userInfo.Payload);
                    if (string.IsNullOrEmpty(userInfoPayload))
                    {
                        Logger.LogWarning("Empty userInfoPayload");
                    }
                    else
                    {
                        _runtimeContext.UserInfoParameter = _runtimeContext.FixZBrainUriForZDesktop(userInfoPayload);
                        Logger.LogInfo($"UserInfoParam:{Environment.NewLine}{_runtimeContext.UserInfoParameter}");
                    }
                }
            }
            else if (frameMessage.Type == FrameType.REQUEST)
            {
                if (frameMessage is FrameMessage<ZBrainRequest> zbrainRequest)
                {
                    Logger.LogDebug($"Handle zbrain proxy message:{zbrainRequest?.Payload?.Path}");
                    var wantChildSession = _runtimeContext.IsMiniModeActive;
                    Logger.LogDebug($"等待 zbrain 初始化 minimode:{wantChildSession}");
                    var requestTimeout = ResolveZBrainRequestTimeout(
                        zbrainRequest.Payload,
                        ZBrainRequestTimeout);
                    using var requestCancellation =
                        CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
                    if (requestTimeout != Timeout.InfiniteTimeSpan)
                    {
                        requestCancellation.CancelAfter(requestTimeout);
                    }
                    try
                    {
                        var zbrainTalker = await _talkerManager.WaitTalker<ZBrainTalker>(
                            talker => talker.IsChildSession == wantChildSession,
                            requestCancellation.Token).WaitAsync(requestCancellation.Token).ConfigureAwait(false);
                        var reply = await zbrainTalker.ProxyTalk(new FrameRequest
                        {
                            Id = zbrainRequest.Id,
                            ReplyId = zbrainRequest.ReplyId,
                            Type = zbrainRequest.Type,
                            Code = zbrainRequest.Code,
                            Message = zbrainRequest.Message,
                            Buffer = zbrainRequest.Buffer,
                            Payload = zbrainRequest.Payload
                        }, requestCancellation.Token).WaitAsync(requestCancellation.Token).ConfigureAwait(false);
                        if (reply == null)
                        {
                            Logger.LogWarning($"zbrain 应答为空，RequestId={zbrainRequest.Id}");
                            await NotifyErrorReplyAsync(zbrainRequest.Id, "zbrain 应答为空").ConfigureAwait(false);
                            return;
                        }

                        try
                        {
                            Logger.LogDebug($"zbrain response: {JsonConvert.SerializeObject(reply)}");
                        }
                        catch { }
                        await Notify(reply).ConfigureAwait(false);
                    }
                    catch (TimeoutException ex)
                    {
                        Logger.LogWarning($"zbrain 请求超时({requestTimeout})，RequestId={zbrainRequest.Id}", ex);
                        await NotifyErrorReplyAsync(zbrainRequest.Id, "zbrain 请求超时").ConfigureAwait(false);
                    }
                    catch (OperationCanceledException ex)
                    {
                        if (CancellationToken.IsCancellationRequested)
                        {
                            // 宿主连接已关闭，请求随连接丢弃
                            return;
                        }

                        if (requestCancellation.IsCancellationRequested)
                        {
                            Logger.LogWarning($"zbrain 请求超时({requestTimeout})，RequestId={zbrainRequest.Id}", ex);
                            await NotifyErrorReplyAsync(zbrainRequest.Id, "zbrain 请求超时").ConfigureAwait(false);
                            return;
                        }

                        Logger.LogWarning($"zbrain 请求被取消，RequestId={zbrainRequest.Id}", ex);
                        await NotifyErrorReplyAsync(zbrainRequest.Id, "zbrain 请求被取消").ConfigureAwait(false);
                    }
                }
            }
            else if (frameMessage is FrameMessage<RuntimeMessage<RunPluginFromClientRequest>> runPluginFromClientRequestMessage)
            {
                if (runPluginFromClientRequestMessage.Payload is IRuntimeMessage)
                {
                    try
                    {
                        Logger.LogInfo($"new runtime message from upstream: {runPluginFromClientRequestMessage.Id}");
                        var requestPayload = runPluginFromClientRequestMessage.Payload.Payload.PayloadRunFromClient;
                        if (!string.Equals(
                                requestPayload.PluginAction,
                                RuntimeHostActions.TransferPluginAction,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            Logger.LogError($"Unsupported action: {requestPayload.PluginAction}");
                            return;
                        }

                        if (TransferPluginActionCodec.TryResolveRequest(
                                requestPayload.PluginParam,
                                Logger,
                                out var runAction))
                        {
                            var runTimeHostTalker = _talkerManager.GetTalker<RuntimeHostTalker>();
                            var resultObj = await runTimeHostTalker.RunUnaryAsync(
                                aPackagePath: requestPayload.PackagePath,
                                aPluginName: runAction.PluginName,
                                anAction: runAction.PluginAction,
                                aJsonParam: runAction.JsonParameters,
                                RuntimeRunParameters.DefaultValue,
                                anIsNeedAutoInit: false,
                                pluginRunOptions: new Core.Models.PluginRunOptions { IsAsync = false, NeedUiThread = false })
                            .ConfigureAwait(false);

                            var retResolved = TransferPluginActionCodec.ResolveResult(
                                resultObj.ToString(),
                                Logger);
                            var response = new ClientResponse
                            {
                                Code = int.Parse(retResolved.Code),
                                Message = retResolved.Message,
                                Result = retResolved.Result,
                                PluginAction = requestPayload.PluginAction
                            };
                            var fm = new FrameMessage<RuntimeMessage<ClientResponse>>()
                            {
                                Payload = new RuntimeMessage<ClientResponse>(response),
                            };
                            await Notify(fm);
                        }
                        else
                        {
                            Logger.LogError($"Failed to TryResolveAction action: {requestPayload.PluginAction}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning("", ex);
                        var response = new ClientResponse
                        {
                            Code = 1,
                            Message = ex.Message,
                            Result = ex.Message,
                            PluginAction = "unknown"
                        };
                        var fm = new FrameMessage<RuntimeMessage<ClientResponse>>()
                        {
                            Payload = new RuntimeMessage<ClientResponse>(response),
                        };
                        await Notify(fm);
                    }
                }
            }
        }

        /// <summary>
        /// 把引擎消息转发到小窗（章鱼桌面）内的子 CoreServer。
        ///
        /// 子核由子会话登录后拉起，比"小窗进程 Running"晚数秒才可用。历史表现是：
        /// 第一次点运行时 run 比子核启动早约 5~8 秒到达，父核连不上子核端口，
        /// 直接回了 "章鱼桌面还未启动!"，用户必须再点一次才能跑。
        ///
        /// 现在对 run 做有界等待 + 重试（间隔 1s，最多 5 分钟）：子核一就绪立刻转发；
        /// 仍然失败才回错误，并且回的是真实原因（服务不可达 / 端口文件缺失 / 连接已中断等）。
        /// 其余控制类消息保持快速失败，避免 stop / ping 之类被长时间阻塞。
        /// </summary>
        private async Task ForwardToChildCoreServerAsync(ActionMessage raw, FrameMessage<ActionMessage> message)
        {
            bool waitForChildCoreServer = string.Equals(raw.Action, EngineMessageActions.Run, StringComparison.OrdinalIgnoreCase);
            TimeSpan timeout = waitForChildCoreServer ? ChildCoreServerRunWaitTimeout : ChildCoreServerWaitTimeout;
            var stopwatch = Stopwatch.StartNew();
            Exception lastError = null;
            int attempt = 0;

            while (!CancellationToken.IsCancellationRequested)
            {
                TimeSpan remaining = timeout - stopwatch.Elapsed;
                if (remaining <= TimeSpan.Zero)
                {
                    break;
                }

                attempt++;
                try
                {
                    var childCoreServerTalker = await _talkerManager
                        .WaitTalker<ChildCoreServerTalker>(remaining, CancellationToken)
                        .ConfigureAwait(false);
                    message.Payload.WithinZdesktop = false;
                    Logger.LogDebug($"HostCoreServerClientTalker:{raw.Action}");
                    await childCoreServerTalker.Notify(message).ConfigureAwait(false);

                    if (attempt > 1)
                    {
                        Logger.LogInfo($"转发到子核成功（第 {attempt} 次尝试，累计 {stopwatch.ElapsedMilliseconds} ms）：{raw.Action}");
                    }

                    return;
                }
                catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    if (!waitForChildCoreServer)
                    {
                        break;
                    }

                    Logger.LogWarning(
                        $"转发到子核失败（第 {attempt} 次尝试，累计 {stopwatch.ElapsedMilliseconds} ms），子核就绪前持续等待：{raw.Action}",
                        ex);

                    try
                    {
                        await Task.Delay(ChildCoreServerForwardRetryInterval, CancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }

            if (CancellationToken.IsCancellationRequested)
            {
                return;
            }

            string reason = DescribeChildCoreServerFailure(lastError);
            Logger.LogError(
                $"转发到子核失败，放弃等待（尝试 {attempt} 次，累计 {stopwatch.ElapsedMilliseconds} ms）：{raw.Action}；原因：{reason}",
                lastError);
            await ReplyChildCoreServerFailureAsync(raw, reason, stopwatch.ElapsedMilliseconds).ConfigureAwait(false);
        }

        /// <summary>把子核不可用的真实原因整理成可读文案（日志与回给引擎共用）。</summary>
        private static string DescribeChildCoreServerFailure(Exception exception)
        {
            switch (exception)
            {
                case null:
                    return "等待子核就绪超时，子核未启动或未写入端口文件";
                case TimeoutException timeout:
                    return $"等待子核就绪超时（{timeout.Message}）";
                case FileNotFoundException fileNotFound:
                    return $"子核端口文件不存在：{fileNotFound.FileName}";
                case InvalidDataException invalidData:
                    return $"子核端口文件内容无效：{invalidData.Message}";
                case RpcException rpc when rpc.StatusCode == StatusCode.Unavailable:
                    return "子核 gRPC 服务不可达（子核可能仍在启动或已退出）";
                case RpcException rpc:
                    return $"子核 gRPC 调用失败（{rpc.StatusCode}）";
                case IOException io:
                    return $"子核连接已中断：{io.Message}";
                default:
                    return $"{exception.GetType().Name}：{exception.Message}";
            }
        }

        /// <summary>
        /// 子核不可用时回给引擎的错误应答：保留 WithinZdesktop 标记，文案带真实原因与等待时长。
        /// </summary>
        private Task ReplyChildCoreServerFailureAsync(ActionMessage raw, string reason, long elapsedMilliseconds)
        {
            string errorText = $"章鱼桌面（子核）未就绪，已等待 {elapsedMilliseconds / 1000.0:0.#} 秒：{reason}";
            return Notify(new FrameMessage<ActionMessage>
            {
                Type = FrameType.EVENT,
                Payload = new ActionMessage
                {
                    Action = raw.Action,
                    WithinZdesktop = true,
                    Payload = EngineMessage.Serialize(new EngineMessage<EngineResponse>(raw.Action, EngineResponse.Error(errorText))
                    {
                        ResponseFor = raw.Message.RequestId,
                    })
                }
            });
        }

        private async Task HandleEngineMessage(FrameMessage<ActionMessage> message)
        {
            Logger.LogInfo($"HandleEngineMessage:{message?.Payload?.Action};WithinZdesktop:{message.Payload.WithinZdesktop}");
            try
            {
                var raw = message.Payload;
                try
                {
                    var businessMessage = raw.Message;
                    Logger.LogDebug(
                        $"[TRACE-RUN] stage=coreserver.host.handle frameId={message.Id} " +
                        $"withinZdesktop={raw.WithinZdesktop} requestId={businessMessage.RequestId} " +
                        $"action={businessMessage.Action} " +
                        $"runId={businessMessage.PayloadJobject["id"]} " +
                        $"status={businessMessage.PayloadJobject["status"]}");
                }
                catch (Exception ex)
                {
                    Logger.LogDebug(
                        $"[TRACE-RUN] stage=coreserver.host.parse-failed frameId={message.Id}",
                        ex);
                }

                if (raw.WithinZdesktop)
                {
                    await ForwardToChildCoreServerAsync(raw, message).ConfigureAwait(false);
                }
                else
                {
                    switch (raw.Action)
                    {
                        case EngineMessageActions.Ping:
                            await SpeakAsEngine(new EngineMessage<object>(raw.Action, new object())
                            {
                                ResponseFor = raw.Message.RequestId,
                            });
                            break;
                        case EngineMessageActions.SetLogLevel:
                            StoreInShare(raw);
                            await SpeakAsEngine(new EngineMessage<EngineResponse>(raw.Action, EngineResponse.Success("Engine设置日志级别成功"))
                            {
                                ResponseFor = raw.Message.RequestId,
                            });
                            break;
                        case EngineMessageActions.GetUserInfo:
                            StoreInShare(raw);
                            await SpeakAsEngine(new ResponseMessage(raw.Action, new Response
                            {
                                Status = Response.SuccessStatus,
                                Message = string.Empty,
                            })
                            {
                                ResponseFor = raw.Message.RequestId,
                            });
                            break;
                        case EngineMessageActions.SetServiceInfo:
                            StoreInShare(raw);
                            await SpeakAsEngine(new EngineMessage<EngineResponse>(raw.Action, EngineResponse.Success(string.Empty))
                            {
                                ResponseFor = raw.Message.RequestId,
                            });
                            break;
                        case EngineMessageActions.Run:
                            try
                            {
                                await ActionRun(raw.Message).ConfigureAwait(false);
                            }
                            catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
                            {
                                throw;
                            }
                            catch (Exception ex)
                            {
                                Logger.LogWarning("Unable to dispatch run to an Executor", ex);
                                await SpeakAsEngine(CreateRunDispatchFailure(raw.Message, ex.Message))
                                    .ConfigureAwait(false);
                            }
                            break;
                        case EngineMessageActions.Stop:
                            await ActionStop(raw.Message);
                            break;
                        case EngineMessageActions.Close:
                            await SpeakAsEngine(new ResponseMessage(raw.Action, new Response
                            {
                                Status = "recv",
                            })
                            {
                                ResponseFor = raw.Message.RequestId,
                            });
                            break;
                        case EngineMessageActions.Trigger:
                        case EngineMessageActions.VerifyTriggerConfig:
                            EventMonitorTalker? eventMonitorTalker = null;
                            try
                            {
                                eventMonitorTalker = _talkerManager.GetTalker<EventMonitorTalker>();
                            }
                            catch (Exception ex)
                            {
                                try
                                {
                                    eventMonitorTalker = new EventMonitorTalker(_talkerManager, Logger, _runtimeContext);
                                    await eventMonitorTalker.Start();
                                    _talkerManager.AddTalker(eventMonitorTalker);
                                }
                                catch
                                {
                                    await Notify(new FrameMessage<ActionMessage>
                                    {
                                        Type = FrameType.EVENT,
                                        Payload = new ActionMessage
                                        {
                                            Action = EngineMessageActions.Trigger,
                                            Payload = EngineMessage.Serialize(new EngineMessage<Trigger>(EngineMessageActions.Trigger, new Trigger()
                                            {
                                                status = -1,
                                            }) { ResponseFor = "", })
                                        }
                                    });
                                    break;
                                }
                            }
                            await eventMonitorTalker?.Notify(message);
                            break;
                        case EngineMessageActions.Pause:
                            _talkerManager.GetTalker<ExecutorTalker>().Suspend();
                            await SpeakAsEngine(new ResponseMessage(raw.Action, new Response
                            {
                                Message = $"{raw.Action} ok",
                                Status = Response.SuccessStatus,
                            })
                            {
                                ResponseFor = raw.Message.RequestId,
                            }).ConfigureAwait(false);
                            break;
                        case EngineMessageActions.Resume:
                            _talkerManager.GetTalker<ExecutorTalker>().Resume();
                            await SpeakAsEngine(new ResponseMessage(raw.Action, new Response
                            {
                                Message = $"{raw.Action} ok",
                                Status = Response.SuccessStatus,
                            })
                            {
                                ResponseFor = raw.Message.RequestId,
                            }).ConfigureAwait(false);
                            break;
                        case EngineMessageActions.Reset:
                            await ActionReset(raw.Message).ConfigureAwait(false);
                            break;
                        case EngineMessageActions.MCPRun:
                            await ActionMCPRun(raw.Message).ConfigureAwait(false);
                            break;
                        case EngineMessageActions.OperatorClientWin:
                            var operatorRequestId = raw.Message.RequestId;
                            if (_operatorWindowRequests.TryRemove(operatorRequestId, out var operatorCompletion))
                            {
                                operatorCompletion.TrySetResult(true);
                            }
                            else
                            {
                                Logger.LogDebug(
                                    $"Ignoring unmatched operator-window response [{operatorRequestId}].");
                            }
                            break;
                        default:
                            if (_runtimeContext.ClientKind != RpaClientKind.Factory)
                            {
                                Logger.LogWarning($"当前客户端未配置 InterpreterTalker，无法处理动作: {raw.Action}");
                                break;
                            }

                            await _talkerManager.GetTalker<InterpreterTalker>().Notify(new FrameMessage<ActionMessage>
                            {
                                Type = FrameType.EVENT,
                                Payload = new ActionMessage
                                {
                                    Action = raw.Message.Action,
                                    Payload = raw.Message.Raw,
                                }
                            });
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogWarning("", e);
                throw;
            }
        }

        private async Task HandleZdesktopMessage(FrameMessage<ZdesktopContract.ActionMessage> frameMessage)
        {
            var payload = frameMessage.Payload;
            var action = payload.Action.ToLower();
            switch (action)
            {

                case ZdesktopMessageActions.CheckAllow:
                    var ret = _talkerManager.CheckZDesktopAllow();
                    Logger.LogDebug($"checkallowzdesktop: {ret.Item1},{ret.Item2}");
                    var msg = new ZdesktopContract.ZdesktopMessage
                    {
                        Action = payload.Action,
                        ResponseFor = payload.Message.RequestId,
                        PayloadJobject = new JObject { { "allow", ret.Item1 } }
                    };
                    Logger.LogDebug($"checkallowzdesktop:{msg.RequestId}");
                    await Notify(new FrameMessage<ZdesktopContract.ActionMessage>
                    {
                        Type = FrameType.EVENT,
                        Payload = new ZdesktopContract.ActionMessage
                        {
                            Action = payload.Action,
                            Payload = ZdesktopContract.ZdesktopMessage.Serialize(msg)
                        }
                    });
                    break;
                case ZdesktopMessageActions.Start:
                    var zdesktopStartLockTaken = false;
                    try
                    {
                        await _zdesktopStartSemaphore.WaitAsync(CancellationToken).ConfigureAwait(false);
                        zdesktopStartLockTaken = true;

                        var zdeskTopTalker = _talkerManager
                            .GetTalkers<ZDeskTopTalker>(readyOnly: false)
                            .FirstOrDefault();
                        if (zdeskTopTalker != null)
                        {
                            Logger.LogInfo("There is an existed zdesktoptalker!");
                            if (!zdeskTopTalker.IsReady &&
                                zdeskTopTalker.ProcessStatus == Models.ProcessStatus.Stopped)
                            {
                                await zdeskTopTalker.Start().ConfigureAwait(false);
                            }

                            await zdeskTopTalker.RequestStartResultAsync(payload).ConfigureAwait(false);
                            return;
                        }

                        Logger.LogInfo("Start new zdesktop");
                        var commandLine = BuildZdesktopCommandLine(payload);
                        var zdesktopTalker = _zdesktopTalkerFactory(commandLine);
                        zdesktopTalker.TrackStartRequest(payload);
                        _talkerManager.AddTalker(zdesktopTalker);
                        try
                        {
                            await zdesktopTalker.Start().ConfigureAwait(false);
                        }
                        catch
                        {
                            _talkerManager.RemoveTalker(zdesktopTalker);
                            await zdesktopTalker.Stop().ConfigureAwait(false);
                            await NotifyZdesktopStartResult(payload, 0).ConfigureAwait(false);
                            throw;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Start zdesktop failed!", ex);
                    }
                    finally
                    {
                        if (zdesktopStartLockTaken)
                        {
                            _zdesktopStartSemaphore.Release();
                        }
                    }
                    break;
                case ZdesktopMessageActions.GetZDesktopStatus:
                    try
                    {
                        var zdeskTopTalker = _talkerManager.GetTalker<ZDeskTopTalker>();
                        await zdeskTopTalker.Notify(new FrameMessage<ZdesktopContract.ActionMessage>
                        {
                            Type = FrameType.EVENT,
                            Payload = new ZdesktopContract.ActionMessage
                            {
                                Action = payload.Action,
                                Payload = ZdesktopContract.ZdesktopMessage.Serialize(BuildZdesktopMessage(payload.Action, payload.Message.RequestId, new JObject()))
                            }
                        }).ConfigureAwait(false);
                    }
                    catch
                    {
                        Logger.LogWarning($"getzdesktopstatus:zdesktop 未启动");
                        await Notify(new FrameMessage<ZdesktopContract.ActionMessage>
                        {
                            Type = FrameType.EVENT,
                            Payload = new ZdesktopContract.ActionMessage
                            {
                                Action = payload.Action,
                                RequestId = payload.RequestId,
                                Payload = ZdesktopContract.ZdesktopMessage.Serialize(BuildZdesktopMessage(payload.Action, payload.Message.RequestId, JObject.FromObject(ZdesktopInfoModel.CreateExitedModel())))
                            }
                        }).ConfigureAwait(false);
                    }
                    break;
                case ZdesktopMessageActions.AgentStatusUpdated:
                    try
                    {
                        var zdeskTopTalker = _talkerManager.GetTalker<ZDeskTopTalker>();
                        await zdeskTopTalker.Notify(new FrameMessage<ZdesktopContract.ActionMessage>
                        {
                            Type = FrameType.EVENT,
                            Payload = new ZdesktopContract.ActionMessage
                            {
                                RequestId = payload.RequestId,
                                Action = payload.Action,
                                Payload = payload.Payload,
                            }
                        }).ConfigureAwait(false);
                    }
                    catch
                    { }

                    await Notify(new FrameMessage<ZdesktopContract.ActionMessage>
                    {
                        Type = FrameType.EVENT,
                        Payload = new ZdesktopContract.ActionMessage
                        {
                            Action = payload.Action,
                            RequestId = payload.RequestId,
                            Payload = payload.Payload,
                        }
                    }).ConfigureAwait(false);
                    break;
                default:

                    try
                    {
                        var zdeskTopTalker = _talkerManager.GetTalker<ZDeskTopTalker>();
                        Logger.LogDebug($"Transfer to zdesktop:{action}");
                        await zdeskTopTalker.Notify(new FrameMessage<ZdesktopContract.ActionMessage>
                        {
                            Type = FrameType.EVENT,
                            Payload = new ZdesktopContract.ActionMessage
                            {
                                RequestId = payload.RequestId,
                                Action = payload.Action,
                                Payload = ZdesktopContract.ZdesktopMessage.Serialize(BuildZdesktopMessage(payload.Action, payload.Message.RequestId, payload.Message.PayloadJobject))
                            }
                        }).ConfigureAwait(false);
                    }
                    catch
                    { }
                    break;
            }
        }

        private async Task SpeakAsEngine(EngineMessage message)
        {
            await Notify(new FrameMessage<ActionMessage>
            {
                Type = FrameType.EVENT,
                Payload = new ActionMessage
                {
                    Action = message.Action,
                    Payload = EngineMessage.Serialize(message),
                }
            });
        }

        private void StoreInShare(ActionMessage raw)
        {
            var msg = raw.Message;
            _talkerManager.SetShare(msg.Action, msg.PayloadJobject);
        }

        private string BuildZdesktopCommandLine(ZdesktopContract.ActionMessage payload)
        {
            var commandLine = new StringBuilder($"--c {_runtimeContext.ProducedClientId} --p {_runtimeContext.ServerPort}");
            if (string.IsNullOrEmpty(payload.Payload))
            {
                return commandLine.ToString();
            }

            var startMessage = JsonConvert.DeserializeObject<StartZdesktopAction>(payload.Payload);
            if (startMessage?.Content.Mode == 2)
            {
                commandLine.Append(" --m agent");
            }
            if (startMessage?.Content.WindowMode.HasValue == true)
            {
                commandLine.Append($" --wm {startMessage.Content.WindowMode}");
            }
            return commandLine.ToString();
        }

        private Task NotifyZdesktopStartResult(
            ZdesktopContract.ActionMessage request,
            int processId)
        {
            return Notify(new FrameMessage<ZdesktopContract.ActionMessage>
            {
                Type = FrameType.EVENT,
                Payload = new ZdesktopContract.ActionMessage
                {
                    Action = request.Action,
                    Payload = ZdesktopContract.ZdesktopMessage.Serialize(BuildZdesktopMessage(
                        request.Action,
                        request.Message.RequestId,
                        new JObject
                        {
                            { "process_name", "II.RPA.ZDeskTop.exe" },
                            { "pid", processId }
                        }))
                }
            });
        }

        internal static ResponseMessage CreateRunDispatchFailure(EngineMessage request, string reason)
            => new ResponseMessage(EngineMessageActions.Run, new Response
            {
                Id = request.PayloadJobject["id"]?.Value<string>() ?? request.RequestId,
                Status = Response.ErrorStatus,
                Message = reason,
                Text = reason,
                Vlog = string.Empty,
                ConsoleLogShowLevel = Response.ConsoleLogSystem,
            })
            {
                ResponseFor = request.RequestId,
            };

        private async Task ActionRun(EngineMessage request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var packageRoot = request.PayloadJobject["root"]?.Value<string>();
            ExecutorTalker? recycledExecutor = null;
            var startedAt = System.Diagnostics.Stopwatch.GetTimestamp();

            while (System.Diagnostics.Stopwatch.GetElapsedTime(startedAt) < ExecutorWaitTimeout)
            {
                CancellationToken.ThrowIfCancellationRequested();

                if (recycledExecutor != null)
                {
                    if (recycledExecutor.IsReady &&
                        await recycledExecutor.TryHandleActionAsync(request).ConfigureAwait(false))
                    {
                        return;
                    }
                }
                else
                {
                    var idleExecutors = _talkerManager
                        .GetTalkers<ExecutorTalker>(readyOnly: true)
                        .Where(executor => !executor.IsRunning)
                        .OrderBy(executor => executor.StateChangedAt)
                        .ToArray();
                    var selected = idleExecutors.FirstOrDefault(executor =>
                            PackageRootsMatch(executor.LastPackageRoot, packageRoot))
                        ?? idleExecutors.FirstOrDefault(executor =>
                            string.IsNullOrWhiteSpace(executor.LastPackageRoot));

                    if (selected != null &&
                        await selected.TryHandleActionAsync(request).ConfigureAwait(false))
                    {
                        return;
                    }

                    // A worker that has loaded another package must be recycled before reuse.
                    // This preserves package isolation without terminating an unrelated active run.
                    var mismatched = idleExecutors.FirstOrDefault();
                    if (mismatched != null)
                    {
                        recycledExecutor = mismatched;
                        await recycledExecutor.RestartAsync(CancellationToken).ConfigureAwait(false);
                        continue;
                    }
                }

                await Task.Delay(50, CancellationToken).ConfigureAwait(false);
            }

            throw new TimeoutException(
                $"No Executor became available within {ExecutorWaitTimeout.TotalSeconds:0} seconds.");
        }

        private async Task ActionStop(EngineMessage request)
        {
            var action = request.Payload<Stop>();
            var executorTalker = _talkerManager
                .GetTalkers<ExecutorTalker>(readyOnly: false)
                .FirstOrDefault(executor => executor.MatchesRun(action.id));
            if (executorTalker != null)
            {
                try
                {
                    // Intentional process shutdowns are not automatically restarted by BaseProcess.
                    // Recycle explicitly so the configured Bot/Factory capacity remains available.
                    await executorTalker.RestartAsync(CancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to recycle Executor[{executorTalker.Id}] after stop", ex);
                }
            }

            await SpeakAsEngine(new ResponseMessage(request.Action, new Response
            {
                Id = action.id,
                Status = "recv",
                ConsoleLogShowLevel = "system",
            })
            {
                ResponseFor = request.RequestId,
            }).ConfigureAwait(false);
            if (_runtimeContext.RootClientId == "Z-Bot")
            {
                await SpeakAsEngine(new ResponseMessage(EngineMessageActions.Log, new Response
                {
                    Id = action.id,
                    Text = $"project [{action.id}] End screen recording",
                    ConsoleLogShowLevel = "system",
                })).ConfigureAwait(false);
            }
            if (_runtimeContext.ClientKind == RpaClientKind.Desktop)
            {
                try
                {
                    await _talkerManager.GetTalker<RuntimeHostTalker>()
                        .DisposeUnaryAsync(action.root)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("ZDeskTop.PluginStopUnary", ex);
                }
            }
            await SpeakAsEngine(new ResponseMessage(EngineMessageActions.Run, new Response
            {
                Id = action.id,
                Status = Response.StopStatus,
                Vlog = string.Empty,
                ConsoleLogShowLevel = "system",
            })
            {
                ResponseFor = request.RequestId,
            }).ConfigureAwait(false);
        }

        private async Task ActionReset(EngineMessage request)
        {
            var executors = _talkerManager
                .GetTalkers<ExecutorTalker>(readyOnly: false)
                .Where(executor => executor.IsRunning || !string.IsNullOrWhiteSpace(executor.LastPackageRoot))
                .ToArray();
            await Task.WhenAll(executors.Select(SafeRestartExecutorAsync)).ConfigureAwait(false);
            try
            {
                var obj = new JObject
                {
                        { "UseSZBrowser", false }
                };
                await _talkerManager.GetTalker<RuntimeHostTalker>()
                    .SetParam(obj.ToString(), cancellationToken: CancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Failed to reset RuntimeHost parameters", ex);
            }

            await SpeakAsEngine(new EngineMessage<EngineResponse>(request.Action, EngineResponse.Success("success"))
            {
                ResponseFor = request.RequestId,
            }).ConfigureAwait(false);
        }

        private async Task SafeRestartExecutorAsync(ExecutorTalker executor)
        {
            try
            {
                await executor.RestartAsync(CancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to reset Executor[{executor.Id}]", ex);
            }
        }

        private static bool PackageRootsMatch(string? left, string? right)
        {
            return string.Equals(
                NormalizePackageRoot(left),
                NormalizePackageRoot(right),
                StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePackageRoot(string? packageRoot)
        {
            if (string.IsNullOrWhiteSpace(packageRoot))
            {
                return string.Empty;
            }

            return packageRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private ZdesktopContract.ZdesktopMessage BuildZdesktopMessage(string action, string requestId, JObject payload)
        {
            return new ZdesktopContract.ZdesktopMessage
            {
                Action = action,
                ResponseFor = requestId,
                PayloadJobject = payload
            };
        }

        private async Task ActionMCPRun(EngineMessage request)
        {
            _talkerManager.TryGetShare(EngineMessageActions.SetServiceInfo, out var serviceInfo);
            var deopHost = serviceInfo?["deopHost"] as JObject;
            if (deopHost == null)
            {
                await SpeakAsEngine(new EngineMessage<object>(request.Action, new
                {
                    status = "prepareError",
                    msg = "程序信息未初始化",
                    text = "1"
                })
                {
                    ResponseFor = request.RequestId,
                });
                return;
            }
            var configuration = new Dictionary<string, string>
            {
                {"II_USER_TOKEN", deopHost.Value<string>("token") ?? string.Empty },
                {"II_AGENT_HOST", (deopHost.Value<string>("url") ?? string.Empty) + "/agent" },
                {"II_AGENT_INPUT", JsonConvert.SerializeObject(request.PayloadJobject)},
            };
            using var runner = new MCPAgentRunner(Logger, _runtimeContext);
            await runner.RunAsync(
                configuration,
                progress => SpeakAsEngine(new EngineMessage<object>(request.Action, progress)
                {
                    ResponseFor = request.RequestId,
                }),
                CancellationToken).ConfigureAwait(false);
        }
    }
}
