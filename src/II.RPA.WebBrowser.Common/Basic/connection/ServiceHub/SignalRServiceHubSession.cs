using Microsoft.AspNet.SignalR.Client;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.Plugin.Core;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic.connection.ServiceHub
{
    internal sealed class SignalRServiceHubSessionFactory : IServiceHubSessionFactory
    {
        private readonly Func<int, string> _url;
        private readonly Func<SignalRClientType> _clientType;

        public SignalRServiceHubSessionFactory(Func<int, string> url, Func<SignalRClientType> clientType)
        {
            _url = url;
            _clientType = clientType;
        }

        public IServiceHubSession Create(int attempt) => new SignalRServiceHubSession(_url(attempt), _clientType());
    }

    internal sealed class SignalRServiceHubSession : IServiceHubSession
    {
        private readonly HubConnection _connection;
        private readonly IHubProxy _proxy;
        private IDisposable _subscription;

        public SignalRServiceHubSession(string url, SignalRClientType clientType)
        {
            _connection = new HubConnection(url, new Dictionary<string, string>
            {
                ["ClientFlag"] = "RPA",
                ["ClientType"] = ((int)clientType).ToString()
            });
            _connection.DeadlockErrorTimeout = TimeSpan.FromSeconds(GlobleConfigurations.DeadlockErrorTimeout);
            _connection.TransportConnectTimeout = TimeSpan.FromMilliseconds(GlobleConfigurations.TransportConnectTimeout);
            _proxy = _connection.CreateHubProxy("TransferHub");
            _connection.Error += OnError;
            _connection.StateChanged += change =>
            {
                if (change.NewState == ConnectionState.Disconnected)
                    Disconnected?.Invoke(new InvalidOperationException("The ServiceHub session disconnected."));
            };
            _subscription = _proxy.On<TransferMessage>("newTransferMessage", message => MessageReceived?.Invoke(message));
        }

        public event Action<TransferMessage> MessageReceived;
        public event Action<Exception> Disconnected;
        public bool IsConnected => _connection.State == ConnectionState.Connected;
        public void Connect(int timeout)
        {
            if (!_connection.Start().Wait(Math.Max(1, timeout)) || !IsConnected)
                throw new TimeoutException("ServiceHub did not become ready within the connection timeout.");
        }
        public async Task<ServiceHubSendResult> SendAsync(TransferMessage message)
        {
            Task invocation;
            try
            {
                invocation = _proxy.Invoke("SendTransferMessage", message.ToJson());
            }
            catch (Exception error)
            {
                return new ServiceHubSendResult(false, error);
            }

            try
            {
                await invocation.ConfigureAwait(false);
                return new ServiceHubSendResult(true, null);
            }
            catch (Exception error)
            {
                return new ServiceHubSendResult(true, error);
            }
        }
        private void OnError(Exception error) => Disconnected?.Invoke(error);
        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;
            _connection.Dispose();
        }
    }
}
