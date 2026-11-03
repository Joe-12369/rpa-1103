using II.RPA.Core.GRPCService.ServiceBoot;
using II.RPA.Core.Server.Attributes;

namespace II.RPA.Core.Server.Models
{
    public enum ServiceBootTypeEnum
    {
        [ServiceBootType(typeof(GRPCServiceBoot))]
        GRPC,
        [ServiceBootType("II.RPA.Core.GRPCService.ServiceBoot.PowerGRPCServiceBoot", "II.RPA.Core.GRPCService")]
        PowerGRPC,
        [ServiceBootType("II.RPA.Core.RestService.ServiceBoot.RestServiceBoot", "II.RPA.Core.RestService")]
        Rest,
        [ServiceBootType("II.RPA.Core.WebSocketService.ServiceBoot.WebSocketServiceBoot", "II.RPA.Core.WebSocketService")]
        WebSocket,
        [ServiceBootType("II.RPA.Core.NamedPipeService.ServiceBoot.NamedPipeServiceBoot", "II.RPA.Core.NamedPipeService")]
        NamedPipe,
        [ServiceBootType("II.RPA.Core.SocketService.ServiceBoot.SocketerviceBoot", "II.RPA.Core.SocketService")]
        Socket,
    }
}
