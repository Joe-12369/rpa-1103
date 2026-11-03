using II.RPA.Core.Log;
using II.RPA.Core.Runtime;
using II.RPA.Core.Contracts.Talker;
using II.RPA.Core.Contracts.Talker.Engine;
using II.RPA.Core.Contracts.Talker.Engine.Message;
using II.RPA.Core.Contracts.Talker.Engine.Message.Action;
using II.RPA.Core.Talker.Processes;
using II.RPA.Core.Talker.Networking;
using II.RPA.Core.Talker.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Sockets;
using System.Text;

namespace II.RPA.Core.Talker.Talkers
{
    public class EventMonitorTalker : BaseTalker
    {
        private readonly EventMonitorProcess _eventMonitorProcess;
        private readonly ITalkerManager _talkerManager;
        private readonly int _port;
        private readonly bool _startProcess;

        private TcpClient _tcpClient;
        private readonly LineDelimitedUtf8Buffer _receiveBuffer = new LineDelimitedUtf8Buffer();
        private readonly object _lifecycleGate = new();
        private CancellationTokenSource? _readCancellation;
        private Task? _readTask;
        private TaskCompletionSource<bool>? _readyCompletion;

        public EventMonitorTalker(ITalkerManager talkerManager, ILogger logger, IRpaRuntimeContext runtimeContext, int port = 6446, bool startProcess = true) : base(logger)
        {
            if (port is <= 0 or > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }
            _talkerManager = talkerManager;
            _port = port;
            _startProcess = startProcess;
            _eventMonitorProcess = new EventMonitorProcess(logger, runtimeContext);
        }

        public override string Name => "EventMonitor";

        public int? ProcessId => _eventMonitorProcess.ProcessId;

        public ProcessStatus ProcessStatus => _eventMonitorProcess.Status;

        public override async Task Start()
        {
            Task readyTask;
            lock (_lifecycleGate)
            {
                if (_readTask is { IsCompleted: false })
                {
                    readyTask = _readyCompletion?.Task ?? Task.CompletedTask;
                }
                else
                {
                    TransitionTo(TalkerLifecycleState.Starting);
                    if (_startProcess)
                    {
                        _eventMonitorProcess.Start();
                    }
                    _readCancellation?.Dispose();
                    _readCancellation = new CancellationTokenSource();
                    _readyCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _readTask = Task.Run(() => RegisterInBackgroundAsync(_readCancellation.Token));
                    readyTask = _readyCompletion.Task;
                }
            }

            await readyTask.ConfigureAwait(false);
        }

