using II.RPA.Logging;
using II.RPA.Logging.Formatting;
using Microsoft.Extensions.Logging;
using System;
using System.IO.Pipes;
using System.Threading;

namespace II.RPA.NamedPipeWrapper
{
    public class NamedPipeClient<TReadWrite> : NamedPipeClient<TReadWrite, TReadWrite> where TReadWrite : class
    {
        public NamedPipeClient(string pipeName, string serverName = ".") : base(pipeName, serverName)
        {
        }
    }

    public class NamedPipeClient<TRead, TWrite>
        where TRead : class
        where TWrite : class
    {
        public bool AutoReconnect { get; set; }
        public event ConnectionMessageEventHandler<TRead, TWrite> ServerMessage;
        public event ConnectionEventHandler<TRead, TWrite> Disconnected;
        public event ConnectionEventHandler<TRead, TWrite> Connected;
        public event PipeExceptionEventHandler Error;
        private readonly string _pipeName;
        private NamedPipeConnection<TRead, TWrite> _connection;
        private static ILogger logger = Logger.OfContext();

        public bool isConnected
        {
            get
            {
                if (_connection == null)
                    return false;
                return _connection.IsConnected;
            }
        }

        private readonly AutoResetEvent _connected = new AutoResetEvent(false);
        private readonly AutoResetEvent _disconnected = new AutoResetEvent(false);
        private volatile bool _closedExplicitly;
        /// <summary>
        /// the server name, which client will connect to.
        /// </summary>
        private string _serverName { get; set; }

        /// <summary>
        /// Constructs a new <c>NamedPipeClient</c> to connect to the NamedPipeServer{TRead, TWrite} specified by <paramref name="pipeName"/>.
        /// </summary>
        /// <param name="pipeName">Name of the server's pipe</param>
        /// <param name="serverName">the Name of the server, default is  local machine</param>
        public NamedPipeClient(string pipeName, string serverName)
        {
            _pipeName = pipeName;
            _serverName = serverName;
            AutoReconnect = true;
        }

        public void NewStart()
        {
            _closedExplicitly = false;
            var worker = new Worker();
            worker.Error += OnError;
            worker.DoWork(NewListenSync);
        }

        private void NewListenSync()
        {
            try
            {
                logger.LogInformation("PipeClientFactory.Connect:" + _pipeName + " " + _serverName);

                // Connect to the actual data pipe
                var dataPipe = PipeClientFactory.CreateAndConnectPipe(_pipeName, _serverName);
                logger.LogDebug(LogBlock.NullCheck(dataPipe, "dataPipe"));

                // Create a Connection object for the data pipe
                _connection = ConnectionFactory.CreateConnection<TRead, TWrite>(dataPipe);
                logger.LogDebug(LogBlock.NullCheck(_connection, "_connection"));

                Connected?.Invoke(_connection);
                //_connection.Disconnected += OnNewDisconnected;
                _connection.ReceiveMessage += OnReceiveMessage;
                _connection.Error += ConnectionOnError;
                _connection.NewOpen();
                logger.LogInformation("_connection open");
                _connected.Set();
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "NamedPipeClientError");
                System.Diagnostics.Trace.WriteLine(ex.ToString(), "Error");
                throw;
            }
        }

        private void OnNewDisconnected(NamedPipeConnection<TRead, TWrite> connection)
        {
            Disconnected?.Invoke(connection);
            _disconnected.Set();
            if (AutoReconnect && !_closedExplicitly)
                NewStart();
        }

        /// <summary>
        /// Connects to the named pipe server asynchronously.
        /// This method returns immediately, possibly before the connection has been established.
        /// </summary>
        public void Start()
        {
            _closedExplicitly = false;
            var worker = new Worker();
            worker.Error += OnError;
            worker.DoWork(ListenSync);
        }

        /// <summary>
        ///     Sends a message to the server over a named pipe.
        /// </summary>
        /// <param name="message">Message to send to the server.</param>
        public void PushMessage(TWrite message)
        {
            if (_connection != null)
            {
                _connection.PushMessage(message);
            }
            else
            {
                logger.LogError("Publish message requested before connection connected");
            }
        }

