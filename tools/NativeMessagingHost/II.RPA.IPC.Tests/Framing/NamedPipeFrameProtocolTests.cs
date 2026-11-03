using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.IPC.NamedPipe;
using II.RPA.IPC.NamedPipe.Framing;
using Xunit;

namespace II.RPA.IPC.Tests.Framing
{
    public class NamedPipeFrameProtocolTests
    {
        [Fact]
        public async Task TwoBytePayloadRemainsABusinessFrame()
        {
            var stream = new FragmentedReadStream(DataFrame(0xAA, 0xBB), 1);
            NamedPipeFrame frame = await NamedPipeFrameReader.ReadAsync(stream, 1024, CancellationToken.None);
            Assert.Equal(NamedPipeFrameKind.Data, frame.Kind);
            Assert.Equal(new byte[] { 0xAA, 0xBB }, frame.Payload);
        }

        [Theory]
        [InlineData(1, NamedPipeFrameKind.Ping)]
        [InlineData(2, NamedPipeFrameKind.Pong)]
        public async Task RecognizedControlCommandIsDecoded(int command, NamedPipeFrameKind expectedKind)
        {
            var stream = new FragmentedReadStream(ControlFrame(command), 1);
            NamedPipeFrame frame = await NamedPipeFrameReader.ReadAsync(stream, 1024, CancellationToken.None);
            Assert.Equal(expectedKind, frame.Kind);
            Assert.Null(frame.Payload);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-2)]
        public async Task InvalidLengthClosesTheProtocol(int length)
        {
            var stream = new MemoryStream(Int32Bytes(length));
            await Assert.ThrowsAsync<InvalidDataException>(() => NamedPipeFrameReader.ReadAsync(stream, 1024, CancellationToken.None));
        }

        [Fact]
        public async Task PayloadLargerThanConfiguredMaximumIsRejectedBeforeAllocation()
        {
            var stream = new MemoryStream(Int32Bytes(1025));
            await Assert.ThrowsAsync<InvalidDataException>(() => NamedPipeFrameReader.ReadAsync(stream, 1024, CancellationToken.None));
        }

        [Fact]
        public async Task UnknownControlCommandIsRejected()
        {
            var stream = new MemoryStream(ControlFrame(3));
            await Assert.ThrowsAsync<InvalidDataException>(() => NamedPipeFrameReader.ReadAsync(stream, 1024, CancellationToken.None));
        }

        [Fact]
        public async Task TruncatedPayloadIsRejected()
        {
            var stream = new FragmentedReadStream(Concat(Int32Bytes(3), new byte[] { 1, 2 }), 1);
            await Assert.ThrowsAsync<EndOfStreamException>(() => NamedPipeFrameReader.ReadAsync(stream, 1024, CancellationToken.None));
        }

        [Fact]
        public async Task WritersUseLittleEndianWireFormat()
        {
            var stream = new MemoryStream();
            await NamedPipeFrameWriter.WriteDataAsync(stream, new byte[] { 0xAA, 0xBB }, 1024, CancellationToken.None);
            await NamedPipeFrameWriter.WriteControlAsync(stream, NamedPipeFrameKind.Ping, CancellationToken.None);
            Assert.Equal(Concat(DataFrame(0xAA, 0xBB), ControlFrame(1)), stream.ToArray());
        }

        [Fact]
        public void OptionsExposeValidatedTransportDefaults()
        {
            var server = new NamedPipeServerOptions();
            var client = new NamedPipeClientOptions();
            Assert.Equal(4, server.MaxConnections);
            Assert.Equal(8, server.MaxPendingRequestsPerConnection);
            Assert.Equal(32, server.MaxPendingRequests);
            Assert.Equal(1024 * 1024, server.MaxFrameLength);
            Assert.Equal(1024 * 1024, client.MaxFrameLength);
            Assert.True(client.ConnectTimeout > TimeSpan.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeServerOptions { MaxConnections = 0 }.Validate());
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeClientOptions { MaxFrameLength = 0 }.Validate());
        }

        private static byte[] DataFrame(params byte[] payload) => Concat(Int32Bytes(payload.Length), payload);
        private static byte[] ControlFrame(int command) => Concat(Int32Bytes(-1), Int32Bytes(command));

        private static byte[] Int32Bytes(int value)
        {
            return new[] { (byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24) };
        }

        private static byte[] Concat(byte[] first, byte[] second)
        {
            var result = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, result, 0, first.Length);
            Buffer.BlockCopy(second, 0, result, first.Length, second.Length);
            return result;
        }

        private sealed class FragmentedReadStream : MemoryStream
        {
            private readonly int _maximumReadLength;
            public FragmentedReadStream(byte[] buffer, int maximumReadLength) : base(buffer) => _maximumReadLength = maximumReadLength;
            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return base.ReadAsync(buffer, offset, Math.Min(count, _maximumReadLength), cancellationToken);
            }
        }
    }
}
