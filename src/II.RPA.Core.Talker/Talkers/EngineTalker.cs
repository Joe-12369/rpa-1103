using II.RPA.Core.Log;
using II.RPA.Core.Contracts.Talker;
using II.RPA.Core.Contracts.Talker.Engine;
using II.RPA.Core.Contracts.Talker.Engine.Message;
using II.RPA.Core.Talker.Models;
using System.Linq;
using Newtonsoft.Json;

namespace II.RPA.Core.Talker.Talkers
{
    public abstract class EngineTalker : BaseTalker
    {
        private readonly ITalkerManager _talkerManager;

        public EngineTalker(ITalkerManager talkerManager, ILogger logger) : base(logger, talkerManager)
        {
            _talkerManager = talkerManager;
        }

        protected string RunningRequestId { get; private set; } = string.Empty;


        protected HostTalker GetHostTalker()
        {
            if (_talkerManager.TryGetTalker<HostTalker>(out var hostTalker))
            {
                return hostTalker;
            }

            // 子会话的 HostTalker 永远不会进入就绪状态（没有 host 角色客户端接入），
            // 但它的 Notify 会把消息经 hostcoreserver 桥转发回根 CoreServer。
            return _talkerManager.GetTalkers<HostTalker>(readyOnly: false).FirstOrDefault()
                ?? throw new InvalidOperationException("HostTalker 未注册");
        }

        protected virtual Task HandleOthersFrameStream(FrameMessage frameMessage) => Task.CompletedTask;

        protected override async Task HandleFrameStream(FrameMessage frameMessage)
        {
            if (frameMessage.Type == FrameType.EVENT && frameMessage is FrameMessage<ActionMessage> actionMessage)
            {
                var traceRequestId = string.Empty;
                var traceAction = string.Empty;
                var traceRunId = string.Empty;
                var traceStatus = string.Empty;
                try
                {
                    var businessMessage = actionMessage.Payload?.Message;
                    traceRequestId = businessMessage?.RequestId ?? string.Empty;
                    traceAction = businessMessage?.Action ?? string.Empty;
                    traceRunId = businessMessage?.PayloadJobject["id"]?.ToString() ?? string.Empty;
                    traceStatus = businessMessage?.PayloadJobject["status"]?.ToString() ?? string.Empty;
                    Logger.LogDebug(
                        $"[TRACE-RUN] stage=engine.forward.begin talker={Name} " +
                        $"frameId={actionMessage.Id} requestId={traceRequestId} " +
                        $"action={traceAction} runId={traceRunId} status={traceStatus}");
                }
                catch (Exception ex)
                {
                    Logger.LogDebug(
                        $"[TRACE-RUN] stage=engine.forward.parse-failed talker={Name} frameId={actionMessage.Id}",
                        ex);
                }

                try
                {
                    if (actionMessage != null && actionMessage.Payload != null && actionMessage.Payload.Action == "run")
                    {
                        Logger.LogDebug($"HandleMessage:{JsonConvert.SerializeObject(actionMessage.Payload)}");
                    }
                }
                catch
                { }

                // 保留来源 Frame 的 Id、ReplyId、Code 与 Buffer，避免上游关联信息在转发时丢失。
                var forwarded = await ForwardActionMessageAsync(actionMessage);
                Logger.LogDebug(
                    forwarded
                        ? $"[TRACE-RUN] stage=engine.forward.ok talker={Name} " +
                          $"frameId={actionMessage.Id} requestId={traceRequestId}"
                        : $"[TRACE-RUN] stage=engine.forward.suppressed talker={Name} " +
                          $"frameId={actionMessage.Id} requestId={traceRequestId}");
            }
            else if (frameMessage.Type == FrameType.EVENT && frameMessage is FrameMessage<ActionEvent> actionEvent)
            {
                var @event = actionEvent.Payload;
                OnActionEvent(@event);
                switch (@event.Type)
                {
                    case ActionEventType.Finish:
                        RunningRequestId = string.Empty;
                        break;
                    default:
                        break;
                }
            }
            else
            {
                await HandleOthersFrameStream(frameMessage);
            }
        }

        protected virtual async Task<bool> ForwardActionMessageAsync(
            FrameMessage<ActionMessage> actionMessage)
        {
            await GetHostTalker().Notify(actionMessage);
            return true;
        }

        protected virtual void OnActionEvent(ActionEvent actionEvent)
        {
        }

        protected void ClearRunningRequest()
        {
            RunningRequestId = string.Empty;
        }

        public async Task BaseHandleAction(EngineMessage request)
        {
            try
            {
                if (request != null && request.Action == "run")
                {
                    RunningRequestId = request.RequestId;
                    Logger.LogDebug($"HandleMessage:{JsonConvert.SerializeObject(request)}");
                }
            }
            catch
            { }

            try
            {
                await Notify(new FrameMessage<ActionMessage>
                {
                    Type = FrameType.EVENT,
                    Payload = new ActionMessage
                    {
                        Action = request.Action,
                        Payload = request.Raw,
                    }
                });
            }
            catch
            {
                throw;
            }
        }
    }
}
