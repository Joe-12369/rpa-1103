using II.RPA.WebBrowser.Common.Basic.connection.ClientTransfer;
using II.RPA.Logging;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;

namespace II.RPA.WebBrowser.Common.Basic.connection
{
    internal sealed class BrowserConnectionException : Exception
    {
        private static readonly ILogger logger = Logger.OfContext();

        internal BrowserConnectionException(
            string message,
            ClientTransferCompletionReason reason,
            Exception innerException = null)
            : base(message, innerException)
        {
            Reason = reason;
        }

        public ClientTransferCompletionReason Reason { get; }

        internal RPAException ToRpaException(eGuiType guiType)
        {
            Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger,
                this,
                "Browser connection failed. Reason={Reason}; Message={Message}",
                Reason,
                base.Message);

            return new RPAException(
                guiType,
                Reason == ClientTransferCompletionReason.Timeout
                    ? eErrorMessageType.Error_TransferMsg_Timeout
                    : eErrorMessageType.Error_PipeNameError,
                eComponentType.None,
                this);
        }
    }
}
