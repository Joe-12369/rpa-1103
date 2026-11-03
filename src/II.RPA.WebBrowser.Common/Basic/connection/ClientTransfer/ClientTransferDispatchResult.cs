using System;

namespace II.RPA.WebBrowser.Common.Basic.connection.ClientTransfer
{
    internal sealed class ClientTransferDispatchResult
    {
        internal ClientTransferDispatchResult(
            ClientTransferCompletionReason reason,
            TransferMessage response = null,
            Exception exception = null)
        {
            Reason = reason;
            Response = response;
            Exception = exception;
        }

        public ClientTransferCompletionReason Reason { get; }

        public TransferMessage Response { get; }

        public Exception Exception { get; }
    }
}
