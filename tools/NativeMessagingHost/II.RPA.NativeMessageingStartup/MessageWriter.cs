using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.NativeMessageingStartup
{
    internal class MessageWriter : IDisposable
    {
        private readonly BinaryWriter _writer;
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private bool _disposedValue;

        // Chrome Native Messaging 协议限制：最大 1MB 消息
        public const int MaxMessageSize = 1024 * 1024;

        public MessageWriter(Stream stream)
        {
            _writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: false);
        }

        public void WriteMessage(string message)
        {
            byte[] contentBytes = GetContentBytes(message);

            _writeLock.Wait();
            try
            {
                WriteFrame(contentBytes);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public async Task WriteMessageAsync(string message)
        {
            byte[] contentBytes = GetContentBytes(message);

            await _writeLock.WaitAsync().ConfigureAwait(false);
            try
            {
                WriteFrame(contentBytes);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        private static byte[] GetContentBytes(string message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            byte[] contentBytes = Encoding.UTF8.GetBytes(message);

            if (contentBytes.Length > MaxMessageSize)
            {
                throw new ArgumentException(
                    $"Message size ({contentBytes.Length} bytes) exceeds Chrome's Native Messaging limit of {MaxMessageSize} bytes.",
                    nameof(message));
            }

            return contentBytes;
        }

        private void WriteFrame(byte[] contentBytes)
        {
            _writer.Write(contentBytes.Length);
            _writer.Write(contentBytes);
            _writer.Flush();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _writer?.Dispose();
                    _writeLock?.Dispose();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
