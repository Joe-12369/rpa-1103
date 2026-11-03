using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.NamedPipeWrapper
{
    public delegate void PipeExceptionEventHandler(Exception exception);
    public class NamedPipeServer<TReadWrite>:Server<TReadWrite,TReadWrite> where TReadWrite:class
    {
        public NamedPipeServer(string pipeName)
           : base(pipeName, null)
        {
        }
        public NamedPipeServer(string pipeName, PipeSecurity pipeSecurity)
            : base(pipeName, pipeSecurity)
        {
        }
    }
    public class Server<TRead, TWrite>
       where TRead : class
       where TWrite : class
    {
        public event ConnectionEventHandler<TRead, TWrite> ClientConnected;
        public event ConnectionEventHandler<TRead, TWrite> ClientDisconnected;
        public event ConnectionMessageEventHandler<TRead, TWrite> ClientMessage;
        public event PipeExceptionEventHandler Error;
        private readonly string _pipeName;
        private readonly PipeSecurity _pipeSecurity;
        private readonly List<NamedPipeConnection<TRead, TWrite>> _connections = new List<NamedPipeConnection<TRead, TWrite>>();
        public int connectionCount
        {
            get { return _connections.Count; }
        }
        private int _nextPipeId;
        private volatile bool _shouldKeepRunning;
        public Server(string pipeName, PipeSecurity pipeSecurity)
        {
            _pipeName = pipeName;
            _pipeSecurity = pipeSecurity;
        }
        public void Start()
        {
            _shouldKeepRunning = true;
            var worker = new Worker();
            worker.Error += OnError;
            worker.DoWork(ListenSync);
        }
        public void PushMessage(TWrite message)
        {
            lock (_connections)
            {
                foreach (var client in _connections)
                {
                    client.PushMessage(message);
                }
            }
        }
        public void PushMessage(TWrite message, string clientName)
        {
            lock (_connections)
            {
                foreach (var client in _connections)
                {
                    if (client.Name == clientName)
                        client.PushMessage(message);
                }
            }
        }
        public void Stop()
        {
            _shouldKeepRunning = false;

            lock (_connections)
            {
                foreach (var client in _connections.ToArray())
                {
                    client.Close();
                }
            }

            // If background thread is still listening for a client to connect,
            // initiate a dummy connection that will allow the thread to exit.
            //dummy connection will use the local server name.
            var dummyClient = new NamedPipeClient<TRead, TWrite>(_pipeName, ".");
            dummyClient.Start();
            dummyClient.WaitForConnection(TimeSpan.FromSeconds(2));
            dummyClient.Stop();
            dummyClient.WaitForDisconnection(TimeSpan.FromSeconds(2));
        }
        private void ListenSync()
        {
            //_isRunning = true;
            while (_shouldKeepRunning)
            {
                WaitForConnection(_pipeName, _pipeSecurity);
            }
            //_isRunning = false;
        }
        private void WaitForConnection(string pipeName, PipeSecurity pipeSecurity)
        {
            NamedPipeServerStream handshakePipe = null;
            NamedPipeServerStream dataPipe = null;
            NamedPipeConnection<TRead, TWrite> connection = null;

            var connectionPipeName = GetNextConnectionPipeName(pipeName);

            try
            {
                // Send the client the name of the data pipe to use
                handshakePipe = PipeServerFactory.CreateAndConnectPipe(pipeName, pipeSecurity);
                var handshakeWrapper = new PipeStreamWrapper<string, string>(handshakePipe);
                handshakeWrapper.WriteObject(connectionPipeName);
                handshakeWrapper.WaitForPipeDrain();
                handshakeWrapper.Close();

                // Wait for the client to connect to the data pipe
                dataPipe = PipeServerFactory.CreatePipe(connectionPipeName, pipeSecurity);
                dataPipe.WaitForConnection();

                // Add the client's connection to the list of connections
                connection = ConnectionFactory.CreateConnection<TRead, TWrite>(dataPipe);
                connection.ReceiveMessage += ClientOnReceiveMessage;
                connection.Disconnected += ClientOnDisconnected;
                connection.Error += ConnectionOnError;
                connection.Open();

                lock (_connections)
                {
                    _connections.Add(connection);
                }

                ClientOnConnected(connection);
            }
            // Catch the IOException that is raised if the pipe is broken or disconnected.
            catch (Exception e)
            {
                Console.Error.WriteLine("Named pipe is broken or disconnected: {0}", e);

                Cleanup(handshakePipe);
                Cleanup(dataPipe);

                ClientOnDisconnected(connection);
            }
        }

        private void ClientOnConnected(NamedPipeConnection<TRead, TWrite> connection)
        {
            if (ClientConnected != null)
                ClientConnected(connection);
        }

        private void ClientOnReceiveMessage(NamedPipeConnection<TRead, TWrite> connection, TRead message)
        {
            if (ClientMessage != null)
                ClientMessage(connection, message);
        }

        private void ClientOnDisconnected(NamedPipeConnection<TRead, TWrite> connection)
        {
            if (connection == null)
                return;

            lock (_connections)
            {
                _connections.Remove(connection);
            }

            if (ClientDisconnected != null)
                ClientDisconnected(connection);
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
            if (Error != null)
                Error(exception);
        }

        private string GetNextConnectionPipeName(string pipeName)
        {
            return string.Format("{0}_{1}", pipeName, ++_nextPipeId);
        }

        private static void Cleanup(NamedPipeServerStream pipe)
        {
            if (pipe == null) return;
            using (var x = pipe)
            {
                x.Close();
            }
        }
    }
    static class PipeServerFactory
    {
        public static NamedPipeServerStream CreateAndConnectPipe(string pipeName, PipeSecurity pipeSecurity)
        {
            var pipe = CreatePipe(pipeName, pipeSecurity);
            pipe.WaitForConnection();
            return pipe;
        }

        public static NamedPipeServerStream CreatePipe(string pipeName, PipeSecurity pipeSecurity)
        {
            return NamedPipeServerStreamAcl.Create(pipeName, PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.WriteThrough, 0, 0, pipeSecurity);
        }
    }
}
