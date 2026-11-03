using Grpc.Core;
using II.RPA.Core.Log;
using II.RPA.Core.Runtime;
using II.RPA.Core.Contracts.Talker;
using II.RPA.Core.Contracts.Talker.Engine.Message;
using II.RPA.Core.Contracts.Talker.Zdesktop;
using II.RPA.Core.Contracts.Talker.Zdesktop.Message;
using II.RPA.Core.Talker.Processes;
using II.RPA.Communication.Endpoints;
using II.RPA.Communication.Hosting;
using II.RPA.Communication.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rpa.Core.Plugin;
using Rpa.Core.Worker;
using System.Diagnostics;
using Status = II.RPA.Core.Contracts.Talker.Zdesktop.Message.Status;
using ZdesktopContract = II.RPA.Core.Contracts.Talker.Zdesktop;
using System.Collections.Concurrent;

namespace II.RPA.Core.Talker.Talkers
{
    public class ZDeskTopTalker : BaseTalker
    {
        private readonly ZDesktopProcess _zdesktopProcess;
        private readonly ITalkerManager _talkerManager;
        private readonly IRpaRuntimeContext _runtimeContext;
        private readonly ConcurrentQueue<(string Action, string RequestId)> _pendingStartRequests = new();
        private static readonly TimeSpan ChildCoreServerRetryDelay = TimeSpan.FromSeconds(2);
        private const int ChildPythonEnvApplyTimeoutSeconds = 10;
        private int _childCoreServerEstablishStarted;
        private readonly SemaphoreSlim _startResultGate = new(1, 1);
        private ChildCoreServerTalker? _registeredChild;

        public ZDeskTopTalker(ITalkerManager talkerManager, string commandArgs, ILogger logger, IRpaRuntimeContext runtimeContext) : base(logger)
        {
            _talkerManager = talkerManager;
            _runtimeContext = runtimeContext ?? throw new ArgumentNullException(nameof(runtimeContext));
            _zdesktopProcess = new ZDesktopProcess(commandArgs, logger, runtimeContext);
            _zdesktopProcess.OnProcessStatusChangedEvent += ZdesktopProcess_OnProcessStatusChangedEvent;
        }

        

        // This value is part of the Worker registration contract.
        public override string Name => "zdesktop";

        public virtual int? ProcessId => _zdesktopProcess.ProcessId;

        public virtual Models.ProcessStatus ProcessStatus => _zdesktopProcess.Status;

        internal void TrackStartRequest(ZdesktopContract.ActionMessage request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var message = request.Message;
            _pendingStartRequests.Enqueue((request.Action, message.RequestId));
        }

        internal async Task RequestStartResultAsync(ZdesktopContract.ActionMessage request)
        {
            TrackStartRequest(request);
            await FlushStartResultsAsync().ConfigureAwait(false);
        }

