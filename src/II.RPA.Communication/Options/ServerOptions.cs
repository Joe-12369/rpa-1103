using II.RPA.Communication.Endpoints;
using Grpc.Core;

namespace II.RPA.Communication.Options;

public sealed class ServerOptions
{
    public Action<string>? NamedPipeDiagnosticLog { get; set; }
    public GRPCEndpoint Endpoint { get; set; } =
        new TcpEndpoint("127.0.0.1", 0);


    public Action<Exception>? ErrorHandler { get; set; }

    public IReadOnlyList<ChannelOption> ChannelOptions { get; set; } =
        Options.ChannelOptions.DefaultServerOptions;
}
