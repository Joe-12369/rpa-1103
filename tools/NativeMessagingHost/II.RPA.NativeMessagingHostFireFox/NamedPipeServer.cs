using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;

namespace II.RPA.NativeMessagingHostFireFox
{

    internal class NamedPipeServer : IDisposable
    {
        private readonly NamedPipeServerStream _stream;

        private bool disposedValue;

        public Stream BaseStream => _stream;

        public NamedPipeServer()
        {
            int processID = Process.GetCurrentProcess().Id;
            string pipeName = $"66819f50ea4d7ec659d7013416a623f1fd8ca6da_{processID}";
            _stream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, -1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
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
                    _stream.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
        }
    }

}
