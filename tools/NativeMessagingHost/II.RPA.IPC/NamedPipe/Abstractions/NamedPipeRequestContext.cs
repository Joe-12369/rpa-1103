using System.Threading;

namespace II.RPA.IPC.NamedPipe.Abstractions
{
    public sealed class NamedPipeRequestContext
    {
        internal NamedPipeRequestContext(long connectionId, CancellationToken connectionCancellation)
        {
            ConnectionId = connectionId;
            ConnectionCancellation = connectionCancellation;
        }

        public long ConnectionId { get; }
        public CancellationToken ConnectionCancellation { get; }
    }
}
