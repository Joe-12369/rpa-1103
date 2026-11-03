using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Threading;

namespace II.RPA.NamedPipeWrapper
{
    public class NamedPipeConnection<TRead, TWrite>
        where TRead : class
        where TWrite : class
    {
        public readonly int Id;
        public string Name { get; }
        public bool IsConnected { get { return _streamWrapper.IsConnected; } }

        public event ConnectionEventHandler<TRead, TWrite> Disconnected;

        public event ConnectionMessageEventHandler<TRead, TWrite> ReceiveMessage;

        public event ConnectionExceptionEventHandler<TRead, TWrite> Error;
        private readonly PipeStreamWrapper<TRead, TWrite> _streamWrapper;
        private readonly AutoResetEvent _writeSignal = new AutoResetEvent(false);
        private readonly BlockingCollection<TWrite> _writeQueue = new BlockingCollection<TWrite>();
        private bool _notifiedSucceeded;
        CancellationTokenSource tokenSource = new CancellationTokenSource();
        private static ILogger logger = Logger.OfContext();

        internal NamedPipeConnection(int id, string name, PipeStream serverStream)
        {
            Id = id;
            Name = name;
            _streamWrapper = new PipeStreamWrapper<TRead, TWrite>(serverStream);
        }

        public void NewOpen()
        {
            var readWorker = new Worker();
            readWorker.Succeeded += ReadWorker_Succeeded;
            readWorker.Error += ReadWorker_Error;
            readWorker.DoWork(NewReadPipe);

            var writeWorker = new Worker();
            writeWorker.Succeeded += ReadWorker_Succeeded;
            writeWorker.Error += ReadWorker_Error;
            writeWorker.DoWork(NewWritePipe);
        }

        private void NewWritePipe()
        {
            while (IsConnected && _streamWrapper.CanWrite)
            {
                try
                {
                    var dequeue = _writeQueue.Take(tokenSource.Token);
                    _streamWrapper.NewWriteObject(dequeue);
                    _streamWrapper.WaitForPipeDrain();
                    dequeue = null;

                }
                catch (OperationCanceledException ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "Write error，cancellation token canceled");
                }
                catch (Exception e)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "Write error");
                    throw e;
                }
            }
        }

        private void NewReadPipe()
        {
            while (IsConnected && _streamWrapper.CanRead)
            {
                try
                {
                    var obj = _streamWrapper.NewReadObject();
                    if (obj == null)
                    {
                        CloseImpl();
                        return;
                    }
                    if (ReceiveMessage != null)
                        ReceiveMessage(this, obj);
                }
                catch (Exception e)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "Read error");
                    throw e;
                }
            }
        }

        public void Open()
        {
            var readWorker = new Worker();
            readWorker.Succeeded += ReadWorker_Succeeded;
            readWorker.Error += ReadWorker_Error;
            readWorker.DoWork(ReadPipe);

            var writeWorker = new Worker();
            writeWorker.Succeeded += ReadWorker_Succeeded;
            writeWorker.Error += ReadWorker_Error;
            writeWorker.DoWork(WritePipe);
        }

        public void PushMessage(TWrite message)
        {
            _writeQueue.Add(message);
            _writeSignal.Set();
        }

        public void Close()
        {
            CloseImpl();
        }

        private void CloseImpl()
        {
            _streamWrapper?.Close();
            _writeSignal.Set();
            tokenSource.Cancel();
        }

        private void ReadPipe()
        {
            while (IsConnected && _streamWrapper.CanRead)
            {
                try
                {
                    var obj = _streamWrapper.ReadObject();
                    if (obj == null)
                    {
                        CloseImpl();
                        return;
                    }
                    if (ReceiveMessage != null)
                        ReceiveMessage(this, obj);
                }
                catch
                {
                }
            }
        }

        private void WritePipe()
        {
            while (IsConnected && _streamWrapper.CanWrite)
            {
                try
                {
                    _streamWrapper.WriteObject(_writeQueue.Take());
                    _streamWrapper.WaitForPipeDrain();
                }
                catch
                {
                }
            }
        }

        private void ReadWorker_Error(Exception exception)
        {
            if (Error != null)
                Error(this, exception);
        }

        private void ReadWorker_Succeeded()
        {
            if (_notifiedSucceeded)
                return;

            _notifiedSucceeded = true;

            if (Disconnected != null)
                Disconnected(this);
        }
    }

    static class ConnectionFactory
    {
        private static int _lastId;
        public static NamedPipeConnection<TRead, TWrite> CreateConnection<TRead, TWrite>(PipeStream pipeStream)
            where TRead : class
            where TWrite : class
        {
            return new NamedPipeConnection<TRead, TWrite>(++_lastId, "Client" + _lastId, pipeStream);
        }
    }

    public delegate void ConnectionEventHandler<TRead, TWrite>(NamedPipeConnection<TRead, TWrite> connection)
        where TRead : class
        where TWrite : class;

    public delegate void ConnectionMessageEventHandler<TRead, TWrite>(NamedPipeConnection<TRead, TWrite> connection, TRead message)
        where TRead : class
        where TWrite : class;

    public delegate void ConnectionExceptionEventHandler<TRead, TWrite>(NamedPipeConnection<TRead, TWrite> connection, Exception exception)
       where TRead : class
       where TWrite : class;
}

