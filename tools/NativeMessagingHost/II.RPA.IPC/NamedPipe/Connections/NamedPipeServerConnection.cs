using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.IPC.NamedPipe.Framing;

namespace II.RPA.IPC.NamedPipe.Connections
{
    public sealed class NamedPipeServerConnection : IDisposable
    {
        private static long s_nextConnectionId;
        private readonly PipeStream _stream;
        private readonly int _maxFrameLength;
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource _connectionCancellation = new CancellationTokenSource();
        private int _closed;

        public NamedPipeServerConnection(PipeStream stream, int maxFrameLength)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            if (maxFrameLength <= 0) throw new ArgumentOutOfRangeException(nameof(maxFrameLength));
            _maxFrameLength = maxFrameLength;
            ConnectionId = Interlocked.Increment(ref s_nextConnectionId);
        }

        public long ConnectionId { get; }
        public CancellationToken ConnectionCancellation => _connectionCancellation.Token;

        public Task<NamedPipeFrame> ReadAsync(CancellationToken cancellationToken)
        {
            ThrowIfClosed();
            return NamedPipeFrameReader.ReadAsync(_stream, _maxFrameLength, cancellationToken);
        }

        public async Task SendAsync(byte[] payload, CancellationToken cancellationToken)
        {
            ThrowIfClosed();
            using (var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _connectionCancellation.Token))
            {
                CancellationToken token = linkedCancellation.Token;
                await _writeLock.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    ThrowIfClosed();
                    await NamedPipeFrameWriter.WriteDataAsync(_stream, payload, _maxFrameLength, token).ConfigureAwait(false);
                }
                finally
                {
                    _writeLock.Release();
                }
            }
        }

        public async Task SendControlAsync(NamedPipeFrameKind kind, CancellationToken cancellationToken)
        {
            ThrowIfClosed();
            using (var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _connectionCancellation.Token))
            {
                CancellationToken token = linkedCancellation.Token;
                await _writeLock.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    ThrowIfClosed();
                    await NamedPipeFrameWriter.WriteControlAsync(_stream, kind, token).ConfigureAwait(false);
                }
                finally
                {
                    _writeLock.Release();
                }
            }
        }

        public void Close()
        {
            if (Interlocked.Exchange(ref _closed, 1) != 0) return;
            try
            {
                _connectionCancellation.Cancel();
            }
            finally
            {
                _stream.Dispose();
            }
        }

        public void Dispose()
        {
            Close();
        }

        private void ThrowIfClosed()
        {
            if (Volatile.Read(ref _closed) != 0)
            {
                throw new ObjectDisposedException(nameof(NamedPipeServerConnection));
            }
        }
    }
}
