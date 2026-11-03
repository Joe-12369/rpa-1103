using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using II.RPA.Communication.Endpoints;
using II.RPA.Communication.Hosting;
using II.RPA.Communication.Options;
using II.RPA.Core.Log;
using II.RPA.Core.Runtime;
using II.RPA.Core.Contracts.Talker;
using II.RPA.Core.Contracts.Talker.Engine.Message;
using II.RPA.Core.Talker.Processes;
using II.RPA.Core.WorkerProtocol.Mapping;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rpa.Core.Worker;
using Rpa.Core.Worker.Zdesktop;
using System.Collections.Concurrent;
using System.Data.Common;
using static Rpa.Core.Worker.WorkerService;
using EngineActionMessage = II.RPA.Core.Contracts.Talker.Engine.ActionMessage;
using ZdesktopContract = II.RPA.Core.Contracts.Talker.Zdesktop;

namespace II.RPA.Core.Talker.Talkers
{
    public class ChildCoreServerTalker : BaseTalker
    {
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<FrameMessage>> _taskCompletionSourceDict;
        private readonly ITalkerManager _talkerManager;
        private readonly IRpaRuntimeContext _runtimeContext;
        private readonly string _fileName;

        private IClientStreamWriter<FrameStream>? _requestStream;
        private IAsyncStreamReader<FrameStream>? _responseStream;
        private FrameStream? _initialUserInfoFrame;
        private readonly object _lifecycleGate = new();
        private readonly SemaphoreSlim _writeGate = new(1, 1);
        private CancellationTokenSource? _readCancellation;
        private Task? _readTask;
        private TaskCompletionSource<bool>? _readyCompletion;
        private GrpcClient? _grpcClient;

        public ChildCoreServerTalker(ITalkerManager talkerManager, ILogger logger, IRpaRuntimeContext runtimeContext)
            : this(talkerManager, logger, runtimeContext, null)
        {
        }

        internal ChildCoreServerTalker(
            ITalkerManager talkerManager,
            ILogger logger,
            IRpaRuntimeContext runtimeContext,
            string? portFileName) : base(logger)
        {
            _talkerManager = talkerManager;
            _runtimeContext = runtimeContext ?? throw new ArgumentNullException(nameof(runtimeContext));
            _taskCompletionSourceDict = new ConcurrentDictionary<Guid, TaskCompletionSource<FrameMessage>>();
            _fileName = portFileName ?? ChildCoreServerPortFile.FullPath;
        }

        public override string Name => "ChildCoreServerTalker";

        public WorkerServiceClient WorkerServiceClient { get; private set; }

        /// <summary>
        /// 子 CoreServer 当前监听的端口（来自 Z-Core\Z-DeskTop.txt）。
        /// 仅在该 Talker Ready 期间有效，供需要直连子核的组件（如补发 SwitchPythonEnv）使用。
        /// </summary>
        public int ChildCoreServerPort { get; private set; }
        internal bool RegistrationCompleted => IsReady && _readyCompletion?.Task.IsCompletedSuccessfully == true;

        public override async Task Start()
        {
            while (true)
            {
                Task readyTask;
                Task? disconnectingTask = null;
                lock (_lifecycleGate)
                {
                    if (RegistrationCompleted)
                    {
                        return;
                    }

                    if (_readTask is { IsCompleted: false })
                    {
                        if (_readyCompletion is { Task.IsCompleted: false })
                        {
                            readyTask = _readyCompletion.Task;
                        }
                        else
                        {
                            // The old connection has already lost readiness but its read loop is
                            // still unwinding. Wait for that generation before creating a new one.
                            disconnectingTask = _readTask;
                            readyTask = Task.CompletedTask;
                        }
                    }
                    else
                    {
                        _readCancellation?.Dispose();
                        _readCancellation = new CancellationTokenSource();
                        _readyCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                        _readTask = Task.Run(() => RunConnectionAsync(_readCancellation.Token));
                        readyTask = _readyCompletion.Task;
                    }
                }

                if (disconnectingTask != null)
                {
                    await disconnectingTask.ConfigureAwait(false);
                    continue;
                }

                await readyTask.ConfigureAwait(false);
                return;
            }
        }

