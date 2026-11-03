using System;
using System.IO;
using System.Text;

namespace II.RPA.JavaBridgeHost.IPC
{
    internal class MessageWriter : IDisposable
	{
		private readonly BinaryWriter _writer;

		private bool disposedValue;

		public MessageWriter(Stream stream)
		{
			_writer = new BinaryWriter(stream);
		}

		public void WriteMessage(string message)
		{
			byte[] contentBytes = Encoding.UTF8.GetBytes(message);
			_writer.Write(contentBytes.Length);
			_writer.Write(contentBytes);
			_writer.Flush();
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					_writer.Close();
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
