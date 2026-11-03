namespace II.RPA.Core.Abstract;

/// <summary>
/// Explicit one-time coordination point for services that must wait until ADB is usable.
/// </summary>
public interface IAdbStartupSignal
{
    bool IsReady { get; }

    void MarkReady();

    Task WaitUntilReadyAsync(CancellationToken cancellationToken = default);
}
