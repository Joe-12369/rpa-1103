using System;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.IPC.NamedPipe.Connections;
using II.RPA.IPC.NamedPipe.Framing;
using Xunit;

namespace II.RPA.IPC.Tests.Server
{
    public class NamedPipeServerConnectionTests
    {
        [Fact]
        public async Task ConcurrentSendsRemainCompleteFrames()
        {
            string pipeName = "ii.rpa.ipc.test." + Guid.NewGuid().ToString("N");
            using (var serverStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (var clientStream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                Task waitForConnection = serverStream.WaitForConnectionAsync();
                await clientStream.ConnectAsync(5000);
                await waitForConnection;
                using (var connection = new NamedPipeServerConnection(serverStream, 1024))
                {
                    byte[] first = Enumerable.Repeat((byte)0x11, 512).ToArray();
                    byte[] second = Enumerable.Repeat((byte)0x22, 512).ToArray();
                    Task sendFirst = connection.SendAsync(first, CancellationToken.None);
                    Task sendSecond = connection.SendAsync(second, CancellationToken.None);
                    NamedPipeFrame receivedFirst = await NamedPipeFrameReader.ReadAsync(clientStream, 1024, CancellationToken.None);
                    NamedPipeFrame receivedSecond = await NamedPipeFrameReader.ReadAsync(clientStream, 1024, CancellationToken.None);
                    await Task.WhenAll(sendFirst, sendSecond);

                    Assert.Contains(receivedFirst.Payload[0], new byte[] { 0x11, 0x22 });
                    Assert.Contains(receivedSecond.Payload[0], new byte[] { 0x11, 0x22 });
                    Assert.NotEqual(receivedFirst.Payload[0], receivedSecond.Payload[0]);
                    Assert.All(receivedFirst.Payload, value => Assert.Equal(receivedFirst.Payload[0], value));
                    Assert.All(receivedSecond.Payload, value => Assert.Equal(receivedSecond.Payload[0], value));
                }
            }
        }

        [Fact]
        public void ConcurrentConnectionsReceiveUniquePositiveIds()
        {
            var ids = new ConcurrentBag<long>();
            Parallel.For(0, 64, index =>
            {
                string pipeName = "ii.rpa.ipc.id." + Guid.NewGuid().ToString("N");
                using (var stream = new NamedPipeServerStream(pipeName))
                using (var connection = new NamedPipeServerConnection(stream, 1024))
                {
                    ids.Add(connection.ConnectionId);
                }
            });

            Assert.Equal(64, ids.Count);
            Assert.Equal(64, ids.Distinct().Count());
            Assert.All(ids, id => Assert.True(id > 0));
        }

        [Fact]
        public void CloseIsIdempotentAndCancelsConnectionToken()
        {
            string pipeName = "ii.rpa.ipc.close." + Guid.NewGuid().ToString("N");
            using (var stream = new NamedPipeServerStream(pipeName))
            using (var connection = new NamedPipeServerConnection(stream, 1024))
            {
                Assert.False(connection.ConnectionCancellation.IsCancellationRequested);
                connection.Close();
                connection.Close();
                Assert.True(connection.ConnectionCancellation.IsCancellationRequested);
            }
        }

        [Fact]
        public void CloseDisposesStreamWhenCancellationCallbackThrows()
        {
            string pipeName = "ii.rpa.ipc.close.throw." + Guid.NewGuid().ToString("N");
            using (var stream = new NamedPipeServerStream(pipeName))
            using (var connection = new NamedPipeServerConnection(stream, 1024))
            using (connection.ConnectionCancellation.Register(() => throw new InvalidOperationException("callback failure")))
            {
                Microsoft.Win32.SafeHandles.SafePipeHandle handle = stream.SafePipeHandle;
                Assert.Throws<AggregateException>(() => connection.Close());
                Assert.True(handle.IsClosed);
            }
        }
    }
}
