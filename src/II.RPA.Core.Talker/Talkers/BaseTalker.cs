using Grpc.Core;
using II.RPA.Core.Log;
using II.RPA.Core.Contracts.Talker;
using II.RPA.Core.Talker.Kernel;
using II.RPA.Core.Talker.Models;
using II.RPA.Core.WorkerProtocol.Mapping;
using Rpa.Core.Worker;
using System.Collections.Concurrent;

namespace II.RPA.Core.Talker.Talkers
{
    public abstract class BaseTalker : ITalker
    {

        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<FrameMessage>> _taskCompletionSourceDict;
        private readonly SemaphoreSlim _writeGate = new(1, 1);
        private readonly KernelRequestDispatcher? _kernelRequestDispatcher;

        private IServerStreamWriter<FrameStream>? _responseStream;
        private IAsyncStreamReader<FrameStream>? _requestStream;
        private long _connectionGeneration;

        protected BaseTalker(ILogger logger, ITalkerManager? talkerManager = null)
        {
            Logger = logger;
            _taskCompletionSourceDict = new ConcurrentDictionary<Guid, TaskCompletionSource<FrameMessage>>();
            if (talkerManager != null)
            {
                _kernelRequestDispatcher = new KernelRequestDispatcher(
                    talkerManager,
                    logger,
                    WriteRawFrameAsync,
                    () => CancellationToken);
            }
        }

        protected CancellationToken CancellationToken { get; private set; } = CancellationToken.None;

        public ILogger Logger { get; }

        public string Id { get; } = Guid.NewGuid().ToString();

        public int Pid { get; private set; } = -1;

        public bool IsReady { get; private set; }

        public TalkerLifecycleState LifecycleState { get; private set; } = TalkerLifecycleState.Created;

        public DateTimeOffset StateChangedAt { get; private set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? ConnectedAt { get; private set; }

        public Exception? LastFault { get; private set; }

        internal int PendingRequestCount => _taskCompletionSourceDict.Count;

        internal long ConnectionGeneration => Volatile.Read(ref _connectionGeneration);

        public abstract string Name { get; }

        protected virtual Task HandleFrameStream(FrameMessage frameMessage) => Task.CompletedTask;

        protected virtual bool ChangeTalkerStatusToReady() => true;

        protected virtual void OnConnectionClosed(Exception? exception) { }

        public virtual Task Start()
        {
            TransitionTo(TalkerLifecycleState.Starting);
            return Task.CompletedTask;
        }

        public virtual Task Stop()
        {
            Interlocked.Increment(ref _connectionGeneration);
            _kernelRequestDispatcher?.ClearSubscriptions();
            TransitionTo(TalkerLifecycleState.Stopping);
            IsReady = false;
            ConnectedAt = null;
            FailPendingRequests(pending => pending.TrySetCanceled());
            TransitionTo(TalkerLifecycleState.Stopped);
            return Task.CompletedTask;
        }

        public Task RegisterTalker(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
            Interlocked.Increment(ref _connectionGeneration);
            EstablishConnection();
            return StartReadStream();
        }

        public async Task RegisterTalker(
            IAsyncStreamReader<FrameStream>? requestStream,
            IServerStreamWriter<FrameStream>? responseStream,
            ServerCallContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var headers = context.RequestHeaders;
            if (Guid.TryParse(headers.FirstOrDefault(kv => kv.Key == "talker-id")?.Value, out var requestedId) &&
                !string.Equals(Id, requestedId.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"无法找到 talker-id=[{requestedId}] 对应的 Talker"));
            }

            if (int.TryParse(headers.FirstOrDefault(kv => kv.Key == "pid")?.Value, out var pid))
            {
                PrepareRegistration(pid);
            }

            _requestStream = requestStream;
            _responseStream = responseStream;
            CancellationToken = context.CancellationToken;
            Interlocked.Increment(ref _connectionGeneration);

            EstablishConnection();
            await StartReadStream().ConfigureAwait(false);
        }

        public virtual Task<FrameMessage> SendAsync(FrameMessage message)
            => SendAsync(message, CancellationToken.None);

        public virtual async Task<FrameMessage> SendAsync(
            FrameMessage message,
            CancellationToken cancellationToken,
            TimeSpan? timeout = null)
        {
            ArgumentNullException.ThrowIfNull(message);

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

                if (timeout is { } requestTimeout && requestTimeout != Timeout.InfiniteTimeSpan)
                {
                    return await taskCompletionSource.Task
                        .WaitAsync(requestTimeout, linkedCancellation.Token)
                        .ConfigureAwait(false);
                }

                return await taskCompletionSource.Task
                    .WaitAsync(linkedCancellation.Token)
                    .ConfigureAwait(false);
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

        public virtual async Task Notify(FrameMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);

            await WriteRawFrameAsync(message.ToWorkerStream()).ConfigureAwait(false);
        }

