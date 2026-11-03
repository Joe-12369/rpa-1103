using System;

namespace II.RPA.IPC.NamedPipe
{
    public sealed class NamedPipeServerOptions
    {
        public int MaxConnections { get; set; } = 4;
        public int MaxPendingRequestsPerConnection { get; set; } = 8;
        public int MaxPendingRequests { get; set; } = 32;
        public int MaxFrameLength { get; set; } = Framing.NamedPipeFrameProtocol.DefaultMaxFrameLength;

        public NamedPipeServerOptions Validate()
        {
            ValidatePositive(MaxConnections, nameof(MaxConnections));
            ValidatePositive(MaxPendingRequestsPerConnection, nameof(MaxPendingRequestsPerConnection));
            ValidatePositive(MaxPendingRequests, nameof(MaxPendingRequests));
            ValidatePositive(MaxFrameLength, nameof(MaxFrameLength));
            return this;
        }

        internal NamedPipeServerOptions CreateValidatedSnapshot()
        {
            Validate();
            return new NamedPipeServerOptions { MaxConnections = MaxConnections, MaxPendingRequestsPerConnection = MaxPendingRequestsPerConnection, MaxPendingRequests = MaxPendingRequests, MaxFrameLength = MaxFrameLength };
        }

        private static void ValidatePositive(int value, string parameterName)
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(parameterName, value, "The value must be positive.");
        }
    }
}
