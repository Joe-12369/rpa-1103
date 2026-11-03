using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;

namespace II.RPA.NativeMessagingHost360
{

    internal class NamedPipeServer : IDisposable
    {
        private readonly NamedPipeServerStream _stream;

        private bool disposedValue;

        public Stream BaseStream => _stream;

        public NamedPipeServer()
        {
            int processID = Process.GetCurrentProcess().Id;
            string pipeName = $"a1773d62a609dd09e98ea1aebeddbd949b78bf83_{processID}";
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
