using II.RPA.Communication.Endpoints;

namespace II.RPA.Communication.Abstractions;

public interface IGRPCServer : IAsyncDisposable
{
    GRPCEndpoint Endpoint { get; }

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}
