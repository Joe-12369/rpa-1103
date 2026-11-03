using Grpc.Core;
using GrpcDotNetNamedPipes;
using II.RPA.Communication.Abstractions;
using II.RPA.Communication.Endpoints;
using II.RPA.Communication.Exceptions;
using II.RPA.Communication.Options;
using II.RPA.Communication.Proto;
using II.RPA.Communication.Stream.Infrastructure;
using II.RPA.Communication.Timeout;

namespace II.RPA.Communication.Hosting;

public sealed class GrpcClient : IGRPCClient
{
    private readonly TimeoutResolver _timeoutResolver;
    private readonly Channel? _tcpChannel;
    private bool _disposed;

    public GrpcClient(ClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _timeoutResolver = new TimeoutResolver(options.Timeout);
        var (callInvoker, tcpChannel) = CreateCallInvoker(options);
        CallInvoker = new TimeoutAwareCallInvoker(callInvoker, _timeoutResolver);
        _tcpChannel = tcpChannel;
    }

    public CallInvoker CallInvoker { get; }

    public TClient CreateClient<TClient>(Func<CallInvoker, TClient> factory)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(factory);

        return factory(CallInvoker);
    }

    public CallOptions CreateCallOptions(
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var effectiveTimeout = _timeoutResolver.Resolve(timeout);
        return new CallOptions(
            deadline: effectiveTimeout == System.Threading.Timeout.InfiniteTimeSpan
                ? null
                : DateTime.UtcNow.Add(effectiveTimeout),
            cancellationToken: cancellationToken);
    }

    public IRequestReplyStreamSession<TRequest, TResponse> CreateRequestReplyStreamSession<TRequest, TResponse>(
        AsyncDuplexStreamingCall<TRequest, TResponse> call,
        Func<TRequest, string> requestCorrelationIdSelector,
        Func<TResponse, string> responseCorrelationIdSelector)
        where TRequest : class
        where TResponse : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return new RequestReplyStreamSession<TRequest, TResponse>(
            call,
            requestCorrelationIdSelector,
            responseCorrelationIdSelector,
            _timeoutResolver);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_tcpChannel is not null)
        {
            await _tcpChannel.ShutdownAsync().ConfigureAwait(false);
        }
    }

    private static (CallInvoker Invoker, Channel? TcpChannel) CreateCallInvoker(ClientOptions options)
    {
        switch (options.Endpoint)
        {
            case TcpEndpoint tcp:
                var channel = new Channel(
                    tcp.Host,
                    tcp.Port,
                    ChannelCredentials.Insecure,
                    options.ChannelOptions);
                return (channel.CreateCallInvoker(), channel);

            case NamedPipeEndpoint namedPipe:
                return (new NamedPipeChannel(
                    ".",
                    namedPipe.PipeName,
                    new NamedPipeChannelOptions
                    {
                        ConnectionTimeout = options.NamedPipeConnectionTimeoutMilliseconds,
                        DiagnosticLog = options.NamedPipeDiagnosticLog
                    }), null);

            default:
                throw new GRPCConfigurationException(
                    $"Unsupported communication endpoint type '{options.Endpoint.GetType().FullName}'.");
        }
    }
}
