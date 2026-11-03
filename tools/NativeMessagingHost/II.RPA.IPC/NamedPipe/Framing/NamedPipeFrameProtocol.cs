namespace II.RPA.IPC.NamedPipe.Framing
{
    public enum NamedPipeFrameKind { Data = 0, Ping = 1, Pong = 2 }

    public sealed class NamedPipeFrame
    {
        internal NamedPipeFrame(NamedPipeFrameKind kind, byte[] payload) { Kind = kind; Payload = payload; }
        public NamedPipeFrameKind Kind { get; }
        public byte[] Payload { get; }
    }

    internal static class NamedPipeFrameProtocol
    {
        internal const int HeaderLength = 4;
        internal const int ControlMarker = -1;
        internal const int PingCommand = 1;
        internal const int PongCommand = 2;
        internal const int DefaultMaxFrameLength = 1024 * 1024;
    }
}
