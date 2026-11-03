using System;

namespace II.RPA.IPC.NamedPipe
{
    public sealed class NamedPipeClientOptions
    {
        public int MaxFrameLength { get; set; } = Framing.NamedPipeFrameProtocol.DefaultMaxFrameLength;
        public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

        public NamedPipeClientOptions Validate()
        {
            if (MaxFrameLength <= 0) throw new ArgumentOutOfRangeException(nameof(MaxFrameLength), MaxFrameLength, "The value must be positive.");
            if (ConnectTimeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(ConnectTimeout), ConnectTimeout, "The value must be positive.");
            return this;
        }

        internal NamedPipeClientOptions CreateValidatedSnapshot()
        {
            Validate();
            return new NamedPipeClientOptions { MaxFrameLength = MaxFrameLength, ConnectTimeout = ConnectTimeout };
        }
    }
}
