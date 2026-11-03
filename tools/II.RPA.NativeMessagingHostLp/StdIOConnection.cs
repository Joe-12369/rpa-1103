using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.NativeMessagingHostLp
{
	internal class StdIOConnection : IDisposable
	{
		private readonly ManualResetEvent _event;

		private readonly MessageReader _reader;

		private readonly MessageWriter _writer;

		private string _response;

		private bool disposedValue;

		public event EventHandler Closed;

		public StdIOConnection(Stream stdin, Stream stdout)
		{
			_event = new ManualResetEvent(initialState: false);
			_reader = new MessageReader(stdin);
			_writer = new MessageWriter(stdout);
			ThreadPool.QueueUserWorkItem(BeginRead);
		}

		public string Send(string message, int timeout)
		{
			_response = null;
			_event.Reset();
			_writer.WriteMessage(message);
			_event.WaitOne(timeout);
			return _response;
		}

		private void BeginRead(object state)
		{
			try
			{
				while (true)
				{
					_response = _reader.ReadMessage();
					_event.Set();
				}
			}
			catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException || ex is EndOfStreamException)
			{
				this.Closed?.Invoke(this, EventArgs.Empty);
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					_reader.Dispose();
					_writer.Dispose();
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
