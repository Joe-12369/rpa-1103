using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.IPC.NamedPipe.Framing
{
    public static class NamedPipeFrameReader
    {
        public static async Task<NamedPipeFrame> ReadAsync(Stream stream, int maxFrameLength, CancellationToken cancellationToken)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (maxFrameLength <= 0) throw new ArgumentOutOfRangeException(nameof(maxFrameLength));
            var header = new byte[NamedPipeFrameProtocol.HeaderLength];
            await ReadExactlyAsync(stream, header, cancellationToken).ConfigureAwait(false);
            int length = ReadInt32LittleEndian(header);
            if (length == NamedPipeFrameProtocol.ControlMarker)
            {
                await ReadExactlyAsync(stream, header, cancellationToken).ConfigureAwait(false);
                int command = ReadInt32LittleEndian(header);
                if (command == NamedPipeFrameProtocol.PingCommand) return new NamedPipeFrame(NamedPipeFrameKind.Ping, null);
                if (command == NamedPipeFrameProtocol.PongCommand) return new NamedPipeFrame(NamedPipeFrameKind.Pong, null);
                throw new InvalidDataException("Unknown named pipe control command.");
            }
            if (length <= 0 || length > maxFrameLength) throw new InvalidDataException("Invalid named pipe frame length.");
            var payload = new byte[length];
            await ReadExactlyAsync(stream, payload, cancellationToken).ConfigureAwait(false);
            return new NamedPipeFrame(NamedPipeFrameKind.Data, payload);
        }

        private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
        {
            int offset = 0;
            while (offset < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer, offset, buffer.Length - offset, cancellationToken).ConfigureAwait(false);
                if (read == 0) throw new EndOfStreamException("The named pipe frame ended before all bytes were read.");
                offset += read;
            }
        }

        internal static int ReadInt32LittleEndian(byte[] buffer) => buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24);
    }
}
