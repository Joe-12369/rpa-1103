namespace II.RPA.Core.Runtime;

/// <summary>
/// Stable boundary for invoking MobileProvider without exposing its process,
/// named-pipe transport, or generated gRPC types to callers.
/// </summary>
public interface IMobileProviderController
{
    bool IsEnabled { get; }

    Task<MobilePluginInvocationResult> InvokeAsync(
        string action,
        string input,
        int timeout,
        CancellationToken cancellationToken = default);
}

public sealed class MobilePluginInvocationResult
{
    public string Code { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Info { get; set; } = string.Empty;
}
