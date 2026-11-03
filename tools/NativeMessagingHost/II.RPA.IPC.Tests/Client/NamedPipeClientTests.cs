using System;
using System.Collections.Concurrent;
using System.Diagnostics;
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

namespace II.RPA.IPC.Tests.Client
{
    public class NamedPipeClientTests
    {
        [Fact]
        public async Task BackgroundReadLoopDeliversBusinessFrames()
        {
            using (NamedPipeServer server = await StartEchoServerAsync())
            using (var client = new NamedPipeClient(server.PipeName, new NamedPipeClientOptions()))
            {
                var received = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
                client.MessageReceived += (sender, args) => received.TrySetResult(args.Payload);
                await client.ConnectAsync(CancellationToken.None);
                await client.SendAsync(new byte[] { 7, 8 }, CancellationToken.None);
                Assert.Equal(new byte[] { 7, 8 }, await WithTimeout(received.Task));
                await client.DisconnectAsync();
                await server.StopAsync();
            }
        }

        [Fact]
        public async Task ConcurrentSendsRemainCompleteFrames()
        {
            var requests = new ConcurrentBag<byte[]>();
            var handler = new DelegateHandler((context, request, token) => { requests.Add(request); return Task.FromResult(request); });
            using (NamedPipeServer server = await StartServerAsync(handler))
            using (var client = new NamedPipeClient(server.PipeName, new NamedPipeClientOptions()))
            {
                var received = new CountdownEvent(2);
                client.MessageReceived += (sender, args) => received.Signal();
                await client.ConnectAsync(CancellationToken.None);
                byte[] first = Enumerable.Repeat((byte)0x11, 512).ToArray();
                byte[] second = Enumerable.Repeat((byte)0x22, 512).ToArray();
                await Task.WhenAll(client.SendAsync(first, CancellationToken.None), client.SendAsync(second, CancellationToken.None));
                Assert.True(received.Wait(TimeSpan.FromSeconds(5)));
                Assert.Equal(2, requests.Count);
                Assert.All(requests, request => Assert.All(request, value => Assert.Equal(request[0], value)));
                await client.DisconnectAsync();
                await server.StopAsync();
            }
        }

        [Fact]
        public async Task ConcurrentPingsAreSerializedAndComplete()
        {
            using (NamedPipeServer server = await StartEchoServerAsync())
            using (var client = new NamedPipeClient(server.PipeName, new NamedPipeClientOptions()))
            {
                await client.ConnectAsync(CancellationToken.None);
                await Task.WhenAll(
                    client.PingAsync(TimeSpan.FromSeconds(2), CancellationToken.None),
                    client.PingAsync(TimeSpan.FromSeconds(2), CancellationToken.None));
                await client.DisconnectAsync();
                await server.StopAsync();
            }
        }

        [Fact]
        public async Task PingTimesOutWhenPeerDoesNotReturnPong()
        {
            string pipeName = "ii.rpa.ipc.client.ping." + Guid.NewGuid().ToString("N");
            using (var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (var client = new NamedPipeClient(pipeName, new NamedPipeClientOptions()))
            {
                Task accept = server.WaitForConnectionAsync();
                await client.ConnectAsync(CancellationToken.None);
                await accept;
                Task<NamedPipeFrame> consumePing = NamedPipeFrameReader.ReadAsync(server, 1024, CancellationToken.None);
                await Assert.ThrowsAsync<TimeoutException>(() => client.PingAsync(TimeSpan.FromMilliseconds(100), CancellationToken.None));
                Assert.Equal(NamedPipeFrameKind.Ping, (await consumePing).Kind);
                await client.DisconnectAsync();
            }
        }

        [Fact]
        public async Task DisconnectNotificationIsRaisedOnlyOnce()
        {
            using (NamedPipeServer server = await StartEchoServerAsync())
            using (var client = new NamedPipeClient(server.PipeName, new NamedPipeClientOptions()))
            {
                int notifications = 0;
                var disconnected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                client.Disconnected += (sender, args) => { if (Interlocked.Increment(ref notifications) == 1) disconnected.TrySetResult(true); };
                await client.ConnectAsync(CancellationToken.None);
                await server.StopAsync();
                await WithTimeout(disconnected.Task);
                await client.DisconnectAsync();
                await client.DisconnectAsync();
                Assert.Equal(1, Volatile.Read(ref notifications));
            }
        }

        [Fact]
        public async Task ConnectHonorsConfiguredTimeout()
        {
            string missingPipe = "ii.rpa.ipc.client.missing." + Guid.NewGuid().ToString("N");
            using (var client = new NamedPipeClient(missingPipe, new NamedPipeClientOptions { ConnectTimeout = TimeSpan.FromMilliseconds(100) }))
            {
                var stopwatch = Stopwatch.StartNew();
                await Assert.ThrowsAnyAsync<TimeoutException>(() => client.ConnectAsync(CancellationToken.None));
                Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(3));
            }
        }

        private static Task<NamedPipeServer> StartEchoServerAsync() => StartServerAsync(new DelegateHandler((context, request, token) => Task.FromResult(request)));

        private static async Task<NamedPipeServer> StartServerAsync(INamedPipeRequestHandler handler)
        {
            var server = new NamedPipeServer("ii.rpa.ipc.client.server." + Guid.NewGuid().ToString("N"), handler);
            await server.StartAsync();
            return server;
        }

        private static async Task<T> WithTimeout<T>(Task<T> task)
        {
            Task completed = await Task.WhenAny(task, Task.Delay(5000));
            if (!ReferenceEquals(completed, task)) throw new TimeoutException();
            return await task;
        }

        private sealed class DelegateHandler : INamedPipeRequestHandler
        {
            private readonly Func<NamedPipeRequestContext, byte[], CancellationToken, Task<byte[]>> _handler;
            public DelegateHandler(Func<NamedPipeRequestContext, byte[], CancellationToken, Task<byte[]>> handler) { _handler = handler; }
            public Task<byte[]> HandleRequestAsync(NamedPipeRequestContext context, byte[] request, CancellationToken cancellationToken) => _handler(context, request, cancellationToken);
        }
    }
}