        protected override bool ChangeTalkerStatusToReady()
        {          
            if (File.Exists(_fileName))
            {
                string idString = File.ReadAllText(_fileName);
                if (!int.TryParse(idString, out var port) || port is <= 0 or > 65535)
                {
                    var exception = new InvalidDataException($"Child CoreServer 端口文件内容无效: {idString}");
                    _readyCompletion?.TrySetException(exception);
                    throw exception;
                }

                ChildCoreServerPort = port;

                var userinfo = string.IsNullOrEmpty(_runtimeContext.UserInfoParameter)
                    ? _runtimeContext.BuildUserInfoParameter()
                    : _runtimeContext.UserInfoParameter;
                var headers = new Metadata
                {
                    { "pid", $"{_runtimeContext.CurrentProcessId}" },
                    { "role", "host" },
                    { "name", "hostcoreserver" },
                };
                var callOptions = new CallOptions(headers, cancellationToken: CancellationToken);

                var childCoreServerGrpcClient = new GrpcClient(new ClientOptions()
                {
                    Endpoint = new TcpEndpoint("127.0.0.1", port)
                });
                _grpcClient = childCoreServerGrpcClient;

                WorkerServiceClient = childCoreServerGrpcClient.CreateClient(invoker => new WorkerServiceClient(invoker));
                var caller = WorkerServiceClient.Talk(callOptions);
                // Creating an AsyncDuplexStreamingCall does not establish the transport. Waiting
                // for response headers proves that the child Core accepted and registered the
                // stream before this Talker becomes visible as Ready.
                caller.ResponseHeadersAsync.WaitAsync(CancellationToken).GetAwaiter().GetResult();

                _requestStream = caller.RequestStream;
                _responseStream = caller.ResponseStream;
                var userInfoMessage = new UserInfoMessage
                {
                    Action = "set",
                    Payload = JsonConvert.ToString(userinfo)
                };
                _initialUserInfoFrame = new FrameStream
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = FrameStream.Types.Type.Event,
                    Payload = Any.Pack(userInfoMessage)
                };

                var ready = base.ChangeTalkerStatusToReady();
                return ready;
            }
            else
            {
                var exception = new FileNotFoundException("未找到 Child CoreServer 端口文件", _fileName);
                _readyCompletion?.TrySetException(exception);
                throw exception;
            }
        }

        public override async Task Notify(FrameMessage message)
        {
            await _writeGate.WaitAsync(CancellationToken).ConfigureAwait(false);
            try
            {
                if (_requestStream == null)
                {
                    throw new InvalidOperationException($"Talker[{Name}][{Id}] 尚未建立请求流");
                }

                // Grpc.Core does not support the cancellation-token overload of IAsyncStreamWriter.WriteAsync.
                // The connection token still guards the queue wait and this pre-write check; an interrupted
                // connection makes the ordinary write fail naturally.
                CancellationToken.ThrowIfCancellationRequested();
                await _requestStream.WriteAsync(message.ToWorkerStream()).ConfigureAwait(false);
            }
            finally
            {
                _writeGate.Release();
            }
        }

        public override Task<FrameMessage> SendAsync(FrameMessage message)
            => SendAsync(message, CancellationToken.None);

        public override async Task<FrameMessage> SendAsync(
            FrameMessage message,
            CancellationToken cancellationToken,
            TimeSpan? timeout = null)
        {
            var taskCompletionSource = new TaskCompletionSource<FrameMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!_taskCompletionSourceDict.TryAdd(message.Id, taskCompletionSource))
            {
                throw new InvalidOperationException($"Talker[{Name}][{Id}] 已存在请求 Id={message.Id}");
            }

