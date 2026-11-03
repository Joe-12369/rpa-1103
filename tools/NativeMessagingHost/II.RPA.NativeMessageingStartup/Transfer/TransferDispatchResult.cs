namespace II.RPA.NativeMessageingStartup
{
    internal enum TransferDispatchStatus
    {
        Completed = 0,
        Expired = 1,
        MessageIdConflict = 2
    }

    internal sealed class TransferDispatchResult
    {
        internal TransferDispatchResult(
            TransferDispatchStatus status,
            TransferMessage response,
            TransferCompletionReason completionReason)
        {
            Status = status;
            Response = response;
            CompletionReason = completionReason;
        }

        internal TransferDispatchStatus Status { get; }
        internal TransferMessage Response { get; }
        internal TransferCompletionReason CompletionReason { get; }
    }
}
