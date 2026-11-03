using System;
using System.ServiceModel;

namespace II.RPA.IPC
{
    internal class IPCPipeClient<T> : IClient<T>
    {
        public T Service { get; internal set; }

        public CommunicationState State
        {
            get
            {
                if (this.Factory != null)
                {
                    return this.Factory.State;
                }
                else
                {
                    return CommunicationState.Faulted;
                }
            }
        }

        internal ICommunicationObject Factory { get; set; }

        public void Dispose()
        {
            this.ChannelCloseSafe(this.Service as IClientChannel);
            this.ChannelCloseSafe(this.Factory);
        }

        private void ChannelCloseSafe(ICommunicationObject channel)
        {
            try
            {
                try
                {
                    if (channel != null)
                    {
                        if (channel.State != CommunicationState.Faulted && channel.State != CommunicationState.Closed)
                        {
                            ///2Sec Timeout
                            channel.Close(new TimeSpan(0, 0, 2));
                        }
                    }
                }
                finally
                {
                    if (channel != null)
                    {
                        channel.Abort();
                    }
                }
            }
            catch
            { }
        }
    }
}