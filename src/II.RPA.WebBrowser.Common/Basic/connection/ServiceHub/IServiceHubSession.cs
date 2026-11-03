using System;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic.connection.ServiceHub
{
    internal interface IServiceHubSession : IDisposable
    {
        event Action<TransferMessage> MessageReceived;
        event Action<Exception> Disconnected;
        bool IsConnected { get; }
        void Connect(int timeout);
        Task<ServiceHubSendResult> SendAsync(TransferMessage message);
    }

    internal interface IServiceHubSessionFactory
    {
        IServiceHubSession Create(int attempt);
    }

    internal sealed class ServiceHubSendResult
    {
        internal ServiceHubSendResult(bool invocationStarted, Exception exception)
        {
            InvocationStarted = invocationStarted;
            Exception = exception;
        }

        public bool InvocationStarted { get; }
        public Exception Exception { get; }
    }
}
