using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.NativeMessageingStartup
{
    internal sealed class TransferRequestDispatcher
    {
        private readonly ConcurrentDictionary<string, TransferEntry> _entries =
            new ConcurrentDictionary<string, TransferEntry>();
        private readonly Func<TransferMessage, Task> _sendToCrxAsync;

        internal TransferRequestDispatcher(Func<TransferMessage, Task> sendToCrxAsync)
        {
            _sendToCrxAsync = sendToCrxAsync ?? throw new ArgumentNullException(nameof(sendToCrxAsync));
        }

        internal async Task<TransferDispatchResult> DispatchAsync(
            TransferMessage message,
            CancellationToken connectionCancellation)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            using (var entry = new TransferEntry(message, connectionCancellation))
            {
                if (entry.CompletionReason != TransferCompletionReason.NotCompleted)
                {
                    return new TransferDispatchResult(
                        entry.CompletionReason == TransferCompletionReason.CrxTimeout
                            ? TransferDispatchStatus.Expired
                            : TransferDispatchStatus.Completed,
                        null,
                        entry.CompletionReason);
                }

                if (!_entries.TryAdd(entry.MessageId, entry))
                {
                    return new TransferDispatchResult(
                        TransferDispatchStatus.MessageIdConflict,
                        null,
                        TransferCompletionReason.NotCompleted);
                }

                try
                {
                    if (entry.CompletionReason != TransferCompletionReason.NotCompleted)
                    {
                        return new TransferDispatchResult(
                            TransferDispatchStatus.Completed,
                            null,
                            entry.CompletionReason);
                    }

                    await _sendToCrxAsync(message).ConfigureAwait(false);
                    TransferMessage response = await entry.WaitAsync().ConfigureAwait(false);
                    return new TransferDispatchResult(
                        TransferDispatchStatus.Completed,
                        response,
                        entry.CompletionReason);
                }
                finally
                {
                    _entries.TryRemove(entry.MessageId, out _);
                }
            }
        }

        internal bool TryCompleteResponse(TransferMessage response)
        {
            if (response == null || string.IsNullOrEmpty(response.MessageId))
            {
                return false;
            }

            return _entries.TryGetValue(response.MessageId, out var entry)
                && entry.TryComplete(response, TransferCompletionReason.Response);
        }
    }
}
