using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.NamedPipeWrapper
{
    public class PipeStreamReader<T> where T : class
    {
        public PipeStream BaseStream { get; private set; }
        /// <summary>
        /// Pipe 是否连接的标识位
        /// </summary>
        public bool IsConnected { get; private set; }

        public PipeStreamReader(PipeStream stream)
        {
            BaseStream = stream;
            IsConnected = stream.IsConnected;
        }
        public T NewReadObject()
        {
            var binaryReader = new BinaryReader(BaseStream);
            int count = binaryReader.ReadInt32();
            byte[] readBytes = binaryReader.ReadBytes(count);
            return JsonHelper.JsonDesByNewtonJson<T>(Encoding.UTF8.GetString(readBytes));
        }
        /// <summary>
        /// 读取消息长度
        /// </summary>
        /// <returns></returns>
        private int ReadLength()
        {
            int lensize = sizeof(int);
            var lenbuf = new byte[lensize];
            var bytesRead = BaseStream.Read(lenbuf, 0, lensize);
            if (bytesRead == 0)
            {
                IsConnected = false;
                return 0;
            }
            if (bytesRead != lensize)
                throw new IOException(string.Format("Expected {0} bytes but read {1}", lensize, bytesRead));
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lenbuf, 0));
        }
        private T ReadObject(int len)
        {
            var data = new byte[len];
            BaseStream.ReadExactly(data, 0, len);
            string json = Encoding.UTF8.GetString(data, 0, len);
            return JsonConvert.DeserializeObject<T>(json);
        }

        public const int BUFFER_SIZE = 1024;
        public static byte[] Decompress2(byte[] data)
        {
            MemoryStream input = new MemoryStream(data);
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
            {
                dstream.CopyTo(output);
            }
            return output.ToArray();
        }
        public static byte[] Decompress(byte[] inputData)
        {
            if (inputData == null)
                throw new ArgumentNullException("inputData must be non-null");
            using (MemoryStream compressedMs = new MemoryStream(inputData))
            {
                using (MemoryStream decompressedMs = new MemoryStream())
                {
                    using (BufferedStream gzs = new BufferedStream(new GZipStream(compressedMs, CompressionMode.Decompress), BUFFER_SIZE))
                    {
                        gzs.CopyTo(decompressedMs);
                    }
                    return decompressedMs.ToArray();
                }
            }
        }

        public T ReadObject()
        {
            var len = ReadLength();
            return len == 0 ? default(T) : ReadObject(len);
        }

    }
}
