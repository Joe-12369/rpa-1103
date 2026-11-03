using Grpc.Core;
using II.RPA.Core.Provider.MobileHostProvider;
using Rpa.Core.Mobileplugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Core.Log;
using II.RPA.Communication.Endpoints;
using II.RPA.Communication.Hosting;
using II.RPA.Communication.Options;
using System.ServiceModel.Channels;
using System.IO;

namespace II.RPA.Core.GRPCService.Service
{
    public class MobilePluginGRPCService
    {
        private readonly ILogger m_Logger;
        public MobilePluginService.MobilePluginServiceClient ServiceClient;
        private readonly MobileHostProvider _mobileHostProvider;
        private GrpcClient _client;
        private string _channelName;
        private static readonly string _mobileChannelFileName = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Z-Core\\Z-Mobile.txt";
        //public MobilePluginGRPCService(ILogger aLogger, MobileHostProvider mobileHostProvider)
        //{
        //    m_Logger = aLogger;
        //    _mobileHostProvider = mobileHostProvider;
        //}
        //public override async Task<MobileResponse> PluginActionInvokeUnary(MobileRequest request, ServerCallContext context)
        //{
        //    return await ServiceClient.PluginActionInvokeUnaryAsync(request);
        //}
        public Task StartMobileHostUnary()
        {
            _channelName = Guid.NewGuid().ToString();
            _client = new GrpcClient(new ClientOptions
            {
                Endpoint = new NamedPipeEndpoint(_channelName)
            });
            File.WriteAllText(_mobileChannelFileName, _channelName);
            ServiceClient = _client.CreateClient(invoker => new MobilePluginService.MobilePluginServiceClient(invoker));
            _mobileHostProvider.MobileHostLoader(_channelName);

            return Task.CompletedTask;
        }

        //public ServerServiceDefinition BindingToService()
        //{
        //    return MobilePluginService.BindService(this);
        //}
    }
}
