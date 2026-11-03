namespace II.RPA.WebBrowser.Common.Basic.connection.ClientTransfer
{
    internal enum ClientTransferCompletionReason
    {
        Response,
        Timeout,
        ConnectionClosed,
        SendFailed,
        Stopped,
        MessageIdConflict,
        ProtocolError
    }
}
