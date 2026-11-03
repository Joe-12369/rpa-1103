namespace II.RPA.Communication.Exceptions;

public sealed class GRPCTimeoutException : TimeoutException
{
    public GRPCTimeoutException(string message)
        : base(message)
    {
    }

    public GRPCTimeoutException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
