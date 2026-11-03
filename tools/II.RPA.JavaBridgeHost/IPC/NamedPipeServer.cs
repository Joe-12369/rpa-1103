using System;
using System.IO;
using System.IO.Pipes;

namespace II.RPA.JavaBridgeHost.IPC
{
    internal class NamedPipeServer : IDisposable
    {
        private readonly NamedPipeServerStream _stream;

        private bool disposedValue;

        public NamedPipeServer(string pipeName)
        {
            _stream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, -1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        }

        public Stream BaseStream => _stream;

        public void Dispose()
        {
            Dispose(disposing: true);
        }

        public void WaitForConnection()
        {
            _stream.WaitForConnection();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        _stream.Disconnect();
                    }
                    catch { }
                    _stream.Dispose();
                }
                disposedValue = true;
            }
        }
    }
}