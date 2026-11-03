using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.NamedPipeWrapper
{
    public class PipeStreamWrapper<TReadWrite> : PipeStreamWrapper<TReadWrite, TReadWrite> where TReadWrite : class
    {
        public PipeStreamWrapper(PipeStream stream) : base(stream)
        {
        }
    }
    public class PipeStreamWrapper<TRead, TWrite>
    where TRead : class
    where TWrite : class
    {
        public PipeStreamWrapper(PipeStream stream)
        {
            BaseStream = stream;
            _reader = new PipeStreamReader<TRead>(BaseStream);
            _writer = new PipeStreamWriter<TWrite>(BaseStream);
        }
        public PipeStream BaseStream { get; private set; }
        public bool IsConnected
        {
            get { return BaseStream.IsConnected && _reader.IsConnected; }
        }
        public bool CanRead
        {
            get { return BaseStream.CanRead; }
        }
        public bool CanWrite
        {
            get { return BaseStream.CanWrite; }
        }
        private readonly PipeStreamReader<TRead> _reader;
        private readonly PipeStreamWriter<TWrite> _writer;

        public TRead NewReadObject()
        {
            return _reader.NewReadObject();
        }
        public void NewWriteObject(TWrite obj)
        {
            _writer.NewWriteObject(obj);
        }
        public TRead ReadObject()
        {
            return _reader.ReadObject();
        }

        public void WriteObject(TWrite obj)
        {
            _writer.WriteObject(obj);
        }
        public void WaitForPipeDrain()
        {
            _writer.WaitForPipeDrain();
        }
        public void Close()
        {
            BaseStream.Close();
        }
    }
}
