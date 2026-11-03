using II.RPA.Core.Contracts.Talker;
using II.RPA.Core.Contracts.Talker.Engine.Message;
using II.RPA.Core.Contracts.Talker.Engine.Message.Action;
using II.RPA.Core.Contracts.Talker.Zdesktop.Message;
using II.RPA.Core.Log;
using II.RPA.Core.Runtime;
using II.RPA.Core.Talker.Models;
using II.RPA.Core.Talker.Talkers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace II.RPA.Core.Talker;

/// <summary>
/// Core Talker implementation of the business-facing worker control boundary.
/// This class deliberately uses explicit calls and request completion tasks instead
/// of exposing worker streams or an observable event bus to business callers.
/// </summary>
public sealed class CoreWorkerControlController : IWorkerControlController
{
    private readonly TalkerManager _talkerManager;
    private readonly ILogger _logger;
    private readonly IRpaRuntimeContext _runtimeContext;
    private readonly SemaphoreSlim _eventMonitorGate = new(1, 1);

    public CoreWorkerControlController(
        TalkerManager talkerManager,
        ILogger logger,
        IRpaRuntimeContext runtimeContext)
    {
        _talkerManager = talkerManager ?? throw new ArgumentNullException(nameof(talkerManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runtimeContext = runtimeContext ?? throw new ArgumentNullException(nameof(runtimeContext));
    }

    public IReadOnlyCollection<JObject> GetWorkers(WorkerQuery? query = null)
    {
        return _talkerManager
            .GetTalkers<BaseTalker>(readyOnly: false)
            .Where(talker => Matches(talker, query))
            .Select(CreateSnapshot)
            .ToArray();
    }

    public JObject GetTopShare() => _talkerManager.CreateShareSnapshot();

    public async Task StopWorkerAsync(int pid, CancellationToken cancellationToken = default)
    {
        if (pid <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pid), "Worker pid must be greater than zero.");
        }

        var talker = _talkerManager
            .GetTalkers<BaseTalker>(readyOnly: false)
            .FirstOrDefault(candidate => candidate.Pid == pid)
            ?? throw new InvalidOperationException($"Worker with pid [{pid}] was not found.");

        cancellationToken.ThrowIfCancellationRequested();
        await talker.Stop().ConfigureAwait(false);
    }

