using II.RPA.IPC.NamedPipe;
using II.RPA.IPC.NamedPipe.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic.connection.NamedPipe
{
    internal sealed class NamedPipeClientAdapterFactory : INamedPipeClientAdapterFactory
    {
        public INamedPipeClientAdapter Create(string pipeName, TimeSpan connectTimeout)
        {
            return new NamedPipeClientAdapter(
                new NamedPipeClient(
                    pipeName,
                    new NamedPipeClientOptions { ConnectTimeout = connectTimeout }));
        }
    }

    internal sealed class NamedPipeClientAdapter : INamedPipeClientAdapter
    {
        private readonly NamedPipeClient _client;
        private int _isConnected;

        internal NamedPipeClientAdapter(NamedPipeClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _client.MessageReceived += ClientMessageReceived;
            _client.Disconnected += ClientDisconnected;
        }

        public event Action<byte[]> MessageReceived;

        public event Action Disconnected;

        public bool IsConnected => Volatile.Read(ref _isConnected) != 0;

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            Volatile.Write(ref _isConnected, 1);
            try
            {
                await _client.ConnectAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                Volatile.Write(ref _isConnected, 0);
                throw;
            }
        }

        public Task SendAsync(byte[] payload, CancellationToken cancellationToken)
        {
            return _client.SendAsync(payload, cancellationToken);
        }

        public Task PingAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return _client.PingAsync(timeout, cancellationToken);
        }

        public async Task DisconnectAsync()
        {
            await _client.DisconnectAsync().ConfigureAwait(false);
            Volatile.Write(ref _isConnected, 0);
        }

        public void Dispose()
        {
            _client.MessageReceived -= ClientMessageReceived;
            _client.Disconnected -= ClientDisconnected;
            Volatile.Write(ref _isConnected, 0);
            _client.Dispose();
        }

        private void ClientMessageReceived(object sender, NamedPipeMessageReceivedEventArgs args)
        {
            MessageReceived?.Invoke(args.Payload);
        }

        private void ClientDisconnected(object sender, EventArgs args)
        {
            Volatile.Write(ref _isConnected, 0);
            Disconnected?.Invoke();
        }
    }
}
