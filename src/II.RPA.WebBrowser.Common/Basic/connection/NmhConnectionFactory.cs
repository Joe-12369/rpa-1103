using II.RPA.WebBrowser.Common.Basic.connection.ClientTransfer;
using System;

namespace II.RPA.WebBrowser.Common.Basic.connection
{
    internal interface IBrowserConnectionActivator
    {
        IBrowserConnectionNew Create(Type connectionType, string endpoint);
    }

    internal sealed class BrowserConnectionActivator : IBrowserConnectionActivator
    {
        public IBrowserConnectionNew Create(Type connectionType, string endpoint)
        {
            if (connectionType == null)
            {
                throw new InvalidOperationException("Browser connection type is not configured.");
            }

            object instance = Activator.CreateInstance(connectionType, endpoint);
            if (!(instance is IBrowserConnectionNew connection))
            {
                throw new InvalidOperationException(
                    $"Connection type '{connectionType.FullName}' does not implement IBrowserConnectionNew.");
            }

            return connection;
        }
    }

    internal sealed class NmhConnectionFactory
    {
        private readonly INmhConnectionCapabilityResolver _capabilityResolver;
        private readonly IBrowserConnectionActivator _connectionActivator;

        internal NmhConnectionFactory()
            : this(new NmhConnectionCapabilityResolver(), new BrowserConnectionActivator())
        {
        }

        internal NmhConnectionFactory(
            INmhConnectionCapabilityResolver capabilityResolver,
            IBrowserConnectionActivator connectionActivator)
        {
            _capabilityResolver = capabilityResolver
                ?? throw new ArgumentNullException(nameof(capabilityResolver));
            _connectionActivator = connectionActivator
                ?? throw new ArgumentNullException(nameof(connectionActivator));
        }

        internal IBrowserConnectionNew GetConnection(ConnectionConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            NmhConnectionCapability capability =
                _capabilityResolver.Resolve(config.NativeMessageHostProcessID);
            if (capability == null || capability.Protocol == NmhConnectionProtocol.Failure)
            {
                return FailOrNull(
                    config,
                    "Unable to resolve the NMH communication protocol.",
                    ClientTransferCompletionReason.ProtocolError,
                    capability?.Error);
            }

            Type connectionType;
            string endpoint;
            switch (capability.Protocol)
            {
                case NmhConnectionProtocol.NamedPipe:
                    connectionType = config.NamePipeConnectionType;
                    endpoint = config.NamedPipeServerName;
                    break;
                case NmhConnectionProtocol.WcfIpc:
                    connectionType = config.ServiceConnectionType;
                    endpoint = config.IPCServiceAddress;
                    break;
                default:
                    return FailOrNull(
                        config,
                        "Unsupported NMH communication protocol.",
                        ClientTransferCompletionReason.ProtocolError);
            }

            IBrowserConnectionNew connection = null;
            try
            {
                connection = _connectionActivator.Create(connectionType, endpoint);
                connection.ConnectTimeout = config.ConnectTimeout;
                connection.Start();
                if (!connection.IsConnected)
                {
                    throw new BrowserConnectionException(
                        "The NMH connection did not become ready.",
                        ClientTransferCompletionReason.ConnectionClosed);
                }

                return connection;
            }
            catch (Exception ex)
            {
                SafeStop(connection);
                if (ex is BrowserConnectionException browserConnectionException)
                {
                    return FailOrNull(
                        config,
                        browserConnectionException.Message,
                        browserConnectionException.Reason,
                        browserConnectionException.InnerException);
                }

                return FailOrNull(
                    config,
                    "Failed to establish the NMH connection.",
                    ClientTransferCompletionReason.ConnectionClosed,
                    ex);
            }
        }

        private static IBrowserConnectionNew FailOrNull(
            ConnectionConfig config,
            string message,
            ClientTransferCompletionReason reason,
            Exception innerException = null)
        {
            if (config.IgnoreConnection)
            {
                return null;
            }

            throw new BrowserConnectionException(message, reason, innerException);
        }

        private static void SafeStop(IBrowserConnectionNew connection)
        {
            try
            {
                connection?.Stop();
            }
            catch
            {
            }
        }
    }
}