        public override async Task Stop()
        {
            Task? readTask;
            try
            {
                lock (_lifecycleGate)
                {
                    _readCancellation?.Cancel();
                    readTask = _readTask;
                }
                _tcpClient?.Close();
                if (_startProcess)
                {
                    await _eventMonitorProcess.StopAsync().ConfigureAwait(false);
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
            }
            finally
            {
                await base.Stop();
            }
        }

        public override async Task Notify(FrameMessage message)
        {
            if (message is FrameMessage<ActionMessage> action)
            {
                var payload = action.Payload.Message;
                if (payload.Action == EngineMessageActions.Trigger)
                {
                    await SendTrigger(payload);
                }
                else if (payload.Action == EngineMessageActions.VerifyTriggerConfig)
                {
                    await SendVerifyTriggerConfig(payload);
                }
            }
        }

        protected override bool ChangeTalkerStatusToReady()
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);
            Exception? lastError = null;

            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    _tcpClient?.Dispose();
                    _tcpClient = new TcpClient();
                    _tcpClient.Connect("127.0.0.1", _port);
                    var keepAlive = Encoding.UTF8.GetBytes("KeepLive 0\r\n");
                    _tcpClient.GetStream().Write(keepAlive, 0, keepAlive.Length);
                    var ready = base.ChangeTalkerStatusToReady();
                    _readyCompletion?.TrySetResult(ready);
                    return ready;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    Thread.Sleep(300);
                }
            }

            var exception = new InvalidOperationException("连接 EventMonitor 触发器端口失败", lastError);
            _readyCompletion?.TrySetException(exception);
            throw exception;
        }

        protected override async Task StartReadStream()
        {
            Exception? disconnectException = null;
            try
            {
                using (var stream = _tcpClient.GetStream())
                {
                    while (!CancellationToken.IsCancellationRequested)
                    {
                        byte[] bf = new byte[2048];
                        var readCount = await stream.ReadAsync(bf, 0, 2048, CancellationToken);

                        if (readCount == 0)
                        {
                            disconnectException = new IOException("EventMonitor TCP 连接已由远端关闭。");
                            break;
                        }
                        await HandleTcpBufferAsync(bf.AsMemory(0, readCount)).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
            {
                Logger.LogDebug("EventMonitor 读取循环已取消");
            }
            catch (Exception ex)
            {
                disconnectException = ex;
                Logger.LogError(ex.Message, ex);
            }
            finally
            {
                _tcpClient?.Dispose();
                if (CancellationToken.IsCancellationRequested)
                {
                    MarkDisconnected();
                }
                else
                {
                    MarkDisconnected(disconnectException ?? new IOException("EventMonitor TCP 读取循环意外结束。"));
                }
            }
        }

        private async Task RegisterInBackgroundAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await RegisterTalker(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    TransitionTo(TalkerLifecycleState.Faulted, ex);
                    _readyCompletion?.TrySetException(ex);
                    Logger.LogError("EventMonitor 注册失败", ex);
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    Logger.LogWarning("EventMonitor 连接已断开，1 秒后重连");
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
        }

        private async Task HandleTcpBufferAsync(ReadOnlyMemory<byte> bytes)
        {
            foreach (var line in _receiveBuffer.Append(bytes.Span))
            {
                if (line == "1" || string.IsNullOrWhiteSpace(line) || line == "{\"status\":0,\"message\":\"Welcome to Trigger Telnet Server\",\"info\":\"\",\"postBack\":null}")
                {
                    continue;
                }

                JObject item;
                try
                {
                    item = JObject.Parse(line);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"无法解析 EventMonitor 消息: {line}", ex);
                    continue;
                }

                var status = item["status"]?.Value<int?>();
                var postBack = item["postBack"];
                var requestId = postBack?["requestId"]?.Value<string>();
                var appId = postBack?["appId"]?.Value<string>();

                if (new string[] { "bot", "Bot" }.Contains(postBack?["source"]?.Value<string>()))
                {
                    if (status != null && new int[] { 1001, 1002, 1003 }.Contains(status.Value))
                    {
                        await SpeakAsEngineAsync(EngineMessageActions.Trigger, new Trigger()
                        {
                            status = status,
                            appId = appId,
                        }, requestId);
                    }
                    else if (status == 1004)
                    {
                        await SpeakAsEngineAsync(EngineMessageActions.VerifyTriggerConfig, new Trigger()
                        {
                            status = status,
                            msg = "触发器校验成功",
                        }, requestId);
                    }
                    else
                    {
                        if (item["status"]?.Value<string>()?.StartsWith("4004") ?? false)
                        {
                            await _talkerManager.GetTalker<HostTalker>().Notify(new FrameMessage<ActionMessage>
                            {
                                Type = FrameType.EVENT,
                                Payload = new ActionMessage
                                {
                                    Action = EngineMessageActions.VerifyTriggerConfig,
                                    Payload = EngineMessage.Serialize(new EngineMessage<Trigger>(EngineMessageActions.VerifyTriggerConfig,
                                        new Trigger
                                        {
                                            status = 0,
                                            msg = item["message"]?.Value<string>() ?? string.Empty,
                                        })
                                    {
                                        ResponseFor = requestId,
                                    })
                                }
                            });
                        }
                        else
                        {
                            await _talkerManager.GetTalker<HostTalker>().Notify(new FrameMessage<ActionMessage>
                            {
                                Type = FrameType.EVENT,
                                Payload = new ActionMessage
                                {
                                    Action = EngineMessageActions.Trigger,
                                    Payload = EngineMessage.Serialize(new EngineMessage<Trigger>(EngineMessageActions.Trigger,
                                        new Trigger
                                        {
                                            status = 0,
                                            appId = appId,
                                            msg = item["message"]?.Value<string>() ?? string.Empty,
                                        })
                                    {
                                        ResponseFor = requestId,
                                    })
                                }
                            });
                        }
                    }
                }
            }
        }

        private async Task SpeakAsEngineAsync<T>(string action, T payload, string requestId)
        {
            await _talkerManager.GetTalker<HostTalker>().Notify(new FrameMessage<ActionMessage>
            {
                Type = FrameType.EVENT,
                Payload = new ActionMessage
                {
                    Action = action,
                    Payload = EngineMessage.Serialize(new EngineMessage<T>(action, payload)
                    {
                        ResponseFor = requestId,
                    })
                }
            });
        }

        private async Task SendTrigger(EngineMessage message)
        {
            var trigger = message.Payload<Trigger>();

            if (trigger.status == 1)
            {
                var data = trigger.appIdParams.FirstOrDefault();
                if (data == null)
                {
                    throw new InvalidOperationException("触发器启动参数为空。");
                }

                if (data["postBack"] is not JObject postBack)
                {
                    postBack = new JObject();
                    data["postBack"] = postBack;
                }

                postBack["requestId"] = message.RequestId;
                data["isRepeat"] = 1;
                await WriteString($"StartTrigger {JsonConvert.SerializeObject(data)}\r\n");
                await SpeakAsEngineAsync(EngineMessageActions.Trigger, new Trigger()
                {
                    status = 1000,
                    msg = "配置开始触发器成功",
                }, message.RequestId);
            }
            else if (trigger.status == 0)
            {
                await WriteString($"CloseTrigger {JsonConvert.SerializeObject(trigger.appId)}\r\n");
                await SpeakAsEngineAsync(EngineMessageActions.Trigger, new Trigger()
                {
                    status = 1000,
                    msg = "配置关闭触发器成功",
                }, message.RequestId);
            }
        }

        private async Task SendVerifyTriggerConfig(EngineMessage message)
        {
            var trigger = message.Payload<VerifyTriggerConfig>();

            var data = trigger.appIdParams.FirstOrDefault();
            if (data == null)
            {
                throw new InvalidOperationException("触发器校验参数为空。");
            }

            data["postBack"] = JObject.FromObject(new
            {
                requestId = message.RequestId,
                source = "bot"
            });
            await WriteString($"VerifyTriggerConfig {JsonConvert.SerializeObject(data)}\r\n");
        }

        private async Task WriteString(string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            await _tcpClient.GetStream().WriteAsync(bytes, 0, bytes.Length);
        }
    }
}
