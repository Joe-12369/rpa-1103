using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.IPC.NamedPipe;
using II.RPA.IPC.NamedPipe.Abstractions;
using II.RPA.IPC.NamedPipe.Framing;
using II.RPA.IPC.NamedPipe.Server;
using Xunit;

namespace II.RPA.IPC.Tests.Server
{
    public class NamedPipeServerTests
    {
        [Fact]
        public async Task SameConnectionRequestsMayCompleteOutOfOrder()
        {
            var responses = new ConcurrentDictionary<byte, TaskCompletionSource<byte[]>>();
            var handler = new DelegateHandler((context, request, token) =>
            {
                var completion = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
                responses[request[0]] = completion;
                return completion.Task;
            });
            using (NamedPipeServer server = await StartServerAsync(handler, new NamedPipeServerOptions()))
            using (NamedPipeClientStream client = await ConnectAsync(server.PipeName))
            {
                await NamedPipeFrameWriter.WriteDataAsync(client, new byte[] { 1 }, 1024, CancellationToken.None);
                await NamedPipeFrameWriter.WriteDataAsync(client, new byte[] { 2 }, 1024, CancellationToken.None);
                await WaitUntilAsync(() => responses.Count == 2);
                responses[2].SetResult(new byte[] { 22 });
                NamedPipeFrame first = await NamedPipeFrameReader.ReadAsync(client, 1024, CancellationToken.None);
                responses[1].SetResult(new byte[] { 11 });
                NamedPipeFrame second = await NamedPipeFrameReader.ReadAsync(client, 1024, CancellationToken.None);
                Assert.Equal(22, first.Payload[0]);
                Assert.Equal(11, second.Payload[0]);
                await server.StopAsync();
            }
        }

        [Fact]
        public async Task BackpressureNeverExceedsConfiguredHandlerLimits()
        {
            int active = 0;
            int maximum = 0;
            var release = new SemaphoreSlim(0);
            var handler = new DelegateHandler(async (context, request, token) =>
            {
                int current = Interlocked.Increment(ref active);
                UpdateMaximum(ref maximum, current);
                try { await release.WaitAsync(token); return request; }
                finally { Interlocked.Decrement(ref active); }
            });
            var options = new NamedPipeServerOptions { MaxConnections = 2, MaxPendingRequestsPerConnection = 2, MaxPendingRequests = 2, MaxFrameLength = 1024 };
            using (NamedPipeServer server = await StartServerAsync(handler, options))
            using (NamedPipeClientStream first = await ConnectAsync(server.PipeName))
            using (NamedPipeClientStream second = await ConnectAsync(server.PipeName))
            {
                for (byte value = 1; value <= 3; value++) await NamedPipeFrameWriter.WriteDataAsync(first, new[] { value }, 1024, CancellationToken.None);
                await NamedPipeFrameWriter.WriteDataAsync(second, new byte[] { 4 }, 1024, CancellationToken.None);
                await WaitUntilAsync(() => Volatile.Read(ref active) == 2);
                await Task.Delay(100);
                Assert.Equal(2, Volatile.Read(ref maximum));
                release.Release(4);
                for (int i = 0; i < 3; i++) await NamedPipeFrameReader.ReadAsync(first, 1024, CancellationToken.None);
                await NamedPipeFrameReader.ReadAsync(second, 1024, CancellationToken.None);
                await server.StopAsync();
            }
        }

        [Fact]
        public async Task PingReturnsPongWithoutCallingHandler()
        {
            int calls = 0;
            var handler = new DelegateHandler((context, request, token) => { Interlocked.Increment(ref calls); return Task.FromResult(request); });
            using (NamedPipeServer server = await StartServerAsync(handler, new NamedPipeServerOptions()))
            using (NamedPipeClientStream client = await ConnectAsync(server.PipeName))
            {
                await NamedPipeFrameWriter.WriteControlAsync(client, NamedPipeFrameKind.Ping, CancellationToken.None);
                NamedPipeFrame response = await NamedPipeFrameReader.ReadAsync(client, 1024, CancellationToken.None);
                Assert.Equal(NamedPipeFrameKind.Pong, response.Kind);
                Assert.Equal(0, Volatile.Read(ref calls));
                await server.StopAsync();
            }
        }

