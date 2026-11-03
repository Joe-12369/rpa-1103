using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace II.RPA.WebBrowser.Common.Basic.connection.ClientTransfer
{
    internal sealed class ClientTransferRequestDispatcher : IDisposable
    {
        private readonly ConcurrentDictionary<string, ClientTransferEntry> _pending =
            new ConcurrentDictionary<string, ClientTransferEntry>(StringComparer.Ordinal);
        private readonly object _closeGate = new object();
        private ClientTransferDispatchResult _closeResult;
        private int _isClosed;

        public int PendingCount => _pending.Count;

        public bool IsClosed => Volatile.Read(ref _isClosed) != 0;

        public ClientTransferEntry Register(string messageId, int remainingTimeout)
        {
            if (string.IsNullOrWhiteSpace(messageId) || !Guid.TryParse(messageId, out _))
            {
                return ClientTransferEntry.CreateCompleted(
                    messageId,
                    new ClientTransferDispatchResult(ClientTransferCompletionReason.ProtocolError));
            }

            lock (_closeGate)
            {
                if (IsClosed)
                {
                    return ClientTransferEntry.CreateCompleted(messageId, _closeResult);
                }

                var entry = new ClientTransferEntry(messageId, remainingTimeout, RemoveCompletedEntry);
                if (!_pending.TryAdd(messageId, entry))
                {
                    entry.Dispose();
                    return ClientTransferEntry.CreateCompleted(
                        messageId,
                        new ClientTransferDispatchResult(ClientTransferCompletionReason.MessageIdConflict));
                }

                return entry;
            }
        }

        public bool TryComplete(TransferMessage response)
        {
            if (response == null || string.IsNullOrWhiteSpace(response.MessageId))
            {
                return false;
            }

            if (!_pending.TryGetValue(response.MessageId, out ClientTransferEntry entry))
            {
                return false;
            }

            return entry.TryComplete(
                new ClientTransferDispatchResult(ClientTransferCompletionReason.Response, response));
        }

        public void CompleteAll(
            ClientTransferCompletionReason reason,
            Exception exception = null)
        {
            var result = new ClientTransferDispatchResult(reason, exception: exception);
            lock (_closeGate)
            {
                if (IsClosed)
                {
                    return;
                }

                _closeResult = result;
                Volatile.Write(ref _isClosed, 1);
            }

            foreach (KeyValuePair<string, ClientTransferEntry> pair in _pending)
            {
                pair.Value.TryComplete(result);
            }
        }

        public void Dispose()
        {
            CompleteAll(ClientTransferCompletionReason.Stopped);
        }

        private void RemoveCompletedEntry(ClientTransferEntry entry)
        {
            RemoveEntry(entry.MessageId, entry);
        }

        private bool RemoveEntry(string messageId, ClientTransferEntry entry)
        {
            return ((ICollection<KeyValuePair<string, ClientTransferEntry>>)_pending)
                .Remove(new KeyValuePair<string, ClientTransferEntry>(messageId, entry));
        }
    }
}
