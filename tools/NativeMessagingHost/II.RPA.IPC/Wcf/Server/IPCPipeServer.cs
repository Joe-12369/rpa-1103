using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace II.RPA.IPC
{
    internal class IPCPipeServer : IServer
    {
        public string Address { get; internal set; }

        public IServiceConfig Config { get; internal set; }

        public IEnumerable<Type> ImplementedContracts { get; internal set; }

        public Type ServiceType { get; internal set; }

        internal ServiceHost Host { get; set; }

        public CommunicationState State
        {
            get
            {
                if (this.Host != null)
                {
                    return this.Host.State;
                }
                else
                {
                    return CommunicationState.Closed;
                }
            }
        }

        public void Close()
        {
            try
            {
                this.Host.Close();
            }
            catch
            {
                //TODO
            }

            try
            {
                this.Host.Abort();
            }
            catch
            {
                //TODO
            }

            this.Host = null;
        }

        public void Dispose()
        {
            this.Close();
        }

        public void Open()
        {
            this.Host.Open();
        }
    }
}