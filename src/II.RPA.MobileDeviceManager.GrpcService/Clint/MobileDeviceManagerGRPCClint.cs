using Grpc.Core;
using II.RPA.Communication.Endpoints;
using II.RPA.Communication.Hosting;
using II.RPA.Communication.Options;
using Rpa.Core.Mobile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileDeviceManager.GrpcService.Clint
{
    public class MobileDeviceManagerGRPCClint
    {
        public MobileDeviceManagerService.MobileDeviceManagerServiceClient MobileDeviceManagerServiceClient;
        private readonly GrpcClient _client;
        public MobileDeviceManagerGRPCClint(string sPort)
        {
            _client = new GrpcClient(new ClientOptions
            {
                Endpoint = new TcpEndpoint("127.0.0.1", int.Parse(sPort))
            });
            MobileDeviceManagerServiceClient = _client.CreateClient(invoker => new MobileDeviceManagerService.MobileDeviceManagerServiceClient(invoker));
        }
    }
}
