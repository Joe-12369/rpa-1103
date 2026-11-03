using System.ServiceModel;
using II.RPA.IPC.Config;

namespace II.RPA.IPC
{
    public class ClientGenerator
    {
        static ClientGenerator()
        {
            Instance = new ClientGenerator();
        }

        private ClientGenerator()
        { }

        public static ClientGenerator Instance
        {
            get;
            private set;
        }

        public IClient<T> Generate<T>(IServiceConfig pipeConfig, string address)
        {
            var endPoint = new EndpointAddress(address);
            return this.Generate<T>(pipeConfig, endPoint);
        }

        public IClient<T> Generate<T>(IServiceConfig pipeConfig, EndpointAddress address)
        {
            NetNamedPipeBinding binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.Transport)
            {
                ReaderQuotas =
                            {
                                MaxArrayLength = int.MaxValue,
                                MaxStringContentLength = int.MaxValue
                            }
            };
            binding.CloseTimeout = pipeConfig.CloseTimeout;
            binding.OpenTimeout = pipeConfig.OpenTimeout;
            binding.ReceiveTimeout = pipeConfig.ReceiveTimeout;
            binding.SendTimeout = pipeConfig.SendTimeout;
            binding.TransferMode = pipeConfig.TransferMode;
            binding.MaxBufferPoolSize = pipeConfig.MaxBufferPoolSize;
            binding.MaxBufferSize = pipeConfig.MaxBufferSize;
            binding.MaxConnections = pipeConfig.MaxConnections;
            binding.MaxReceivedMessageSize = pipeConfig.MaxReceivedMessageSize;
            var endPoint = address;

            //var binding2 = new NetNamedPipeBinding(NetNamedPipeSecurityMode.Transport);

            //创建通道
            var factory = new ChannelFactory<T>(binding, endPoint);
            var service = factory.CreateChannel();
            IPCPipeClient<T> client = new IPCPipeClient<T>()
            {
                Service = service,
                Factory = factory
            };
            return client;
        }
    }
}
