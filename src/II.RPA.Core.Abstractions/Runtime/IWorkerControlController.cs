using Newtonsoft.Json.Linq;

namespace II.RPA.Core.Runtime;

/// <summary>
/// Stable application boundary for worker diagnostics and control operations used by
/// the HTTP and gRPC business entry points. The implementation is provided by the
/// refactored Core Talker manager.
/// </summary>
public interface IWorkerControlController
{
    IReadOnlyCollection<JObject> GetWorkers(WorkerQuery? query = null);

    JObject GetTopShare();

    Task StopWorkerAsync(int pid, CancellationToken cancellationToken = default);

    Task StartEventMonitorAsync(CancellationToken cancellationToken = default);

    Task HandleEngineActionAsync(
        string? role,
        string serializedMessage,
        CancellationToken cancellationToken = default);

    void SetShare(WorkerQuery query, string key, JObject value);

    void SetMeta(WorkerQuery query, WorkerMetadata metadata);

    Task<JObject> EnsureHostWorkerAsync(CancellationToken cancellationToken = default);

    Task<JObject> RunHostAsync(
        string serializedMessage,
        CancellationToken cancellationToken = default);

    Task StopHostAsync(string actionId, CancellationToken cancellationToken = default);

    Task RestartEngineWorkersAsync(
        bool includeInterpreter,
        CancellationToken cancellationToken = default);

    Task<bool> RequestOperatorWindowAsync(
        OperatorWindowRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> SyncUserInfoToChildCoreServerAsync(
        string userInfo,
        CancellationToken cancellationToken = default);

    void WriteShareChannel(string key, JObject value);
}

public sealed class WorkerQuery
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Optional logical family used by diagnostics routes. Supported values are
    /// <c>host</c> and <c>engine</c>.
    /// </summary>
    public string Group { get; set; } = string.Empty;
}

public sealed class WorkerMetadata
{
    public int Pid { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}

public sealed class OperatorWindowRequest
{
    public int WindowType { get; set; }

    public int WindowState { get; set; }

    public TimeSpan Timeout { get; set; } = TimeSpan.FromMilliseconds(500);
}