        private async Task FlushStartResultsAsync()
        {
            await _startResultGate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_registeredChild?.RegistrationCompleted != true) return;
                while (_pendingStartRequests.TryDequeue(out var pending))
                    await NotifyStartResultAsync(pending.Action, pending.RequestId,
                        _zdesktopProcess.ProcessId ?? 0).ConfigureAwait(false);
            }
            finally
            {
                _startResultGate.Release();
            }
        }

        public override Task Start()
        {
            ClearStaleChildCoreServerPortFile();
            _zdesktopProcess.Start();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 启动新小窗前清掉上一次遗留的子核端口文件。
        /// 端口文件只在子核启动时写入、子核退出时不会清理；不清掉的话，父核会拿旧端口
        /// 连到上一会话残留（正在退出）的子核上，连接瞬间建立又断开，
        /// run 就会被判成"章鱼桌面还未启动"。清理后父核只能等新子核写入端口才认为就绪。
        /// </summary>
        private void ClearStaleChildCoreServerPortFile()
        {
            if (ChildCoreServerPortFile.TryClear(out var error))
            {
                // 用 Info 级别：这是"启动小窗"链路的关键现场信息，默认日志级别下也应当可见
                Logger.LogInfo($"已清理子核端口文件：{ChildCoreServerPortFile.FullPath}");
                return;
            }

            Logger.LogWarning($"清理子核端口文件失败（{ChildCoreServerPortFile.FullPath}）：{error}");
        }

        public override async Task Stop()
        {
            await _zdesktopProcess.StopAsync();
            await base.Stop();
        }

        protected override async Task HandleFrameStream(FrameMessage frameMessage)
        {
            if (frameMessage is FrameMessage<ActionMessage> actionMessage)
            {
                var info = actionMessage.Payload.Message.PayloadJobject;
                Logger.LogDebug($"From ZdesktopTalker:{Environment.NewLine}{info}");
                if (actionMessage.Payload.Action == ZdesktopMessageActions.UpdateStatus)
                {
                    var infoModel = JsonConvert.DeserializeObject<ZdesktopInfoModel>(info.ToString());
                    if (infoModel.Status == Status.Running)
                    {
                        EnsureChildCoreServer();
                        await _talkerManager.GetTalker<HostTalker>().Notify(actionMessage).ConfigureAwait(false);
                        await FlushStartResultsAsync().ConfigureAwait(false);
                    }
                    else if (infoModel.Status == Status.Exited)
                    {
                        await _talkerManager.GetTalker<HostTalker>().Notify(actionMessage).ConfigureAwait(false);
                        Logger.LogWarning("zdesktop exit!");
                    }
                    else
                    {
                        await _talkerManager.GetTalker<HostTalker>().Notify(actionMessage).ConfigureAwait(false);
                        Logger.LogDebug($"zdesktop status changed: {infoModel.Status}");
                    }
                }
                else
                {
                    await _talkerManager.GetTalker<HostTalker>().Notify(actionMessage).ConfigureAwait(false);
                }
            }
        }

        private void EnsureChildCoreServer()
        {
            var childCoreServerTalker = _talkerManager
                .GetTalkers<ChildCoreServerTalker>(readyOnly: false)
                .FirstOrDefault();
            if (childCoreServerTalker == null)
            {
                childCoreServerTalker = new ChildCoreServerTalker(_talkerManager, Logger, _runtimeContext);
                _talkerManager.AddTalker(childCoreServerTalker);
            }

            if (!childCoreServerTalker.IsReady &&
                Interlocked.CompareExchange(ref _childCoreServerEstablishStarted, 1, 0) == 0)
            {
                _ = Task.Run(() => EstablishChildCoreServerWithRetryAsync(childCoreServerTalker));
            }
        }

        private async Task EstablishChildCoreServerWithRetryAsync(ChildCoreServerTalker childCoreServerTalker)
        {
            try
            {
                while (!CancellationToken.IsCancellationRequested &&
                       _talkerManager.GetTalkers<ChildCoreServerTalker>(readyOnly: false).Contains(childCoreServerTalker))
                {
                    if (!childCoreServerTalker.IsReady)
                    {
                        try
                        {
                            // Wait for registration, initial user info and shared state before
                            // acknowledging desktop startup to the host.
                            await childCoreServerTalker.Start().ConfigureAwait(false);
                            _registeredChild = childCoreServerTalker;
                        }
                        catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            if (ChildCoreServerConnectionFailure.IsNotReady(ex))
                            {
                                // 与小窗启动共生的正常状态，避免每次启动都刷错误堆栈
                                Logger.LogDebug($"Child CoreServer 尚未就绪，稍后重试：{ex.Message}");
                            }
                            else
                            {
                                Logger.LogWarning("建立 Child CoreServer 连接失败，稍后重试", ex);
                            }
                        }
                    }

                    // 子核就绪后，补发在它未就绪期间被暂存的 Python 环境切换请求
                    // （虚拟运行会先发 SwitchPythonEnv，而此刻小窗/子核可能还没起来）。
                    if (childCoreServerTalker.IsReady && ZDesktopPythonEnvState.NeedsApply())
                    {
                        await ApplyPendingPythonEnvAsync(childCoreServerTalker).ConfigureAwait(false);
                    }

                    await FlushStartResultsAsync().ConfigureAwait(false);

                    try
                    {
                        await Task.Delay(ChildCoreServerRetryDelay, CancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
            finally
            {
                Interlocked.Exchange(ref _childCoreServerEstablishStarted, 0);
            }
        }

        /// <summary>
        /// 把虚拟运行时记录下来的 Python 环境版本补发给子 CoreServer。
        /// 覆盖两类场景：小窗刚启动（子核稍后才就绪）、子核重启期间下发失败。
        /// 版本与已应用版本一致时不做任何事。
        /// </summary>
        private async Task ApplyPendingPythonEnvAsync(ChildCoreServerTalker childCoreServerTalker)
        {
            var version = ZDesktopPythonEnvState.DesiredVersion;
            if (string.IsNullOrWhiteSpace(version) || childCoreServerTalker.ChildCoreServerPort <= 0)
            {
                return;
            }

            GrpcClient? grpcClient = null;
            try
            {
                grpcClient = new GrpcClient(new ClientOptions
                {
                    Endpoint = new TcpEndpoint("127.0.0.1", childCoreServerTalker.ChildCoreServerPort)
                });
                var pluginServiceClient = grpcClient.CreateClient(invoker => new PluginService.PluginServiceClient(invoker));
                var request = new SwitchPythonEnvRequest
                {
                    ClientId = string.IsNullOrWhiteSpace(_runtimeContext.ProducedClientId) ? "Z-Factory" : _runtimeContext.ProducedClientId,
                    WithinZdesktop = false,
                    Payload = new PayloadSwitchPythonEnv
                    {
                        Version = version,
                        IsUpgrade = false,
                    }
                };
                var headers = new Metadata
                {
                    { "pid", $"{_runtimeContext.CurrentProcessId}" },
                    { "role", "coreserver" },
                    { "name", "coreserver" }
                };

                var response = await pluginServiceClient
                    .SwitchPythonEnvUnaryAsync(request, headers, DateTime.UtcNow.AddSeconds(ChildPythonEnvApplyTimeoutSeconds), CancellationToken)
                    .ConfigureAwait(false);

                if (response.Code == 0)
                {
                    ZDesktopPythonEnvState.MarkApplied(version);
                    Logger.LogInfo($"已补发 SwitchPythonEnv 到 Child CoreServer；version={version}");
                }
                else
                {
                    Logger.LogWarning($"补发 SwitchPythonEnv 到 Child CoreServer 返回失败；version={version}; code={response.Code}; message={response.Message}");
                }
            }
            catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"补发 SwitchPythonEnv 到 Child CoreServer 异常；version={version}", ex);
            }
            finally
            {
                if (grpcClient != null)
                {
                    await grpcClient.DisposeAsync().ConfigureAwait(false);
                }
            }
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

        private Task NotifyStartResultAsync(string action, string requestId, int processId)
        {
            return _talkerManager.GetTalker<HostTalker>().Notify(new FrameMessage<ActionMessage>
            {
                Type = FrameType.EVENT,
                Payload = new ActionMessage
                {
                    Action = action,
                    Payload = ZdesktopContract.ZdesktopMessage.Serialize(BuildZdesktopMessage(
                        action,
                        requestId,
                        new JObject
                        {
                            { "process_name", "II.RPA.ZDeskTop.exe" },
                            { "pid", processId }
                        }))
                }
            });
        }
        private void ZdesktopProcess_OnProcessStatusChangedEvent(object? sender, Models.ProcessStatus e)
        {
            _ = HandleZdesktopProcessStatusChangedAsync(e);
        }

        internal async Task HandleZdesktopProcessStatusChangedAsync(Models.ProcessStatus e)
        {
            try
            {
                if (e != Models.ProcessStatus.Stopped)
                {
                    return;
                }

                // 小窗退出后子 CoreServer 会随之重建，已应用的 Python 环境需要重新下发
                ZDesktopPythonEnvState.ClearApplied();
                _registeredChild = null;
                while (_pendingStartRequests.TryDequeue(out var pending))
                    await NotifyStartResultAsync(pending.Action, pending.RequestId, 0).ConfigureAwait(false);

                try
                {
                    foreach (var childTalker in _talkerManager.GetTalkers<ChildCoreServerTalker>(readyOnly: false))
                    {
                        await childTalker.Stop().ConfigureAwait(false);
                        _talkerManager.RemoveTalker(childTalker);
                    }
                }
                finally
                {
                    _talkerManager.StopChildZBrainProxy();
                }

                var hostTalker = _talkerManager.GetTalker<HostTalker>();

                var info = new ZdesktopInfoModel(new Rect { H = 0, W = 0, X = 0, Y = 0 }, Status.Exited, Mode.Normal, true, 1);
                var zdesktopMessage = new ZdesktopContract.ZdesktopMessage
                {
                    Action = "updatezdesktopstatus",
                    RequestId = Guid.NewGuid().ToString(),
                    PayloadJobject = JObject.FromObject(info),
                };
                ActionMessage actionMessage = new ActionMessage
                {
                    Action = "updatezdesktopstatus",
                    Payload = JsonConvert.SerializeObject(zdesktopMessage)
                };

                await hostTalker.Notify(new FrameMessage<ActionMessage>
                {
                    Payload = actionMessage,
                    Id = Guid.NewGuid(),
                    Type = FrameType.EVENT,
                });

                await hostTalker.Notify(new FrameMessage<ActionMessage>
                {
                    Type = FrameType.EVENT,
                    Payload = new ActionMessage
                    {
                        Action = ZdesktopMessageActions.Start,
                        Payload = ZdesktopContract.ZdesktopMessage.Serialize(BuildZdesktopMessage(ZdesktopMessageActions.Start, Guid.NewGuid().ToString(), new JObject
                        {
                             { "process_name", "II.RPA.ZDeskTop.exe"},
                             { "pid", 0 }
                        }))
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.LogError("处理 ZDesktop 进程退出事件失败", ex);
            }
        }
    }
}