        /// <summary>
        /// Closes the named pipe.
        /// </summary>
        public void Stop()
        {
            _closedExplicitly = true;
            if (_connection != null)
            {
                _connection.Disconnected -= OnNewDisconnected;
                _connection.ReceiveMessage -= OnReceiveMessage;
                _connection.Error -= ConnectionOnError;
                _connection.Close();

            }
        }

        #region Wait for connection/disconnection

        public void WaitForConnection()
        {
            _connected.WaitOne();
        }

        public void WaitForConnection(int millisecondsTimeout)
        {
            _connected.WaitOne(millisecondsTimeout);
        }

        public void WaitForConnection(TimeSpan timeout)
        {
            _connected.WaitOne(timeout);
        }

        public void WaitForDisconnection()
        {
            _disconnected.WaitOne();
        }

        public void WaitForDisconnection(int millisecondsTimeout)
        {
            _disconnected.WaitOne(millisecondsTimeout);
        }

        public void WaitForDisconnection(TimeSpan timeout)
        {
            _disconnected.WaitOne(timeout);
        }

        #endregion

        private void ListenSync()
        {
            try
            {
                // Get the name of the data pipe that should be used from now on by this NamedPipeClient
                var handshake = PipeClientFactory.Connect<string, string>(_pipeName, _serverName);
                var dataPipeName = handshake.ReadObject();
                handshake.Close();
                logger.LogInformation("PipeClientFactory.Connect:" + _pipeName + " " + _serverName);

                // Connect to the actual data pipe
                var dataPipe = PipeClientFactory.CreateAndConnectPipe(dataPipeName, _serverName);
                logger.LogDebug(LogBlock.NullCheck(dataPipe, "dataPipe"));

                // Create a Connection object for the data pipe
                _connection = ConnectionFactory.CreateConnection<TRead, TWrite>(dataPipe);
                logger.LogDebug(LogBlock.NullCheck(_connection, "_connection"));

                Connected?.Invoke(_connection);
                _connection.Disconnected += OnDisconnected;
                _connection.ReceiveMessage += OnReceiveMessage;
                _connection.Error += ConnectionOnError;
                _connection.Open();
                logger.LogInformation("_connection Open");
                _connected.Set();
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "NamedPipeClientError");
                System.Diagnostics.Trace.WriteLine(ex.ToString(), "Error");
                throw;
            }
        }

        private void OnDisconnected(NamedPipeConnection<TRead, TWrite> connection)
        {
            Disconnected?.Invoke(connection);

            _disconnected.Set();

            // Reconnect
            if (AutoReconnect && !_closedExplicitly)
                Start();
        }

        private void OnReceiveMessage(NamedPipeConnection<TRead, TWrite> connection, TRead message)
        {
            ServerMessage?.Invoke(connection, message);
        }

        /// <summary>
        ///     Invoked on the UI thread.
        /// </summary>
        private void ConnectionOnError(NamedPipeConnection<TRead, TWrite> connection, Exception exception)
        {
            OnError(exception);
        }

        /// <summary>
        ///     Invoked on the UI thread.
        /// </summary>
        /// <param name="exception"></param>
        private void OnError(Exception exception)
        {
            Error?.Invoke(exception);
        }
    }

    static class PipeClientFactory
    {
        private static ILogger logger = Logger.OfContext();

        public static PipeStreamWrapper<TRead, TWrite> Connect<TRead, TWrite>(string pipeName, string serverName)
           where TRead : class
           where TWrite : class
        {
            return new PipeStreamWrapper<TRead, TWrite>(CreateAndConnectPipe(pipeName, serverName));
        }

        public static NamedPipeClientStream CreateAndConnectPipe(string pipeName, string serverName, int timeOut = 30000)
        {
            try
            {
                var pipe = CreatePipe(pipeName, serverName);
                logger.LogInformation("NamedPipeClientStream CreateAndConnectPipe:" + pipeName + "ServerName:" + serverName);
                pipe.Connect(timeOut);
                logger.LogInformation("NamedPipeClientStream Connect Succeed");
                return pipe;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "管道连接异常");
                throw ex;
            }
        }

        private static NamedPipeClientStream CreatePipe(string pipeName, string serverName)
        {
            return new NamedPipeClientStream(serverName, pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
        }
    }
}

