using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Description;
using II.RPA.IPC.Config;

namespace II.RPA.IPC.Builder
{
    public class PipeServerBuilder : IServerBuilder
    {
        private string address = string.Empty;
        private IEnumerable<Type> implementedContracts = null;
        private IServiceConfig pipeConfig = null;
        private Type serviceType = null;
        private object service = null;

        public PipeServerBuilder(IServiceConfig pipeConfig, string address, IEnumerable<Type> implementedContracts, Type serviceType)
        {
            this.pipeConfig = pipeConfig;
            this.address = address;
            this.implementedContracts = implementedContracts;
            this.serviceType = serviceType;
        }

        public PipeServerBuilder(IServiceConfig pipeConfig, string address, IEnumerable<Type> implementedContracts, object service)
        {
            this.pipeConfig = pipeConfig;
            this.address = address;
            this.implementedContracts = implementedContracts;
            this.serviceType = service.GetType();
            this.service = service;
        }

        public IServer Build()
        {
            IPCPipeServer server = new IPCPipeServer()
            {
                Address = this.address,
                Config = this.pipeConfig,
                ImplementedContracts = this.implementedContracts,
                ServiceType = this.serviceType
            };

            if (this.service != null)
            {
                server.Host = new ServiceHost(this.service);
            }
            else
            {
                server.Host = new ServiceHost(this.serviceType);
            }

            NetNamedPipeBinding binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.Transport)
            {
                ReaderQuotas =
                            {
                                MaxArrayLength = int.MaxValue,
                                MaxStringContentLength = int.MaxValue
                            }
            };

            binding.CloseTimeout = this.pipeConfig.CloseTimeout;
            binding.OpenTimeout = this.pipeConfig.OpenTimeout;
            binding.ReceiveTimeout = this.pipeConfig.ReceiveTimeout;
            binding.SendTimeout = this.pipeConfig.SendTimeout;
            binding.TransferMode = this.pipeConfig.TransferMode;
            binding.MaxBufferPoolSize = this.pipeConfig.MaxBufferPoolSize;
            binding.MaxBufferSize = this.pipeConfig.MaxBufferSize;
            binding.MaxConnections = this.pipeConfig.MaxConnections;
            binding.MaxReceivedMessageSize = this.pipeConfig.MaxReceivedMessageSize;
            server.Host.Description.Behaviors.Find<ServiceDebugBehavior>().IncludeExceptionDetailInFaults = true;

            foreach (var item in this.implementedContracts)
            {
                server.Host.AddServiceEndpoint(item, binding, this.address);
            }
            return server;
        }
    }

    public sealed class PipeServerBuilder<T, K> : PipeServerBuilder where T : K
    {
        public PipeServerBuilder(IServiceConfig pipeConfig, string address) :
            base(pipeConfig, address, new List<Type>() { typeof(K) }, typeof(T))
        {
        }
    }
}