            try
            {
                await Notify(message).ConfigureAwait(false);
                using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    CancellationToken);
                return timeout is { } requestTimeout && requestTimeout != Timeout.InfiniteTimeSpan
                    ? await taskCompletionSource.Task.WaitAsync(requestTimeout, linkedCancellation.Token).ConfigureAwait(false)
                    : await taskCompletionSource.Task.WaitAsync(linkedCancellation.Token).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                Logger.LogWarning($"Talker[{Id}] was disposed during Talk operation");
                throw;
            }
            finally
            {
                _taskCompletionSourceDict.TryRemove(message.Id, out _);
            }
        }

        protected override async Task StartReadStream()
        {
            if (_responseStream == null || _requestStream == null)
            {
                return;
            }

            try
            {
                if (_initialUserInfoFrame != null)
                {
                    await _requestStream.WriteAsync(_initialUserInfoFrame);
                    await SyncShareToChildServer();
                    Logger.LogDebug("set userinfo");
                    _readyCompletion?.TrySetResult(true);
                }

                while (await _responseStream.MoveNext(CancellationToken))
                {
                    var frameMessage = _responseStream.Current.ToFrameMessage();
                    if (frameMessage.Type == FrameType.REPLY && _taskCompletionSourceDict.TryGetValue(frameMessage.ReplyId, out var pending))
                    {
                        try
                        {
                            pending.TrySetResult(frameMessage);
                        }
                        finally
                        {
                            _taskCompletionSourceDict.TryRemove(frameMessage.ReplyId, out _);
                        }
                    }
                    else
                    {
                        await HandleFrameStream(frameMessage);
                    }
                }
            }
            catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
            {
                Logger.LogDebug("Child CoreServer 读取循环已取消");
            }
            catch (Exception ex)
            {
                Logger.LogError("Child CoreServer连接异常", ex);
            }
            finally
            {
                var disconnected = new IOException($"Talker[{Name}][{Id}] 连接已终止");
                _readyCompletion?.TrySetException(disconnected);
                foreach (var pending in _taskCompletionSourceDict.Values)
                {
                    pending.TrySetException(disconnected);
                }
                _taskCompletionSourceDict.Clear();
                if (CancellationToken.IsCancellationRequested)
                {
                    MarkDisconnected();
                }
                else
                {
                    MarkDisconnected(disconnected);
                }
            }
        }

        protected override async Task HandleFrameStream(FrameMessage frameMessage)
        {
            if (frameMessage is FrameMessage<EngineActionMessage> actionMessage &&
                string.Equals(
                    actionMessage.Payload?.Action,
                    EngineMessageActions.ChildRunOver,
                    StringComparison.OrdinalIgnoreCase))
            {
                var zdesktopTalker = _talkerManager
                    .GetTalkers<ZDeskTopTalker>(readyOnly: false)
                    .FirstOrDefault();
                if (zdesktopTalker != null)
                {
                    await zdesktopTalker.Notify(frameMessage).ConfigureAwait(false);
                }
                return;
            }

            await _talkerManager.GetTalker<HostTalker>()
                .Notify(frameMessage)
                .ConfigureAwait(false);
        }

        public override async Task Stop()
        {
            Task? readTask;
            lock (_lifecycleGate)
            {
                _readCancellation?.Cancel();
                readTask = _readTask;
            }

            try
            {
                if (_requestStream != null)
                {
                    await _requestStream.CompleteAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug("关闭 Child CoreServer 请求流失败", ex);
            }
            finally
            {
                if (_grpcClient != null)
                {
                    await _grpcClient.DisposeAsync().ConfigureAwait(false);
                }
                _grpcClient = null;
                _requestStream = null;
                _responseStream = null;
            }

            if (readTask != null)
            {
                try
                {
                    await readTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }

            await base.Stop().ConfigureAwait(false);
        }

        private async Task RunConnectionAsync(CancellationToken cancellationToken)
        {
            try
            {
                await RegisterTalker(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _readyCompletion?.TrySetCanceled(cancellationToken);
                MarkDisconnected();
            }
            catch (Exception ex)
            {
                _readyCompletion?.TrySetException(ex);
                MarkDisconnected(ex);
                if (ChildCoreServerConnectionFailure.IsNotReady(ex))
                {
                    // 子核还没起来（端口文件未写入/内容非法/gRPC 不可达）属于启动期的正常状态，
                    // 由 ZDeskTopTalker 的 2s 重试负责，不打错误堆栈
                    Logger.LogDebug($"Child CoreServer 尚未就绪：{ex.Message}");
                }
                else
                {
                    Logger.LogError("建立 Child CoreServer 连接失败", ex);
                }
            }
        }

        private async Task SyncShareToChildServer()
        {
            if (_talkerManager.TryGetShare(EngineMessageActions.SetServiceInfo, out JObject setServiceInfo))
            {
                EngineMessage engineMessage = new EngineMessage(EngineMessageActions.SetServiceInfo)
                {
                    PayloadJobject = setServiceInfo
                };
                var setserviceMsg = new EngineActionMessage()
                {
                    Action = EngineMessageActions.SetServiceInfo,
                    Payload = EngineMessage.Serialize(engineMessage),
                };

                Logger.LogDebug($"SyncShareToChildServer:{setserviceMsg.Payload}");
                await Notify(new FrameMessage<EngineActionMessage>
                {
                    Payload = setserviceMsg,
                    Type = FrameType.EVENT,
                }).ConfigureAwait(false);
            }

            if (_talkerManager.TryGetShare(EngineMessageActions.GetUserInfo, out JObject getUserInfo))
            {
                EngineMessage engineMessageUserInfo = new EngineMessage(EngineMessageActions.GetUserInfo)
                {
                    PayloadJobject = getUserInfo
                };
                var userinfoMsg = new EngineActionMessage()
                {
                    Action = EngineMessageActions.GetUserInfo,
                    Payload = EngineMessage.Serialize(engineMessageUserInfo),
                };

                Logger.LogDebug($"SyncShareToChildServer:{userinfoMsg.Payload}");
                await Notify(new FrameMessage<EngineActionMessage>
                {
                    Payload = userinfoMsg,
                    Type = FrameType.EVENT,
                }).ConfigureAwait(false);
            }
        }
    }
}
