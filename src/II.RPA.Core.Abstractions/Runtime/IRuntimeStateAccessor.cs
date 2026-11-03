namespace II.RPA.Core.Runtime;

/// <summary>
/// Read-only access to runtime state supplied by the connected Host worker.
/// The boundary keeps gRPC services independent from the concrete Talker manager.
/// </summary>
public interface IRuntimeStateAccessor
{
    bool TryGetSharedJson(string key, out string json);
}
