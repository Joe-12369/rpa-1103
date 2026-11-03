using Grpc.Core;

namespace II.RPA.Communication.Abstractions;

public interface IGRPCClient : IAsyncDisposable
{
    CallInvoker CallInvoker { get; }

    TClient CreateClient<TClient>(Func<CallInvoker, TClient> factory);

    CallOptions CreateCallOptions(
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);

    IRequestReplyStreamSession<TRequest, TResponse> CreateRequestReplyStreamSession<TRequest, TResponse>(
        AsyncDuplexStreamingCall<TRequest, TResponse> call,
        Func<TRequest, string> requestCorrelationIdSelector,
        Func<TResponse, string> responseCorrelationIdSelector)
        where TRequest : class
        where TResponse : class;
}
