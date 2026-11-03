using II.RPA.Core.Log;
using II.RPA.Core.Runtime;
using II.RPA.Core.Contracts.Talker;
using II.RPA.Core.Contracts.Talker.Engine;
using II.RPA.Core.Contracts.Talker.Engine.Message;
using II.RPA.Core.Contracts.Talker.Engine.Message.Action;
using II.RPA.Core.Talker.Models;
using II.RPA.Core.Talker.Processes;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace II.RPA.Core.Talker.Talkers
{
    public class ExecutorTalker : EngineTalker
    {
        private readonly ExecutorProcess _executorProcess;
        private readonly ITalkerManager _talkerManager;
        private readonly IRpaRuntimeContext _runtimeContext;
        private readonly object _runStateGate = new();
        private readonly SemaphoreSlim _restartGate = new(1, 1);
        private static readonly TimeSpan TerminalCompensationGrace =
            TimeSpan.FromMilliseconds(250);
        private readonly RunTerminalTracker _terminalTracker = new();
        private string? _activeRunActionId;
        private string? _activeRunRequestId;
        private string? _lastPackageRoot;
        private bool _activeRunCompleted;

        private readonly record struct InterruptedRunState(
            string RequestId,
            string ActionId,
            bool Completed);

        public ExecutorTalker(ITalkerManager talkerManager, ILogger logger, IRpaRuntimeContext runtimeContext) : base(talkerManager, logger)
        {
            _talkerManager = talkerManager;
            _runtimeContext = runtimeContext ?? throw new ArgumentNullException(nameof(runtimeContext));
            _executorProcess = new ExecutorProcess(logger, runtimeContext);
            _executorProcess.OnProcessStatusChangedEvent += ExecutorProcess_OnProcessStatusChangedEvent;
        }

        private void ExecutorProcess_OnProcessStatusChangedEvent(object? sender, Models.ProcessStatus e)
        {
            _ = HandleExecutorProcessStatusChangedAsync(e);
        }

        private async Task HandleExecutorProcessStatusChangedAsync(Models.ProcessStatus e)
        {
            try
            {
                if (e != Models.ProcessStatus.Stopped || _executorProcess.IsInitiativeStop)
                {
                    return;
                }

                // The process may be restarted as soon as the exit is observed and
                // StartCore clears LastExitCode. Capture it before any await.
                var exitCode = _executorProcess.LastExitCode;
                // Bind this callback to the tracked runs before the replacement can start.
                var exitingRuns = _terminalTracker.SnapshotExitingRuns(ConnectionGeneration);
                // The execution slot belongs to this talker, not to the connection snapshot.
                // Release it even if the tracker has no matching-generation record.
                var interruptedRun = ClearRunState(clearPackageRoot: true);
                if (interruptedRun is { } activeRun &&
                    !exitingRuns.Any(run => string.Equals(
                        run.RequestId,
                        activeRun.RequestId,
                        StringComparison.OrdinalIgnoreCase)))
                {
                    exitingRuns = exitingRuns
                        .Append(new CompensatedRunTerminal(activeRun.RequestId, activeRun.ActionId))
                        .ToArray();
                }
                Logger.LogInfo(
                    $"[TRACE-RUN] stage=executor.exit-release executor={Id} pid={Pid} " +
                    $"exitCode={exitCode} activeRequestId={interruptedRun?.RequestId ?? "<none>"} " +
                    $"connectionGeneration={ConnectionGeneration} exitingRuns={exitingRuns.Length}");
                await Task.Delay(TerminalCompensationGrace).ConfigureAwait(false);
                foreach (var run in exitingRuns)
                {
                    ClearRunState(clearPackageRoot: true, run.RequestId);
                    await ReconcileExitedRunAsync(run, exitCode).ConfigureAwait(false);
                }
                if (_runtimeContext.ClientKind == RpaClientKind.Desktop)
                {
                    await GetHostTalker().Notify(new FrameMessage<ActionMessage>
                    {
                        Type = FrameType.EVENT,
                        Payload = new ActionMessage
                        {
                            Action = EngineMessageActions.ChildRunOver,
                            Payload = ""
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("处理 Executor 进程退出事件失败", ex);
            }
        }

        private async Task ReconcileExitedRunAsync(CompensatedRunTerminal run, int? exitCode)
        {
            for (var attempt = 1; ; attempt++)
            {
                if (_terminalTracker.HasTerminalForwarded(run.RequestId))
                {
                    _terminalTracker.ClearActiveRun(run.RequestId);
                    return;
                }

                var success = _terminalTracker.TryClaimCompensation(
                    run.RequestId, exitCode, out var terminal);
                if (!success && !_terminalTracker.TryClaimTerminal(run.RequestId))
                {
                    // A real terminal is being forwarded. Recheck after it commits or rolls back.
                    await Task.Delay(TerminalCompensationGrace).ConfigureAwait(false);
                    continue;
                }

                try
                {
                    Logger.LogInfo($"[TRACE-RUN] stage=executor.reconcile executor={Id} " +
                        $"requestId={run.RequestId} exitCode={exitCode} success={success} attempt={attempt}");
                    if (success)
                    {
                        await SendCompensatedSuccessAsync(terminal).ConfigureAwait(false);
                    }
                    else
                    {
                        var response = new ResponseMessage(EngineMessageActions.Run, new Response
                        {
                            Id = run.ActionId,
                            Status = Response.ErrorStatus,
                            Vlog = string.Empty,
                            ConsoleLogShowLevel = Response.ConsoleLogSystem,
                            Text = eCommonMessageType.ExecutorTalker_cs_JinChengTuiChuRenWuZhiXingShiBai.GetMessage(),
                        }) { ResponseFor = run.RequestId };
                        // Preserve the diagnostic error event used by clients for failure details.
                        // Its failure must not prevent the business terminal from being delivered.
                        try
                        {
                            var error = new ResponseMessage(EngineMessageActions.Error, new Response
                            {
                                Id = run.ActionId,
                                Status = Response.ErrorStatus,
                                ConsoleLogShowLevel = Response.ConsoleLogRun,
                                Text = eCommonMessageType.ExecutorTalker_cs_YunXingCuoWu.GetMessage() +
                                    "[010000200040001]" +
                                    eCommonMessageType.ExecutorTalker_cs_JinChengTuiChuRenWuZhiXingShiBai.GetMessage(),
                            }) { ResponseFor = run.RequestId };
                            await GetHostTalker().Notify(new FrameMessage<ActionMessage>
                            {
                                Type = FrameType.EVENT,
                                Payload = new ActionMessage
                                {
                                    Action = EngineMessageActions.Error,
                                    Payload = EngineMessage.Serialize(error),
                                },
                            }).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning($"Executor[{Id}] exit diagnostic delivery failed: requestId={run.RequestId}", ex);
                        }
                        await GetHostTalker().Notify(new FrameMessage<ActionMessage>
                        {
                            Type = FrameType.EVENT,
                            Payload = new ActionMessage
                            {
                                Action = EngineMessageActions.Run,
                                Payload = EngineMessage.Serialize(response),
                            },
                        }).ConfigureAwait(false);
                    }
                    _terminalTracker.CommitTerminal(run.RequestId);
                    _terminalTracker.ClearActiveRun(run.RequestId);
                    return;
                }
                catch (Exception ex)
                {
                    _terminalTracker.RollbackTerminal(run.RequestId);
                    Logger.LogWarning($"Executor[{Id}] terminal reconciliation failed: " +
                        $"requestId={run.RequestId} attempt={attempt}", ex);
                    // Retry pending delivery even if the host was disconnected during exit.
                    await Task.Delay(attempt < 3 ? TerminalCompensationGrace : TimeSpan.FromSeconds(10))
                        .ConfigureAwait(false);
                }
            }
        }

        public override string Name => "executor";

        public bool IsRunning
        {
            get
            {
                lock (_runStateGate)
                {
                    return _activeRunRequestId != null;
                }
            }
        }

        public string? RunActionId
        {
            get
            {
                lock (_runStateGate)
                {
                    return _activeRunActionId;
                }
            }
        }

        public string? LastPackageRoot
        {
            get
            {
                lock (_runStateGate)
                {
                    return _lastPackageRoot;
                }
            }
        }

        public ProcessStatus ProcessStatus => _executorProcess.Status;

        public override Task Start()
        {
            _executorProcess.Start();
            return Task.CompletedTask;
        }

        public override async Task Stop()
        {
            await _executorProcess.StopAsync();
            await base.Stop();
        }

        public async Task RestartAsync(CancellationToken cancellationToken = default)
        {
            await _restartGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Cancellation may prevent entering the restart transaction, but
                // once Stop begins the replacement process must still be started.
                cancellationToken.ThrowIfCancellationRequested();

                Logger.LogDebug($"Executor[{Id}] restart started.");
                await Stop().ConfigureAwait(false);
                Logger.LogDebug($"Executor[{Id}] stopped; starting replacement.");

                var interruptedRun = ClearRunState(clearPackageRoot: true);
                if (interruptedRun is { } run)
                {
                    _terminalTracker.ClearActiveRun(run.RequestId);
                }
                await Start().ConfigureAwait(false);
                Logger.LogDebug($"Executor[{Id}] restart completed.");
            }
            finally
            {
                _restartGate.Release();
            }
        }

        public Task Restart() => RestartAsync();

        public void Suspend()
        {
            _executorProcess.Suspend();
        }

        public void Resume()
        {
            _executorProcess.Resume();
        }

        public async Task HandleAction(EngineMessage request)
        {
            if (!await TryHandleActionAsync(request).ConfigureAwait(false))
            {
                throw new InvalidOperationException($"Executor[{Id}] is already running another request.");
            }
        }

        public async Task<bool> TryHandleActionAsync(EngineMessage request)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.Action != EngineMessageActions.Run)
            {
                await BaseHandleAction(request).ConfigureAwait(false);
                return true;
            }

            var actionId = request.PayloadJobject["id"]?.Value<string>();
            if (string.IsNullOrWhiteSpace(actionId))
            {
                actionId = request.RequestId;
            }

            lock (_runStateGate)
            {
                if (_activeRunRequestId != null || !IsReady)
                {
                    return false;
                }

                _activeRunActionId = actionId;
                _activeRunRequestId = request.RequestId;
                _lastPackageRoot = request.PayloadJobject["root"]?.Value<string>();
                _activeRunCompleted = false;
                _terminalTracker.StartRun(request.RequestId, actionId, ConnectionGeneration);
            }

            try
            {
                await BaseHandleAction(request).ConfigureAwait(false);
                return true;
            }
            catch
            {
                var interruptedRun = ClearRunState(clearPackageRoot: false, request.RequestId);
                if (interruptedRun is { } run)
                {
                    _terminalTracker.ClearActiveRun(run.RequestId);
                }
                throw;
            }
        }

        public bool MatchesRun(string? actionId)
        {
            if (string.IsNullOrWhiteSpace(actionId))
            {
                return false;
            }

            lock (_runStateGate)
            {
                return string.Equals(_activeRunActionId, actionId, StringComparison.OrdinalIgnoreCase);
            }
        }

        protected override void OnActionEvent(ActionEvent actionEvent)
        {
            if (!string.Equals(actionEvent.Action, EngineMessageActions.Run, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Logger.LogInfo($"[TRACE-RUN] stage=executor.action-event requestId={actionEvent.RequestId} type={actionEvent.Type}");
            if (actionEvent.Type == ActionEventType.Done)
                _terminalTracker.MarkDone(actionEvent.RequestId);
            lock (_runStateGate)
            {
                if (_activeRunRequestId == null ||
                    !string.Equals(_activeRunRequestId, actionEvent.RequestId, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (actionEvent.Type == ActionEventType.Done)
                {
                    _activeRunCompleted = true;
                }
                else if (actionEvent.Type == ActionEventType.Finish)
                {
                    // Keep terminal tracking after releasing the execution slot.
                    _activeRunActionId = null;
                    _activeRunRequestId = null;
                    _activeRunCompleted = false;
                }
            }
        }

        protected override async Task HandleOthersFrameStream(FrameMessage frameMessage)
        {
            if (frameMessage.Type == FrameType.COMMAND && frameMessage is FrameMessage<EngineCommand> EngineCommand)
            {
                var content = EngineCommand.Payload;
                switch (content.Type)
                {
                    case EngineCommandType.MarkBanished:
                        Logger.LogInfo($"Mark Talker[{Id}] banished");
                        var requedtId = content.KeyValueArgumets.Value<string>("request_id");
                        if (!string.IsNullOrWhiteSpace(requedtId) &&
                            _terminalTracker.MarkBanished(requedtId))
                        {
                            _executorProcess.ExpectRetirement();
                            Logger.LogInfo($"Executor[{Id}] 已确认 banished: {requedtId}");
                        }
                        else
                        {
                            Logger.LogWarning("Mark banished failed");
                        }
                        break;
                    default:
                        Logger.LogWarning($"Unknow Command.Type from executor: {content.Type}");
                        break;
                }
            }
        }

        protected override async Task<bool> ForwardActionMessageAsync(
            FrameMessage<ActionMessage> actionMessage)
        {
            if (!TryGetTerminalRun(actionMessage.Payload, out var requestId, out _))
            {
                return await base.ForwardActionMessageAsync(actionMessage).ConfigureAwait(false);
            }

            if (!_terminalTracker.TryClaimTerminal(requestId))
            {
                Logger.LogDebug(
                    $"[TERMINAL-DEDUPE] ignored duplicate terminal result " +
                    $"executor={Id} requestId={requestId}");
                return false;
            }

            try
            {
                await base.ForwardActionMessageAsync(actionMessage).ConfigureAwait(false);
                _terminalTracker.CommitTerminal(requestId);
                return true;
            }
            catch
            {
                _terminalTracker.RollbackTerminal(requestId);
                throw;
            }
        }

        private async Task SendCompensatedSuccessAsync(CompensatedRunTerminal terminal)
        {
            await GetHostTalker().Notify(new FrameMessage<ActionMessage>
            {
                Type = FrameType.EVENT,
                Payload = new ActionMessage
                {
                    Action = EngineMessageActions.Run,
                    Payload = EngineMessage.Serialize(CreateCompensatedSuccess(terminal)),
                },
            }).ConfigureAwait(false);
        }

        internal static ResponseMessage CreateCompensatedSuccess(
            CompensatedRunTerminal terminal)
        {
            return new ResponseMessage(EngineMessageActions.Run, new Response
            {
                Id = terminal.ActionId,
                Status = Response.SuccessStatus,
                Vlog = string.Empty,
                ConsoleLogShowLevel = Response.ConsoleLogSystem,
            })
            {
                ResponseFor = terminal.RequestId,
            };
        }

        internal static bool TryGetTerminalRun(
            ActionMessage? actionMessage,
            out string requestId,
            out string actionId)
        {
            requestId = string.Empty;
            actionId = string.Empty;
            if (string.IsNullOrWhiteSpace(actionMessage?.Payload))
            {
                return false;
            }

            try
            {
                var raw = JObject.Parse(actionMessage.Payload);
                requestId = raw["requestId"]?.Type == JTokenType.String
                    ? raw.Value<string>("requestId") ?? string.Empty
                    : string.Empty;
                actionId = raw["payload"]?["id"]?.Type == JTokenType.String
                    ? raw["payload"]!.Value<string>("id") ?? string.Empty
                    : string.Empty;
                var status = raw["payload"]?["status"]?.Type == JTokenType.String
                    ? raw["payload"]!.Value<string>("status")
                    : null;

                return string.Equals(
                           raw.Value<string>("action"),
                           EngineMessageActions.Run,
                           StringComparison.OrdinalIgnoreCase) &&
                       !string.IsNullOrWhiteSpace(requestId) &&
                       !string.IsNullOrWhiteSpace(actionId) &&
                       status is Response.SuccessStatus or Response.ErrorStatus or Response.StopStatus;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private InterruptedRunState? ClearRunState(
            bool clearPackageRoot,
            string? expectedRequestId = null)
        {
            lock (_runStateGate)
            {
                if (expectedRequestId != null &&
                    !string.Equals(_activeRunRequestId, expectedRequestId, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                InterruptedRunState? result = _activeRunRequestId == null
                    ? null
                    : new InterruptedRunState(
                        _activeRunRequestId,
                        _activeRunActionId ?? _activeRunRequestId,
                        _activeRunCompleted);
                _activeRunActionId = null;
                _activeRunRequestId = null;
                _activeRunCompleted = false;
                if (clearPackageRoot)
                {
                    _lastPackageRoot = null;
                }
                ClearRunningRequest();
                return result;
            }
        }
    }
}
