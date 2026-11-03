using Grpc.Core;
using II.RPA.Communication.Endpoints;
using II.RPA.Communication.Hosting;
using II.RPA.Communication.Options;
using II.RPA.Core.Abstract;
using II.RPA.Core.Common;
using II.RPA.Core.GRPCService.Service;
using II.RPA.Core.Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace II.RPA.Core.GRPCService.ServiceBoot
{
    public class GRPCServiceBoot : IServiceBoot, IAdminServiceBoot
    {
        private readonly IEnumerable<IGRPCService> _grpcServices;
        private readonly IMessageNotify _messageService;
        private readonly IMessageNotify _visualDataService;
        private readonly ILogger _logger;
        private readonly IEndPointProvider _endPointProvider;
        private readonly MobileDeviceManagerGRPCService _mobileDeviceManagerGRPCService;

        private IServerContext _serverContext;
        private GrpcServer? _tcpServer;

        public GRPCServiceBoot(IGRPCService[] aServices,
            string anIP,
            string aPort,
            ILogger aLogger,
            IEndPointProvider anEndPointProvider,
            IMessageNotify aMessageService,
            IMessageNotify aVisualDataService,
            MobileDeviceManagerGRPCService aMobileDeviceManagerGRPCService)
        {
            _logger = aLogger;
            IP = anIP;
            string[] ports = aPort.Split(',');
            int index = (int)Global.Client;
            Port = int.Parse(ports[index]);
            _grpcServices = aServices;
            _endPointProvider = anEndPointProvider;
            _messageService = aMessageService;
            _visualDataService = aVisualDataService;
            _mobileDeviceManagerGRPCService = aMobileDeviceManagerGRPCService;
        }

        public string BootName { get => nameof(GRPCServiceBoot); }
        public string IP { get; set; }
        public int Port
        {
            get => Global.ServerPort;
            set => Global.ServerPort = value;
        }

        public async Task StopAsync(int aDelayMilliSecond)
        {
            if (File.Exists("search.json"))
            {
                File.Delete("search.json");
            }
            _logger.LogInfo($"shutdown request received.server will be stopped in {aDelayMilliSecond}ms");
            await Task.Delay(aDelayMilliSecond);
            await _tcpServer?.StopAsync();
            if (_serverContext != null)
            {
                await _serverContext.ShutDown(100);
            }
        }
        public void StopServerOnly()
        {
            _logger.LogInfo($"Stop serverBoot");
        }

        public async Task StartAsync()
        {
            var serverServiceDefinitions = _grpcServices.Select(service =>
            {
                if (service is IAdminProvider adminProvider)
                {
                    adminProvider.AuthToManageServiceBoot(this);
                }
                return service.BindingToService();
            });

            void configureServices(GrpcServiceRegistry service)
            {
                foreach (var item in serverServiceDefinitions)
                {
                    service.Register(item);
                }

                service.Register(_messageService.BindingToService());
                service.Register(_visualDataService.BindingToService());
                service.Register(_mobileDeviceManagerGRPCService.BindingToService());
            }

            while (true)
            {
                _tcpServer = new GrpcServer(new ServerOptions()
                {
                    Endpoint = new TcpEndpoint("127.0.0.1", Port),
                }, configureServices);

                try
                {
                    await _tcpServer.StartAsync();
                    break;
                }
                catch (IOException ex)
                {
                    _logger.LogWarning($"{BootName} fail to bind port:{Port}", ex);
                    await _tcpServer.StopAsync();
                    _tcpServer = null;
                    Port = _endPointProvider.GetAvailablePort(Port, BootName);
                    _logger.LogInfo($"{BootName} try to restart with new port:{Port}");
                }
            }

            _logger.LogInfo($"{BootName} is starting with port:{Port}");
            _logger.LogInfo($"{BootName}'s services : {Environment.NewLine}{string.Join(Environment.NewLine, _grpcServices.Select(s => s.GetType().Name))}");
        }

        public void AuthToManageServer(IServerContext aServerContext)
        {
            _serverContext = aServerContext;
        }

        public async Task ReStartAsync(int aStopDelayMilliSecond)
        {
            _logger.LogInfo($"shutdown request received.{nameof(GRPCServiceBoot)} will be restarted in {aStopDelayMilliSecond}ms");
            await StopAsync(aStopDelayMilliSecond);
            await StartAsync();
        }
    }
}
