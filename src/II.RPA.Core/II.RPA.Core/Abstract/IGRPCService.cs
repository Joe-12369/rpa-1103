using Grpc.Core;

namespace II.RPA.Core.Abstract
{
    public interface IGRPCService : IService
    {
        ServerServiceDefinition BindingToService();
    }
}
