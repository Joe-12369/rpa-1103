using System;
using System.IO;
using System.Text;

namespace II.RPA.NativeMessagingHostFireFox
{
    internal class MessageReader : IDisposable
	{
		private readonly BinaryReader _reader;

		private bool disposedValue;

		public MessageReader(Stream stream)
		{
			_reader = new BinaryReader(stream);
		}

		public string ReadMessage()
		{
			int count = _reader.ReadInt32();
			byte[] contentBytes = _reader.ReadBytes(count);
			return Encoding.UTF8.GetString(contentBytes);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					_reader.Close();
					_reader.Dispose();
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
