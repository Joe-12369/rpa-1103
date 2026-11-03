using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.NativeMessagingHostLp
{
	internal class NamedPipeServer : IDisposable
	{
		private readonly NamedPipeServerStream _stream;

		private bool disposedValue;

		public Stream BaseStream => _stream;

		public NamedPipeServer()
		{
			int processID = Process.GetCurrentProcess().Id;
			string pipeName = $"aa3f7b1a-c221-4d19-a646-77cd6c58f815";
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
