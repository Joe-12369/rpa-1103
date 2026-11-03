namespace II.RPA.Communication.Exceptions;

public sealed class GRPCConfigurationException : InvalidOperationException
{
    public GRPCConfigurationException(string message)
        : base(message)
    {
    }
}
