using II.RPA.IPC;
using II.RPA.IPC.Config;
using System;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading;

namespace II.RPA.WebBrowser.Common.Basic.connection
{
    internal interface IWcfConnectionProbe : IDisposable
    {
        void Pending(int timeoutMilliseconds);
    }

    internal interface IWcfConnectionProbeFactory
    {
        IWcfConnectionProbe Create(string address);
    }

    internal sealed class WcfConnectionProbeFactory : IWcfConnectionProbeFactory
    {
        public IWcfConnectionProbe Create(string address)
        {
            return new WcfConnectionProbe(
                ClientGenerator.Instance.Generate<IMessageExchange>(PipeConfig.DefaultPipe, address));
        }
    }

    internal sealed class WcfConnectionProbe : IWcfConnectionProbe
    {
        private readonly IClient<IMessageExchange> _client;

        internal WcfConnectionProbe(IClient<IMessageExchange> client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public void Pending(int timeoutMilliseconds)
        {
            if (_client.Service is IContextChannel channel)
            {
                channel.OperationTimeout = TimeSpan.FromMilliseconds(
                    Math.Max(1, timeoutMilliseconds));
            }

            _client.Service.Pending();
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }

    internal interface IConnectionWaitClock
    {
        long ElapsedMilliseconds { get; }

        void Restart();

        void Wait(int milliseconds);
    }

    internal sealed class ConnectionWaitClock : IConnectionWaitClock
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();

        public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;

        public void Restart()
        {
            _stopwatch.Restart();
        }

        public void Wait(int milliseconds)
        {
            if (milliseconds > 0)
            {
                Thread.Sleep(milliseconds);
            }
        }
    }
}
