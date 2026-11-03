using System.Collections.Concurrent;
using Grpc.Core;
using GrpcDotNetNamedPipes;
using II.RPA.Communication.Endpoints;
using II.RPA.Communication.Hosting;
using II.RPA.Communication.Options;
using Xunit;

namespace II.RPA.Communication.Tests;

public class NamedPipeDiagnosticsTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Diagnostics_AreForwarded_AndCannotBreakRpc(bool throwFromLogger)
    {
        var clientLog = new ConcurrentQueue<string>();
        var serverLog = new ConcurrentQueue<string>();
        Action<string> logger(ConcurrentQueue<string> messages) => message =>
        {
            messages.Enqueue(message);
            if (throwFromLogger) throw new InvalidOperationException("diagnostic sink failed");
        };
        var pipeName = $"rpa-diagnostics-{Guid.NewGuid():N}";
        using var server = new NamedPipeServer(pipeName, new NamedPipeServerOptions
        {
            DiagnosticLog = logger(serverLog)
        });
        server.ServiceBinder.AddMethod<string, string>(EchoCallInvoker.EchoMethod,
            (string request, ServerCallContext context) => Task.FromResult(request));
        server.Start();
        await using var client = new GrpcClient(new ClientOptions
        {
            Endpoint = new NamedPipeEndpoint(pipeName),
            NamedPipeDiagnosticLog = logger(clientLog)
        });
        var invoker = client.CreateClient(value => value);
        using var call = invoker.AsyncUnaryCall(EchoCallInvoker.EchoMethod, null,
            new CallOptions(deadline: DateTime.UtcNow.AddSeconds(10)), "private-payload");
        Assert.Equal("private-payload", await call.ResponseAsync.WaitAsync(TimeSpan.FromSeconds(15)));
        Assert.Contains(clientLog, line => line.Contains("Pipe connected elapsedMs="));
        Assert.Contains(serverLog, line => line.Contains("RequestInit received acceptId="));
        Assert.DoesNotContain(clientLog.Concat(serverLog), line => line.Contains("private-payload"));
    }
}