        private async Task WriteRawFrameAsync(FrameStream message)
        {
            ArgumentNullException.ThrowIfNull(message);

            var traceRunFrame = RunTrace.IsEngineAction(message);
            await _writeGate.WaitAsync(CancellationToken).ConfigureAwait(false);
            try
            {
                var responseStream = _responseStream;
                if (responseStream == null)
                {
                    throw new InvalidOperationException($"Talker[{Name}][{Id}] 尚未建立响应流");
                }

                // Grpc.Core's server writer does not support the cancellation-token overload.
                // The connection token still guards the queue wait and this pre-write check;
                // an interrupted connection causes the ordinary write to fail naturally.
                CancellationToken.ThrowIfCancellationRequested();
                if (traceRunFrame)
                {
                    Logger.LogDebug(
                        $"[TRACE-RUN] stage=host.write.begin talker={Name} {RunTrace.FormatFrame(message)}");
                }

                await responseStream.WriteAsync(message).ConfigureAwait(false);
                if (traceRunFrame)
                {
                    Logger.LogDebug(
                        $"[TRACE-RUN] stage=host.write.ok talker={Name} frameId={message.Id}");
                }
            }
            catch (Exception ex)
            {
                if (traceRunFrame)
                {
                    Logger.LogDebug(
                        $"[TRACE-RUN] stage=host.write.failed talker={Name} {RunTrace.FormatFrame(message)}",
                        ex);
                }

                throw;
            }
            finally
            {
                _writeGate.Release();
            }
        }

        protected virtual async Task StartReadStream()
        {
            var requestStream = _requestStream;
            var cancellationToken = CancellationToken;
            var generation = Volatile.Read(ref _connectionGeneration);
            Exception? disconnectException = null;

            try
            {
                if (requestStream == null)
                {
                    return;
                }

                while (await requestStream.MoveNext(cancellationToken).ConfigureAwait(false))
                {
                    try
                    {
                        var rawFrame = requestStream.Current;
                        var traceRunFrame = RunTrace.IsEngineAction(rawFrame);
                        if (traceRunFrame)
                        {
                            Logger.LogDebug(
                                $"[TRACE-RUN] stage=coreserver.raw talker={Name} {RunTrace.FormatFrame(rawFrame)}");
                        }

                        if (_kernelRequestDispatcher?.TryHandle(rawFrame) == true)
                        {
                            if (traceRunFrame)
                            {
                                Logger.LogDebug(
                                    $"[TRACE-RUN] stage=coreserver.kernel-handled talker={Name} frameId={rawFrame.Id}");
                            }

                            continue;
                        }

                        var frameMessage = rawFrame.ToFrameMessage();
                        if (traceRunFrame)
                        {
                            Logger.LogDebug(
                                $"[TRACE-RUN] stage=coreserver.decoded talker={Name} frameId={frameMessage.Id} " +
                                $"decodedType={frameMessage.Type} " +
                                $"payloadClrType={frameMessage.AnonymousPayload?.GetType().Name ?? "<null>"}");
                        }

                        if (frameMessage.Type == FrameType.REPLY)
                        {
                            if (_taskCompletionSourceDict.TryRemove(frameMessage.ReplyId, out var taskCompletionSource))
                            {
                                taskCompletionSource.TrySetResult(frameMessage);
                            }
                        }
                        else
                        {
                            await HandleFrameStream(frameMessage).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning("处理 Worker 消息失败", ex);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                disconnectException = ex;
                if (generation == Volatile.Read(ref _connectionGeneration))
                {
                    TransitionTo(TalkerLifecycleState.Faulted, ex);
                }
                Logger.LogError("预期外的Worker连接异常", ex);
            }
            finally
            {
                // A reconnect can replace the stream while the old read loop is winding down.
                // Only the current connection is allowed to change readiness or fail requests.
                if (generation == Volatile.Read(ref _connectionGeneration))
                {
                    IsReady = false;
                    ConnectedAt = null;
                    _kernelRequestDispatcher?.ClearSubscriptions();
                    var disconnected = disconnectException ?? new IOException($"Talker[{Name}][{Id}] 连接已终止");
                    FailPendingRequests(pending => pending.TrySetException(disconnected));
                    if (LifecycleState != TalkerLifecycleState.Stopping && LifecycleState != TalkerLifecycleState.Stopped)
                    {
                        TransitionTo(disconnectException == null ? TalkerLifecycleState.Stopped : TalkerLifecycleState.Faulted,
                            disconnectException);
                    }
                    OnConnectionClosed(disconnectException);
                }

                Logger.LogInfo($"连接终止【{Id}】【{Pid}】，generation={generation}");
            }
        }

        internal void PrepareRegistration(int pid)
        {
            Pid = pid;
        }

        protected void TransitionTo(TalkerLifecycleState state, Exception? exception = null)
        {
            LifecycleState = state;
            StateChangedAt = DateTimeOffset.UtcNow;
            if (exception != null)
            {
                LastFault = exception;
            }
            else if (state is TalkerLifecycleState.Starting or TalkerLifecycleState.Connecting or TalkerLifecycleState.Ready)
            {
                LastFault = null;
            }
        }

        protected void MarkDisconnected(Exception? exception = null)
        {
            IsReady = false;
            ConnectedAt = null;
            TransitionTo(exception == null ? TalkerLifecycleState.Stopped : TalkerLifecycleState.Faulted, exception);
        }

        protected bool EstablishConnection()
        {
            TransitionTo(TalkerLifecycleState.Connecting);
            IsReady = ChangeTalkerStatusToReady();
            ConnectedAt = IsReady ? DateTimeOffset.UtcNow : null;
            TransitionTo(IsReady ? TalkerLifecycleState.Ready : TalkerLifecycleState.Faulted,
                IsReady ? null : new InvalidOperationException($"Talker[{Name}] 未能建立连接。"));
            return IsReady;
        }

        private void FailPendingRequests(Action<TaskCompletionSource<FrameMessage>> complete)
        {
            foreach (var pair in _taskCompletionSourceDict)
            {
                if (_taskCompletionSourceDict.TryRemove(pair.Key, out var pending))
                {
                    complete(pending);
                }
            }
        }
    }
}
