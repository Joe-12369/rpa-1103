using System.Threading.Channels;
using Grpc.Core;
using II.RPA.Communication.Exceptions;
using II.RPA.Communication.Options;
using II.RPA.Communication.Stream.Infrastructure;
using II.RPA.Communication.Timeout;
using Xunit;

namespace II.RPA.Communication.Tests;

public class RequestReplyStreamTimeoutTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InfiniteRequest_CanReceiveReply(bool explicitInfinite)
    {
        var transport = new TestStream();
        await using var session = CreateSession(transport, explicitInfinite
            ? TimeSpan.FromMilliseconds(1) : null);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var pending = session.SendAsync("request", explicitInfinite
            ? System.Threading.Timeout.InfiniteTimeSpan : null, cancellation.Token);
        await transport.Written.Task.WaitAsync(cancellation.Token);
        Assert.False(pending.IsCompleted);
        await transport.Replies.Writer.WriteAsync("request", cancellation.Token);
        Assert.Equal("request", await pending);
    }

    [Fact]
    public async Task ExplicitTimeout_StillExpires()
    {
        var transport = new TestStream();
        await using var session = CreateSession(transport);
        await Assert.ThrowsAsync<GRPCTimeoutException>(async () =>
            await session.SendAsync("request", TimeSpan.FromMilliseconds(50))
                .WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task DefaultInfiniteRequest_CallerCanCancel()
    {
        var transport = new TestStream();
        await using var session = CreateSession(transport);
        using var cancellation = new CancellationTokenSource();
        var pending = session.SendAsync("request", cancellationToken: cancellation.Token);
        await transport.Written.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await pending.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    private static RequestReplyStreamSession<string, string> CreateSession(
        TestStream transport, TimeSpan? defaultTimeout = null)
        => new(new AsyncDuplexStreamingCall<string, string>(transport, transport,
                Task.FromResult(new Metadata()), () => Status.DefaultSuccess,
                () => new Metadata(), () => transport.Replies.Writer.TryComplete()),
            request => request, response => response,
            new TimeoutResolver(defaultTimeout.HasValue
                ? new TimeoutOptions { DefaultRequestTimeout = defaultTimeout.Value }
                : new TimeoutOptions()));

    private sealed class TestStream : IClientStreamWriter<string>, IAsyncStreamReader<string>
    {
        public Channel<string> Replies { get; } = System.Threading.Channels.Channel.CreateUnbounded<string>();
        public TaskCompletionSource Written { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public WriteOptions? WriteOptions { get; set; }
        public string Current { get; private set; } = string.Empty;
        public Task WriteAsync(string message)
        {
            Written.TrySetResult();
            return Task.CompletedTask;
        }
        public Task CompleteAsync() => Task.CompletedTask;
        public async Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            while (await Replies.Reader.WaitToReadAsync(cancellationToken))
            {
                if (Replies.Reader.TryRead(out var response))
                {
                    Current = response;
                    return true;
                }
            }
            return false;
        }
        public void Dispose() => Replies.Writer.TryComplete();
    }
}
