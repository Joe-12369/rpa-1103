using Grpc.Core;

namespace II.RPA.Core.Abstract;

/// <summary>
/// Inverts the Worker gRPC dependency so the service assembly does not need to
/// reference a concrete Talker implementation.
/// </summary>
public interface IWorkerTalkerRegistrar
{
    void Initialization();

    Task RegisterTalker(
        object requestStream,
        object responseStream,
        ServerCallContext context);

    Task RegisterChunkTalker(
        object requestStream,
        object responseStream,
        ServerCallContext context);

    Task StopAsync();
}
