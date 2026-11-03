
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.NativeMessageingStartup
{
    public class TransferEntry : IDisposable
    {
        private static ILogger logger = Logger.OfContext();

        private readonly TaskCompletionSource<TransferMessage> _tcs;
        private readonly CancellationTokenSource _timeoutCancellation;
        private readonly CancellationTokenRegistration _timeoutRegistration;
        private readonly CancellationTokenRegistration _connectionRegistration;
        private int _completionReason;
        private int _disposed;

        internal TransferMessage SendTransferMessage { get; private set; }
        public string MessageId => SendTransferMessage.MessageId;
        public DateTime CreateTime => DateTime.Now;
        /// <summary>
        /// 消息超时时间
        /// </summary>
        public int Timeout => SendTransferMessage.Timeout;
        public long Timestamp { get; private set; }
        public long ElapsedMilliseconds => (long)((Stopwatch.GetTimestamp() - Timestamp) * 1000D / Stopwatch.Frequency);
        internal static TransferEntry NewTransferEntry(TransferMessage tMsg) => new TransferEntry(tMsg);
        public TransferEntry(TransferMessage tMsg)
            : this(tMsg, CancellationToken.None)
        {
        }

        internal TransferEntry(TransferMessage tMsg, CancellationToken connectionCancellation)
        {
            if (tMsg == null)
            {
                throw new ArgumentNullException(nameof(tMsg));
            }

            _tcs = new TaskCompletionSource<TransferMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            SendTransferMessage = tMsg;
            Timestamp = tMsg.Timestamp == 0
                                  ? Stopwatch.GetTimestamp()
                                  : tMsg.Timestamp;

            long timeout = Timeout - ElapsedMilliseconds;

            if (timeout <= 0)
            {
                TryComplete(null, TransferCompletionReason.CrxTimeout);
                return;
            }

            if (connectionCancellation.CanBeCanceled)
            {
                _connectionRegistration = connectionCancellation.Register(
                    () => TryComplete(null, TransferCompletionReason.ConnectionClosed),
                    useSynchronizationContext: false);
            }

            if (CompletionReason == TransferCompletionReason.NotCompleted)
            {
                _timeoutCancellation = new CancellationTokenSource((int)timeout);
                _timeoutRegistration = _timeoutCancellation.Token.Register(
                    () => TryComplete(null, TransferCompletionReason.CrxTimeout),
                    useSynchronizationContext: false);
            }
        }

        internal TransferCompletionReason CompletionReason =>
            (TransferCompletionReason)Volatile.Read(ref _completionReason);

        public void Complete(TransferMessage result)
        {
            TryComplete(
                result,
                result == null
                    ? TransferCompletionReason.CrxTimeout
                    : TransferCompletionReason.Response);
        }

        internal bool TryComplete(TransferMessage result, TransferCompletionReason reason)
        {
            if (reason == TransferCompletionReason.NotCompleted)
            {
                throw new ArgumentOutOfRangeException(nameof(reason));
            }

            if (Interlocked.CompareExchange(
                ref _completionReason,
                (int)reason,
                (int)TransferCompletionReason.NotCompleted) != (int)TransferCompletionReason.NotCompleted)
            {
                return false;
            }

            _tcs.TrySetResult(result);
            return true;
        }

        public Task<TransferMessage> WaitAsync() => _tcs.Task;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _connectionRegistration.Dispose();
            _timeoutRegistration.Dispose();
            _timeoutCancellation?.Dispose();
        }

    }
}
