using Grpc.Core;
using System.Threading.Tasks;

namespace II.RPA.Core.GRPCService.Service
{
    public class SimpleGRPCService : Simple.Grpc.SimpleService.SimpleServiceBase,IGRPCService
    {
        public SimpleGRPCService()
        {
        }

        public ServerServiceDefinition BindingToService()
        {
            return Simple.Grpc.SimpleService.BindService(this);
        }

        public override Task<Simple.Grpc.SimpleResponse> SimpleUnary(Simple.Grpc.SimpleRequest request, ServerCallContext context)
        {
            return Task.FromResult(new Simple.Grpc.SimpleResponse 
            { 
                Message = request.Message,
            });
        }

        public object GetNew()
        {
            return new 
            {
                Haha = 1,
                Lala = "2",
                Yaya = new 
                {
                    Nana = true
                }
            };
        }
    }
}
