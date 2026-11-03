using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.IPC.NamedPipe.Framing
{
    public static class NamedPipeFrameWriter
    {
        public static async Task WriteDataAsync(Stream stream, byte[] payload, int maxFrameLength, CancellationToken cancellationToken)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            if (maxFrameLength <= 0) throw new ArgumentOutOfRangeException(nameof(maxFrameLength));
            if (payload.Length <= 0 || payload.Length > maxFrameLength) throw new InvalidDataException("Invalid named pipe frame length.");
            byte[] header = GetInt32LittleEndian(payload.Length);
            await stream.WriteAsync(header, 0, header.Length, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(payload, 0, payload.Length, cancellationToken).ConfigureAwait(false);
        }

        public static async Task WriteControlAsync(Stream stream, NamedPipeFrameKind kind, CancellationToken cancellationToken)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            int command;
            if (kind == NamedPipeFrameKind.Ping) command = NamedPipeFrameProtocol.PingCommand;
            else if (kind == NamedPipeFrameKind.Pong) command = NamedPipeFrameProtocol.PongCommand;
            else throw new ArgumentOutOfRangeException(nameof(kind), kind, "Only Ping and Pong are control frames.");
            byte[] marker = GetInt32LittleEndian(NamedPipeFrameProtocol.ControlMarker);
            byte[] commandBytes = GetInt32LittleEndian(command);
            await stream.WriteAsync(marker, 0, marker.Length, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(commandBytes, 0, commandBytes.Length, cancellationToken).ConfigureAwait(false);
        }

        internal static byte[] GetInt32LittleEndian(int value) => new[] { (byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24) };
    }
}
