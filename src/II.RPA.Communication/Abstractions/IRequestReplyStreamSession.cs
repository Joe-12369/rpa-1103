namespace II.RPA.Communication.Abstractions;

public interface IRequestReplyStreamSession<TRequest, TResponse> : IAsyncDisposable
    where TRequest : class
    where TResponse : class
{
    Task<TResponse> SendAsync(
        TRequest request,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);
}
