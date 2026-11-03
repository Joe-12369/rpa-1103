using Grpc.Core;
using GrpcDotNetNamedPipes;
using II.RPA.Communication.Abstractions;
using II.RPA.Communication.Endpoints;
using II.RPA.Communication.Exceptions;
using II.RPA.Communication.Options;

namespace II.RPA.Communication.Hosting;

public sealed class GrpcServer : IGRPCServer
{
    private readonly ServerOptions _options;
    private readonly Action<GrpcServiceRegistry> _configureServices;
    private Server? _tcpServer;
    private NamedPipeServer? _namedPipeServer;

    public GrpcServer(
        ServerOptions options,
        Action<GrpcServiceRegistry> configureServices)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _configureServices = configureServices ?? throw new ArgumentNullException(nameof(configureServices));
    }

    public GRPCEndpoint Endpoint => _options.Endpoint;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_tcpServer is not null || _namedPipeServer is not null)
        {
            throw new InvalidOperationException("The communication server has already been started.");
        }

        var registry = new GrpcServiceRegistry();
        _configureServices(registry);
        if (registry.Registrations.Count == 0)
        {
            throw new GRPCConfigurationException(
                "At least one gRPC service must be supplied when the server is created.");
        }

        StartEndpoint(registry);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_tcpServer is not null)
        {
            await _tcpServer.ShutdownAsync().ConfigureAwait(false);
            _tcpServer = null;
        }

        if (_namedPipeServer is not null)
        {
            _namedPipeServer.Dispose();
            _namedPipeServer = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }

    private void StartEndpoint(GrpcServiceRegistry registry)
    {
        switch (_options.Endpoint)
        {
            case TcpEndpoint tcp:
                StartTcpEndpoint(tcp, registry);
                break;
            case NamedPipeEndpoint namedPipe:
                StartNamedPipeEndpoint(namedPipe, registry);
                break;
            default:
                throw new GRPCConfigurationException(
                    $"Unsupported communication endpoint type '{_options.Endpoint.GetType().FullName}'.");
        }
    }

    private void StartTcpEndpoint(TcpEndpoint endpoint, GrpcServiceRegistry registry)
    {
        _tcpServer = new Server(_options.ChannelOptions);

        foreach (var registration in registry.Registrations)
        {
            _tcpServer.Services.Add(registration.Definition);
        }

        _tcpServer.Ports.Add(endpoint.Host, endpoint.Port, ServerCredentials.Insecure);
        _tcpServer.Start();
    }

    private void StartNamedPipeEndpoint(NamedPipeEndpoint endpoint, GrpcServiceRegistry registry)
    {
        _namedPipeServer = new NamedPipeServer(endpoint.PipeName, new NamedPipeServerOptions
        {
            DiagnosticLog = _options.NamedPipeDiagnosticLog
        });
        if (_options.ErrorHandler is not null)
        {
            _namedPipeServer.Error += (_, args) => _options.ErrorHandler(args.Error);
        }

        foreach (var registration in registry.Registrations)
        {
            registration.BindTo(_namedPipeServer.ServiceBinder);
        }

        _namedPipeServer.Start();
    }
}
