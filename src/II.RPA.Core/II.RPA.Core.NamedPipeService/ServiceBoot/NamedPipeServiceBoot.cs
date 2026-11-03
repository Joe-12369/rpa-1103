using II.RPA.Core.Abstract;
using System;
using System.Threading.Tasks;

namespace II.RPA.Core.NamedPipeService.ServiceBoot
{
    public class NamedPipeServiceBoot : IServiceBoot
    {
       

        public string BootName => throw new NotImplementedException();

        public int Port { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string IP { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public Task StopAsync(int aDelayMilliSecond)
        {
            throw new NotImplementedException();
        }
    }
}
