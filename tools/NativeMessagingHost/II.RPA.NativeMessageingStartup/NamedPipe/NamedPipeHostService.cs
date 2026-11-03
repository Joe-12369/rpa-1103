using System;
using System.Diagnostics;
using System.Threading.Tasks;
using II.RPA.IPC.NamedPipe;
using II.RPA.IPC.NamedPipe.Abstractions;
using II.RPA.IPC.NamedPipe.Server;

namespace II.RPA.NativeMessageingStartup.NamedPipe
{
    internal sealed class NamedPipeHostService : IDisposable
    {
        private const int MaxFrameLength = 1024 * 1024;
        private readonly NamedPipeServer server;

        internal NamedPipeHostService(string serviceKey, INamedPipeRequestHandler handler)
        {
            if (string.IsNullOrWhiteSpace(serviceKey)) throw new ArgumentException("A service key is required.", nameof(serviceKey));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            PipeName = $"{serviceKey}_{Process.GetCurrentProcess().Id}";
            var options = new NamedPipeServerOptions
            {
                MaxConnections = 4,
                MaxPendingRequestsPerConnection = 8,
                MaxPendingRequests = 32,
                MaxFrameLength = MaxFrameLength
            };
            server = new NamedPipeServer(PipeName, handler, options);
        }

        internal string PipeName { get; }
        internal Task Ready => server.Ready;
        internal Task Completion => server.Completion;

        internal Task StartAsync()
        {
            return server.StartAsync();
        }

        internal Task StopAsync()
        {
            return server.StopAsync();
        }

        public void Dispose()
        {
            server.Dispose();
        }
    }
}
