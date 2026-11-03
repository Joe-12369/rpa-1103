using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.IPC.NamedPipe;
using II.RPA.IPC.NamedPipe.Abstractions;
using II.RPA.IPC.NamedPipe.Client;
using II.RPA.IPC.NamedPipe.Framing;
using II.RPA.IPC.NamedPipe.Server;
using Xunit;

namespace II.RPA.IPC.Tests.Stress
{
    public class NamedPipeStressTests
    {
        [Fact]
        public async Task MultipleClientsExchangeConcurrentFramesWithoutLossOrCorruption()
        {
            const int clientCount = 4, messagesPerClient = 20;
            var options = new NamedPipeServerOptions { MaxConnections = clientCount, MaxPendingRequestsPerConnection = 8, MaxPendingRequests = 32 };
            using (NamedPipeServer server = await StartServerAsync(NewPipeName(), EchoHandler(), options))
            {
                var clients = new List<NamedPipeClient>();
                var received = new ConcurrentDictionary<string, byte>();
                var allReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                try
                {
                    for (int index = 0; index < clientCount; index++)
                    {
                        var client = new NamedPipeClient(server.PipeName);
                        client.MessageReceived += (sender, args) =>
                        {
                            received.TryAdd(args.Payload[0] + ":" + args.Payload[1], 0);
                            if (received.Count == clientCount * messagesPerClient) allReceived.TrySetResult(true);
                        };
                        clients.Add(client);
                    }
                    await Task.WhenAll(clients.Select(client => client.ConnectAsync(CancellationToken.None)));
                    var sends = new List<Task>();
                    for (byte client = 0; client < clientCount; client++)
                        for (byte message = 0; message < messagesPerClient; message++)
                            sends.Add(clients[client].SendAsync(new[] { client, message }, CancellationToken.None));
                    await Task.WhenAll(sends);
                    await WithTimeout(allReceived.Task);
                    Assert.Equal(clientCount * messagesPerClient, received.Count);
                }
                finally
                {
                    foreach (NamedPipeClient client in clients) client.Dispose();
                    await server.StopAsync();
                }
            }
        }

        [Fact]
        public async Task ClientCanRapidlyDisconnectAndReconnect()
        {
            const int cycles = 20;
            using (NamedPipeServer server = await StartServerAsync(NewPipeName(), EchoHandler(), new NamedPipeServerOptions()))
            using (var client = new NamedPipeClient(server.PipeName))
            {
                var responses = new ConcurrentDictionary<byte, byte>();
                client.MessageReceived += (sender, args) => responses.TryAdd(args.Payload[0], 0);
                for (byte cycle = 0; cycle < cycles; cycle++)
                {
                    await client.ConnectAsync(CancellationToken.None);
                    await client.SendAsync(new[] { cycle }, CancellationToken.None);
                    byte expected = cycle;
                    await WaitUntilAsync(() => responses.ContainsKey(expected));
                    await client.DisconnectAsync();
                }
                Assert.Equal(cycles, responses.Count);
                await server.StopAsync();
            }
        }

        [Fact]
        public async Task AbruptDisconnectCancelsHandlerAndReleasesRequestPermits()
        {
            int calls = 0;
            var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var handler = new DelegateHandler(async (context, request, token) =>
            {
                if (Interlocked.Increment(ref calls) == 1)
                {
                    entered.TrySetResult(true);
                    try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
                    catch (OperationCanceledException) { cancelled.TrySetResult(true); throw; }
                }
                return request;
            });
            var options = new NamedPipeServerOptions { MaxConnections = 1, MaxPendingRequestsPerConnection = 1, MaxPendingRequests = 1 };
            using (NamedPipeServer server = await StartServerAsync(NewPipeName(), handler, options))
            {
                using (NamedPipeClientStream abandoned = await ConnectRawAsync(server.PipeName))
                {
                    await NamedPipeFrameWriter.WriteDataAsync(abandoned, new byte[] { 1 }, 1024, CancellationToken.None);
                    await WithTimeout(entered.Task);
                }
                await WithTimeout(cancelled.Task);
                using (var replacement = new NamedPipeClient(server.PipeName))
                {
                    var response = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
                    replacement.MessageReceived += (sender, args) => response.TrySetResult(args.Payload);
                    await replacement.ConnectAsync(CancellationToken.None);
                    await replacement.SendAsync(new byte[] { 2 }, CancellationToken.None);
                    Assert.Equal(new byte[] { 2 }, await WithTimeout(response.Task));
                    await replacement.DisconnectAsync();
                }
                await server.StopAsync();
            }
        }

        [Fact]
        public async Task StoppedServerReleasesPipeNameForImmediateRestart()
        {
            string pipeName = NewPipeName();
            using (NamedPipeServer first = await StartServerAsync(pipeName, EchoHandler(), new NamedPipeServerOptions()))
            {
                using (NamedPipeClientStream connection = await ConnectRawAsync(pipeName)) { }
                await first.StopAsync();
            }
            using (NamedPipeServer second = await StartServerAsync(pipeName, EchoHandler(), new NamedPipeServerOptions()))
            using (NamedPipeClientStream connection = await ConnectRawAsync(pipeName))
            {
                await NamedPipeFrameWriter.WriteDataAsync(connection, new byte[] { 7 }, 1024, CancellationToken.None);
                Assert.Equal(7, (await WithTimeout(NamedPipeFrameReader.ReadAsync(connection, 1024, CancellationToken.None))).Payload[0]);
                await second.StopAsync();
            }
        }

        private static DelegateHandler EchoHandler() => new DelegateHandler((context, request, token) => Task.FromResult(request));
        private static string NewPipeName() => "ii.rpa.ipc.stress." + Guid.NewGuid().ToString("N");
        private static async Task<NamedPipeServer> StartServerAsync(string name, INamedPipeRequestHandler handler, NamedPipeServerOptions options)
        { var server = new NamedPipeServer(name, handler, options); await server.StartAsync(); return server; }
        private static async Task<NamedPipeClientStream> ConnectRawAsync(string name)
        { var client = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous); await client.ConnectAsync(5000); return client; }
        private static async Task WithTimeout(Task task)
        { Task completed = await Task.WhenAny(task, Task.Delay(10000)); if (!ReferenceEquals(completed, task)) throw new TimeoutException(); await task; }
        private static async Task<T> WithTimeout<T>(Task<T> task)
        { Task completed = await Task.WhenAny(task, Task.Delay(10000)); if (!ReferenceEquals(completed, task)) throw new TimeoutException(); return await task; }
        private static async Task WaitUntilAsync(Func<bool> condition)
        { DateTime deadline = DateTime.UtcNow.AddSeconds(10); while (!condition()) { if (DateTime.UtcNow >= deadline) throw new TimeoutException(); await Task.Delay(10); } }

        private sealed class DelegateHandler : INamedPipeRequestHandler
        {
            private readonly Func<NamedPipeRequestContext, byte[], CancellationToken, Task<byte[]>> _handler;
            public DelegateHandler(Func<NamedPipeRequestContext, byte[], CancellationToken, Task<byte[]>> handler) { _handler = handler; }
            public Task<byte[]> HandleRequestAsync(NamedPipeRequestContext context, byte[] request, CancellationToken cancellationToken) => _handler(context, request, cancellationToken);
        }
    }
}
