namespace II.RPA.NativeMessageingStartup
{
    internal enum TransferCompletionReason
    {
        NotCompleted = 0,
        Response = 1,
        CrxTimeout = 2,
        ConnectionClosed = 3,
        HostStopping = 4
    }
}
