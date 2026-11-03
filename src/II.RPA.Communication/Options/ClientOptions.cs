using II.RPA.Communication.Endpoints;
using Grpc.Core;

namespace II.RPA.Communication.Options;

public sealed class ClientOptions
{
    public Action<string>? NamedPipeDiagnosticLog { get; set; }
    public GRPCEndpoint Endpoint { get; set; } =
        new TcpEndpoint("127.0.0.1", 0);

    public TimeoutOptions Timeout { get; set; } = new();

    public IReadOnlyList<ChannelOption> ChannelOptions { get; set; } =
        Options.ChannelOptions.DefaultClientOptions;

    public int NamedPipeConnectionTimeoutMilliseconds { get; set; } = 30_000;
}
