using Grpc.Core;
using GrpcDotNetNamedPipes;
using II.RPA.Communication.Proto;
using II.RPA.Communication.Timeout;
using II.RPA.Communication.Options;
using Xunit;

namespace II.RPA.Communication.Tests;

public class NamedPipeDeadlineTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InfiniteDeadline_ReachesServerWithoutDeadline(bool utcKind)
    {
        var deadline = DateTime.SpecifyKind(DateTime.MaxValue,
            utcKind ? DateTimeKind.Utc : DateTimeKind.Unspecified);
        await AssertRoundTrip(deadline, DateTime.MaxValue);
    }

    [Fact]
    public async Task FiniteUtcDeadline_IsPreservedOnServer()
    {
        var deadline = DateTime.UtcNow.AddMinutes(1);
        await AssertRoundTrip(deadline, deadline);
    }

    [Fact]
    public async Task InfiniteDeadline_CallerCanCancelRunningCall()
    {
        var pipeName = $"rpa-deadline-test-{Guid.NewGuid():N}";
        using var server = new NamedPipeServer(pipeName);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        server.ServiceBinder.AddMethod<string, string>(EchoCallInvoker.EchoMethod,
            async (string request, ServerCallContext context) =>
            {
                entered.TrySetResult();
                await release.Task;
                return request;
            });
        server.Start();

        var invoker = new TimeoutAwareCallInvoker(new NamedPipeChannel(".", pipeName),
            new TimeoutResolver(new TimeoutOptions()));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var call = invoker.AsyncUnaryCall(EchoCallInvoker.EchoMethod, null,
            new CallOptions(deadline: DateTime.MaxValue, cancellationToken: cancellation.Token), "ping");

        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            cancellation.Cancel();
            var exception = await Record.ExceptionAsync(async () =>
                await call.ResponseAsync.WaitAsync(TimeSpan.FromSeconds(5)));

            Assert.True(exception is OperationCanceledException or RpcException { StatusCode: StatusCode.Cancelled },
                $"Expected caller cancellation, got: {exception}");
        }
        finally
        {
            release.TrySetResult();
        }
    }

    private static async Task AssertRoundTrip(DateTime deadline, DateTime expectedDeadline)
    {
        var pipeName = $"rpa-deadline-test-{Guid.NewGuid():N}";
        using var server = new NamedPipeServer(pipeName);
        server.ServiceBinder.AddMethod<string, string>(EchoCallInvoker.EchoMethod,
            (string request, ServerCallContext context) =>
            {
                Assert.Equal(expectedDeadline, context.Deadline);
                return Task.FromResult($"echo:{request}");
            });
        server.Start();

        var invoker = new TimeoutAwareCallInvoker(new NamedPipeChannel(".", pipeName),
            new TimeoutResolver(new TimeoutOptions()));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var call = invoker.AsyncUnaryCall(EchoCallInvoker.EchoMethod, null,
            new CallOptions(deadline: deadline, cancellationToken: cancellation.Token), "ping");

        Assert.Equal("echo:ping", await call.ResponseAsync.WaitAsync(TimeSpan.FromSeconds(15)));
    }
}