        [Fact]
        public async Task HandlerFailureClosesOnlyOwningConnection()
        {
            var handler = new DelegateHandler((context, request, token) =>
            {
                if (request[0] == 0) throw new InvalidOperationException("handler failure");
                return Task.FromResult(request);
            });
            using (NamedPipeServer server = await StartServerAsync(handler, new NamedPipeServerOptions { MaxConnections = 2 }))
            using (NamedPipeClientStream failing = await ConnectAsync(server.PipeName))
            using (NamedPipeClientStream healthy = await ConnectAsync(server.PipeName))
            {
                await NamedPipeFrameWriter.WriteDataAsync(failing, new byte[] { 0 }, 1024, CancellationToken.None);
                await NamedPipeFrameWriter.WriteDataAsync(healthy, new byte[] { 7 }, 1024, CancellationToken.None);
                NamedPipeFrame response = await NamedPipeFrameReader.ReadAsync(healthy, 1024, CancellationToken.None);
                Assert.Equal(7, response.Payload[0]);
                await Assert.ThrowsAnyAsync<Exception>(() => NamedPipeFrameReader.ReadAsync(failing, 1024, CancellationToken.None));
                Assert.False(server.Completion.IsCompleted);
                await server.StopAsync();
            }
        }

        [Fact]
        public async Task ExcessConnectionIsClosedWhileExistingConnectionRemainsUsable()
        {
            var handler = new DelegateHandler((context, request, token) => Task.FromResult(request));
            using (NamedPipeServer server = await StartServerAsync(handler, new NamedPipeServerOptions { MaxConnections = 1 }))
            using (NamedPipeClientStream existing = await ConnectAsync(server.PipeName))
            using (NamedPipeClientStream excess = await ConnectAsync(server.PipeName))
            {
                await Assert.ThrowsAnyAsync<Exception>(() => NamedPipeFrameReader.ReadAsync(excess, 1024, CancellationToken.None));
                await NamedPipeFrameWriter.WriteDataAsync(existing, new byte[] { 9 }, 1024, CancellationToken.None);
                NamedPipeFrame response = await NamedPipeFrameReader.ReadAsync(existing, 1024, CancellationToken.None);
                Assert.Equal(9, response.Payload[0]);
                await server.StopAsync();
            }
        }

        private static async Task<NamedPipeServer> StartServerAsync(INamedPipeRequestHandler handler, NamedPipeServerOptions options)
        {
            var server = new NamedPipeServer("ii.rpa.ipc.server." + Guid.NewGuid().ToString("N"), handler, options);
            await server.StartAsync();
            Assert.True(server.Ready.IsCompleted);
            return server;
        }

        private static async Task<NamedPipeClientStream> ConnectAsync(string pipeName)
        {
            var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await client.ConnectAsync(5000);
            return client;
        }

        private static async Task WaitUntilAsync(Func<bool> condition)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(5);
            while (!condition())
            {
                if (DateTime.UtcNow >= deadline) throw new TimeoutException();
                await Task.Delay(10);
            }
        }

        private static void UpdateMaximum(ref int maximum, int value)
        {
            int observed;
            while (value > (observed = Volatile.Read(ref maximum)) && Interlocked.CompareExchange(ref maximum, value, observed) != observed) { }
        }

        private sealed class DelegateHandler : INamedPipeRequestHandler
        {
            private readonly Func<NamedPipeRequestContext, byte[], CancellationToken, Task<byte[]>> _handler;
            public DelegateHandler(Func<NamedPipeRequestContext, byte[], CancellationToken, Task<byte[]>> handler) { _handler = handler; }
            public Task<byte[]> HandleRequestAsync(NamedPipeRequestContext context, byte[] request, CancellationToken cancellationToken) => _handler(context, request, cancellationToken);
        }
    }
}