    public async Task StartEventMonitorAsync(CancellationToken cancellationToken = default)
    {
        await _eventMonitorGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var monitor = _talkerManager
                .GetTalkers<EventMonitorTalker>(readyOnly: false)
                .FirstOrDefault();
            if (monitor == null)
            {
                monitor = new EventMonitorTalker(_talkerManager, _logger, _runtimeContext);
                await monitor.Start().ConfigureAwait(false);
                _talkerManager.AddTalker(monitor);
                return;
            }

            if (!monitor.IsReady)
            {
                await monitor.Start().ConfigureAwait(false);
            }
        }
        finally
        {
            _eventMonitorGate.Release();
        }
    }

    public async Task HandleEngineActionAsync(
        string? role,
        string serializedMessage,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serializedMessage);
        var talker = _talkerManager
            .GetTalkers<EngineTalker>(readyOnly: true)
            .FirstOrDefault(candidate => string.IsNullOrWhiteSpace(role) ||
                string.Equals(candidate.Name, NormalizeRole(role), StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"No ready Engine worker matched role [{role}].");

        cancellationToken.ThrowIfCancellationRequested();
        var message = EngineMessage.Parse(serializedMessage);
        if (talker is ExecutorTalker executor)
        {
            await executor.HandleAction(message).ConfigureAwait(false);
        }
        else
        {
            await talker.BaseHandleAction(message).ConfigureAwait(false);
        }
    }

    public void SetShare(WorkerQuery query, string key, JObject value)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        _ = FindRequiredWorker(query);
        _talkerManager.SetShare(key, value);
    }

    public void SetMeta(WorkerQuery query, WorkerMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(metadata);
        var talker = FindRequiredWorker(query);

        if (!string.IsNullOrWhiteSpace(metadata.Role) &&
            !string.Equals(talker.Name, NormalizeRole(metadata.Role), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Worker [{talker.Id}] has immutable role [{talker.Name}] and cannot become [{metadata.Role}].");
        }

        if (metadata.Pid > 0)
        {
            talker.PrepareRegistration(metadata.Pid);
        }
    }

    public async Task<JObject> EnsureHostWorkerAsync(CancellationToken cancellationToken = default)
    {
        var host = _talkerManager
            .GetTalkers<HostTalker>(readyOnly: false)
            .FirstOrDefault();
        if (host == null)
        {
            host = new HostTalker(_talkerManager, _logger, _runtimeContext);
            _talkerManager.AddTalker(host);
            cancellationToken.ThrowIfCancellationRequested();
            await host.Start().ConfigureAwait(false);
        }

        return CreateSnapshot(host);
    }

    public async Task<JObject> RunHostAsync(
        string serializedMessage,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serializedMessage);
        var host = GetRequiredHost();
        cancellationToken.ThrowIfCancellationRequested();
        await host.RunAsync(EngineMessage.Parse(serializedMessage), cancellationToken).ConfigureAwait(false);
        return CreateSnapshot(host);
    }

    public Task StopHostAsync(string actionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);
        return GetRequiredHost().StopRunAsync(actionId, cancellationToken);
    }

    public async Task RestartEngineWorkersAsync(
        bool includeInterpreter,
        CancellationToken cancellationToken = default)
    {
        var restarts = _talkerManager
            .GetTalkers<ExecutorTalker>(readyOnly: false)
            .Select(talker => talker.RestartAsync(cancellationToken))
            .ToList();
        if (includeInterpreter)
        {
            restarts.AddRange(_talkerManager
                .GetTalkers<InterpreterTalker>(readyOnly: false)
                .Select(talker => talker.RestartAsync(cancellationToken)));
        }

        await Task.WhenAll(restarts).ConfigureAwait(false);
    }

    public Task<bool> RequestOperatorWindowAsync(
        OperatorWindowRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return GetRequiredHost().RequestOperatorWindowAsync(
            request.WindowType,
            request.WindowState,
            request.Timeout,
            cancellationToken);
    }

    public async Task<bool> SyncUserInfoToChildCoreServerAsync(
        string userInfo,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userInfo) ||
            !_talkerManager.TryGetTalker<ChildCoreServerTalker>(out var childTalker) ||
            !childTalker.IsReady)
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await childTalker.Notify(new FrameMessage<UserInfoMessage>
        {
            Type = FrameType.EVENT,
            Payload = new UserInfoMessage
            {
                Action = "set",
                Payload = JsonConvert.SerializeObject(userInfo)
            }
        }).ConfigureAwait(false);
        return true;
    }

    public void WriteShareChannel(string key, JObject value)
        => _talkerManager.WriteShareChannel(key, value);

    private HostTalker GetRequiredHost()
    {
        return _talkerManager
            .GetTalkers<HostTalker>(readyOnly: false)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("Host worker was not found.");
    }

    private BaseTalker FindRequiredWorker(WorkerQuery query)
    {
        return _talkerManager
            .GetTalkers<BaseTalker>(readyOnly: false)
            .FirstOrDefault(talker => Matches(talker, query))
            ?? throw new InvalidOperationException("No worker matched the supplied selector.");
    }

    private bool Matches(BaseTalker talker, WorkerQuery? query)
    {
        if (query == null)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(query.Id) &&
            !string.Equals(talker.Id, query.Id, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.Name) &&
            !string.Equals(talker.Name, query.Name, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(GetSummaryName(talker), query.Name, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.Role) &&
            !string.Equals(talker.Name, NormalizeRole(query.Role), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return query.Group?.Trim().ToLowerInvariant() switch
        {
            "host" => talker is HostTalker,
            "engine" => talker is EngineTalker,
            _ => true
        };
    }

    private JObject CreateSnapshot(BaseTalker talker)
    {
        var snapshot = new JObject
        {
            ["Id"] = talker.Id,
            ["Pid"] = talker.Pid > 0 ? JToken.FromObject(talker.Pid) : JValue.CreateNull(),
            ["Name"] = GetSummaryName(talker),
            ["ProcessStatus"] = GetProcessStatus(talker),
            ["Share"] = new JObject(),
            ["Meta"] = new JObject
            {
                ["Pid"] = Math.Max(0, talker.Pid),
                ["Shadow"] = false,
                ["Name"] = GetMetaName(talker),
                ["Role"] = talker.Name,
                ["TalkerId"] = talker.Id,
                ["Temporary"] = false,
                ["IsChild"] = talker is ZBrainTalker zBrain && zBrain.IsChildSession
            },
            ["GrpcState"] = talker.LifecycleState.ToString(),
            ["GuardState"] = GetProcessStatus(talker)
        };

        if (talker is ExecutorTalker executor)
        {
            snapshot["ProcessingMessages"] = new JArray();
            snapshot["RunActionId"] = executor.RunActionId;
            snapshot["PackageRoot"] = executor.LastPackageRoot;
        }

        return snapshot;
    }

    private string GetMetaName(BaseTalker talker)
    {
        return talker switch
        {
            EngineTalker => "rpa-engine",
            HostTalker => _runtimeContext.ClientId,
            ZBrainTalker => "rpa-core",
            _ => talker.Name
        };
    }

    private static string GetSummaryName(BaseTalker talker)
    {
        return talker switch
        {
            EngineTalker => "Engine",
            HostTalker => "Host",
            ZBrainTalker => "ZBrain",
            _ => talker.Name
        };
    }

    private static string GetProcessStatus(BaseTalker talker)
    {
        return talker switch
        {
            ExecutorTalker executor => executor.ProcessStatus.ToString(),
            InterpreterTalker interpreter => interpreter.ProcessStatus.ToString(),
            ZBrainTalker zBrain => zBrain.ProcessStatus.ToString(),
            EventMonitorTalker eventMonitor => eventMonitor.ProcessStatus.ToString(),
            MobileProviderTalker mobileProvider => mobileProvider.ProcessStatus.ToString(),
            ZDeskTopTalker zDesktop => zDesktop.ProcessStatus.ToString(),
            _ => talker.LifecycleState.ToString()
        };
    }

    private static string NormalizeRole(string role)
    {
        return role.Trim().ToLowerInvariant() switch
        {
            "zbrain" => "z_brain",
            "z_desktop" => "zdesktop",
            var normalized => normalized
        };
    }
}
