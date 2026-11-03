using Grpc.Core;

namespace II.RPA.Communication.Exceptions;

public static class GRPCExceptionHelper
{
    public static bool IsTimeout(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception is GRPCTimeoutException
            || exception is TimeoutException
            || exception is RpcException { StatusCode: StatusCode.DeadlineExceeded };
    }
}
