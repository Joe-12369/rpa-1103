using II.RPA.Core.Models;

namespace II.RPA.Core.Runtime;

/// <summary>
/// Stable application boundary for RuntimeHost operations. Callers should not depend on
/// a concrete process provider, Talker, or gRPC client.
/// </summary>
public interface IRuntimeHostController
{
    string? CurrentPackagePath { get; }
    string? ZBrainServicePath { get; set; }
    bool IsInitialized { get; }
    bool IsIpaInitialized { get; set; }
    Task<object> InitializeAsync(string packagePath, string installationPath, CancellationToken cancellationToken = default);
    Task<object> RunAsync(RuntimeHostRunRequest request, CancellationToken cancellationToken = default);
    Task SetParametersAsync(RuntimeHostSetParametersRequest request, CancellationToken cancellationToken = default);
    Task<object> DisposeAsync(string packagePath, CancellationToken cancellationToken = default);
    bool ExtendTimeout(RuntimeHostTimeoutExtension extension);
    Task RestartAsync(bool dueToTimeout = false, CancellationToken cancellationToken = default);
    Task<bool> TryRestartAfterRunEndAsync(CancellationToken cancellationToken = default);
}

public enum RuntimeHostExecutionMode
{
    Queued = 0,
    Direct = 1
}

public sealed class RuntimeHostRunRequest
{
    public string PackagePath { get; set; } = string.Empty;
    public string PluginName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string JsonParameters { get; set; } = string.Empty;
    public RuntimeRunParameters RunParameters { get; set; } = RuntimeRunParameters.DefaultValue;
    public bool CancelRuntimeOnTimeout { get; set; } = true;
    public bool AutoInitialize { get; set; } = true;
    public string RunMode { get; set; } = string.Empty;
    public PluginRunOptions? Options { get; set; }
    public RuntimeHostExecutionMode ExecutionMode { get; set; } = RuntimeHostExecutionMode.Queued;
}

public sealed class RuntimeHostSetParametersRequest
{
    public string PackagePath { get; set; } = string.Empty;
    public string JsonParameters { get; set; } = string.Empty;
    public bool AutoInitialize { get; set; } = true;
}

public sealed class RuntimeHostTimeoutExtension
{
    public string RunUniqueId { get; set; } = string.Empty;
    public long NewTimeout { get; set; } = 20000;
    public string? TimeoutMessage { get; set; }
}
