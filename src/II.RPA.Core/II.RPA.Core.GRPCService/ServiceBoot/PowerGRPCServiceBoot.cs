using Grpc.Core;
using II.RPA.Core.Abstract;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace II.RPA.Core.GRPCService.ServiceBoot
{
    public class PowerGRPCServiceBoot : IServiceBoot
    {
        private readonly IGRPCService[] m_Services;
        private Server m_GRPCServer;
        public PowerGRPCServiceBoot(IGRPCService[] aServices)
        {
            IP = "0.0.0.0";
            Port = 8191;
            m_Services = aServices;
        }

        public string BootName => nameof(PowerGRPCServiceBoot);
        public int Port { get; set; }
        public string IP { get; set; }

        public void Start()
        {
            m_GRPCServer = new Server
            {
                Ports =
                {
                    // C-core benchmarks currently only support insecure (h2c)
                    {
                        IP,
                        Port,
                        ServerCredentials.Insecure
                    }
                }
            };
            m_Services.Select(s => s.BindingToService())
                .ToList()
                .ForEach(sd => m_GRPCServer.Services.Add(sd));
            m_GRPCServer.Start();
        }

        public async Task StopAsync()
        {
            await m_GRPCServer.ShutdownAsync();
        }
        public void Stop()
        {
            m_GRPCServer.KillAsync();
        }
        public void StopAndDispose()    
        {
            throw new NotImplementedException();
        }

        public Task StopAsync(int aDelayMilliSecond)
        {
            throw new NotImplementedException();
        }

        public Task ReStartAsync(int aStopDelayMilliSecond)
        {
            throw new NotImplementedException();
        }
    }
}
