using System;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic.connection.NamedPipe
{
    internal interface INamedPipeClientAdapter : IDisposable
    {
        event Action<byte[]> MessageReceived;

        event Action Disconnected;

        bool IsConnected { get; }

        Task ConnectAsync(CancellationToken cancellationToken);

        Task SendAsync(byte[] payload, CancellationToken cancellationToken);

        Task PingAsync(TimeSpan timeout, CancellationToken cancellationToken);

        Task DisconnectAsync();
    }

    internal interface INamedPipeClientAdapterFactory
    {
        INamedPipeClientAdapter Create(string pipeName, TimeSpan connectTimeout);
    }
}
