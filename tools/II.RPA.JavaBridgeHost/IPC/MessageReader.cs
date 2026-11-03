using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace II.RPA.JavaBridgeHost.IPC
{
    internal class MessageReader : IDisposable
    {
        private readonly Stream input;

        private bool disposedValue;

        public MessageReader(Stream stream)
        {
            input = stream;
        }

        public T ReadMessage<T>()
        {
            string ret = this.ReadMessage();
            return JsonConvert.DeserializeObject<T>(ret);
        }

        public string ReadMessage()
        {
            byte[] readLenBytes = new byte[4];

            while (input.Read(readLenBytes, 0, 4) > 0)
            {
                int num = readLenBytes[0] | (readLenBytes[1] << 8) | (readLenBytes[2] << 16) | (readLenBytes[3] << 24);
                byte[] content = new byte[num];
                int i;
                int num2;
                for (i = 0; i < num; i += num2)
                {
                    num2 = input.Read(content, i, num - i);
                    if (num2 == 0)
                    {
                        break;
                    }
                }
                if (i < num)
                {
                    new IOException($"The length of stream is {num} bytes,but the stream can read only {i} bytes.");
                    break;
                }
                return Encoding.UTF8.GetString(content);
            }

            throw new IOException("Cannot read the length of stream!");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    input.Close();
                    input.Dispose();
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
