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
    public class PipeStreamWriter<T> where T : class
    {
        public PipeStream BaseStream { get; private set; }
        public PipeStreamWriter(PipeStream stream)
        {
            BaseStream = stream;
        }
        public void NewWriteObject(T obj)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            var binaryWriter = new BinaryWriter(BaseStream);
            byte[] contentBytes = Encoding.UTF8.GetBytes(json);
            binaryWriter.Write(contentBytes.Length);
            binaryWriter.Write(contentBytes);
            binaryWriter.Flush();
        }
        private void WriteLength(int len)
        {
            var lenbuf = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(len));
            BaseStream.Write(lenbuf, 0, lenbuf.Length);
        }
        private void WriteObject(byte[] data)
        {
            BaseStream.Write(data, 0, data.Length);
        }
        private void Flush()
        {
            BaseStream.Flush();
        }
        /// <summary>
        /// Writes an object to the pipe.  This method blocks until all data is sent.
        /// </summary>
        /// <param name="obj">Object to write to the pipe</param>
        /// <exception cref="SerializationException">An object in the graph of type parameter <typeparamref name="T"/> is not marked as serializable.</exception>
        public void WriteObject(T obj)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            var data = System.Text.Encoding.UTF8.GetBytes(json);
            WriteLength(data.Length);
            WriteObject(data);
            Flush();
        }
        public const int BUFFER_SIZE = 1024;
        public static byte[] Compress2(byte[] data)
        {
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(output, CompressionMode.Compress))
            {
                dstream.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }
        public static byte[] Compress(byte[] inputData)
        {
            if (inputData == null)
                throw new ArgumentNullException("inputData must be non-null");

            using (var compressIntoMs = new MemoryStream())
            {
                using (var gzs = new BufferedStream(new GZipStream(compressIntoMs,
                 CompressionMode.Compress), BUFFER_SIZE))
                {
                    gzs.Write(inputData, 0, inputData.Length);
                }
                return compressIntoMs.ToArray();
            }
        }
        /// <summary>
        ///     Waits for the other end of the pipe to read all sent bytes.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The pipe is closed.</exception>
        /// <exception cref="NotSupportedException">The pipe does not support write operations.</exception>
        /// <exception cref="IOException">The pipe is broken or another I/O error occurred.</exception>
        public void WaitForPipeDrain()
        {
            BaseStream.WaitForPipeDrain();
        }
    }
}
