namespace II.RPA.Core.Runtime;

/// <summary>
/// Stable application boundary for ZBrain worker selection, package switching, and HTTP proxying.
/// </summary>
public interface IZBrainController
{
    ZBrainWorkerSnapshot? GetCurrentWorker(bool childSession = false);
    IReadOnlyCollection<ZBrainWorkerSnapshot> GetWorkers();
    Task<ZBrainPackageSwitchResult> SwitchPackageAsync(string packagePath, bool scan = true, bool childSession = false, CancellationToken cancellationToken = default);
    Task<string?> ProxyAsync(ZBrainProxyRequestData request, bool childSession = false, CancellationToken cancellationToken = default);
    Task StopWorkerAsync(int? pid = null, bool childSession = false, CancellationToken cancellationToken = default);
}

public sealed class ZBrainWorkerSnapshot
{
    public string Id { get; set; } = string.Empty;
    public int? Pid { get; set; }
    public string State { get; set; } = string.Empty;
    public string ProcessStatus { get; set; } = string.Empty;
    public bool IsReady { get; set; }
    public bool IsChildSession { get; set; }
    public bool PluginLoaded { get; set; }
}

public sealed class ZBrainProxyRequestData
{
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    public string Body { get; set; } = string.Empty;
}

public sealed class ZBrainPackageSwitchResult
{
    public bool IsSuccess { get; set; }
    public string Code { get; set; } = "0";
    public string Message { get; set; } = string.Empty;

    public static ZBrainPackageSwitchResult Success(string message = "switch plugin") => new()
    {
        IsSuccess = true,
        Message = message
    };

    public static ZBrainPackageSwitchResult Failure(string code, string message) => new()
    {
        IsSuccess = false,
        Code = code,
        Message = message
    };
}
