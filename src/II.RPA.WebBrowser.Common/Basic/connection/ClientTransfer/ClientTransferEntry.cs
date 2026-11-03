using System;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic.connection.ClientTransfer
{
    internal sealed class ClientTransferEntry : IDisposable
    {
        private readonly Action<ClientTransferEntry> _completed;
        private readonly TaskCompletionSource<ClientTransferDispatchResult> _completionSource;
        private Timer _timeoutTimer;
        private int _isCompleted;

        internal ClientTransferEntry(
            string messageId,
            int remainingTimeout,
            Action<ClientTransferEntry> completed)
            : this(messageId, remainingTimeout, completed, true)
        {
        }

        private ClientTransferEntry(
            string messageId,
            int remainingTimeout,
            Action<ClientTransferEntry> completed,
            bool scheduleTimeout)
        {
            MessageId = messageId;
            _completed = completed;
            _completionSource = new TaskCompletionSource<ClientTransferDispatchResult>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            if (scheduleTimeout)
            {
                _timeoutTimer = new Timer(
                    OnTimeout,
                    null,
                    Math.Max(0, remainingTimeout),
                    Timeout.Infinite);
            }
        }

        public string MessageId { get; }

        public Task<ClientTransferDispatchResult> Completion => _completionSource.Task;

        internal static ClientTransferEntry CreateCompleted(
            string messageId,
            ClientTransferDispatchResult result)
        {
            var entry = new ClientTransferEntry(messageId, Timeout.Infinite, null, false);
            entry.TryComplete(result);
            return entry;
        }

        public bool TryComplete(ClientTransferDispatchResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (Interlocked.CompareExchange(ref _isCompleted, 1, 0) != 0)
            {
                return false;
            }

            Interlocked.Exchange(ref _timeoutTimer, null)?.Dispose();
            _completed?.Invoke(this);
            _completionSource.TrySetResult(result);
            return true;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _timeoutTimer, null)?.Dispose();
        }

        private void OnTimeout(object state)
        {
            TryComplete(new ClientTransferDispatchResult(ClientTransferCompletionReason.Timeout));
        }
    }
}
