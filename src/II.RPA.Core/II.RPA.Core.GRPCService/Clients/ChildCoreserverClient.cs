using Grpc.Core;
using II.RPA.Communication.Endpoints;
using II.RPA.Communication.Hosting;
using II.RPA.Communication.Options;
using II.RPA.Core.Common;
using System;
using System.IO;
using System.Threading;
using static Rpa.Core.Worker.WorkerService;

namespace II.RPA.Core.GRPCService.Clients
{
    public class ChildCoreserverClient
    {
        private readonly WorkerServiceClient _workerServiceClient;
        private readonly CallOptions _callOptions;
        private readonly GrpcClient _client;
        public ChildCoreserverClient() 
        {
            Cancellation = new CancellationTokenSource();
            var fileName = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Z-Core\\{Global.RootClientString}.txt";
            if (File.Exists(fileName))
            {
                string idString = File.ReadAllText(fileName);
                var port = int.Parse(idString);

                var headers = new Metadata
                {
                    { "pid", $"{Global.CurrentPId}" },
                    { "role", "childcoreserver" },
                    { "name", "childcoreserver" }
                };
                _callOptions = new CallOptions(headers, cancellationToken: Cancellation.Token);

                _client = new GrpcClient(new ClientOptions
                {
                    Endpoint = new TcpEndpoint("127.0.0.1", port)
                });
                _workerServiceClient = _client.CreateClient(invoker => new WorkerServiceClient(invoker));
            }
        }

        public CancellationTokenSource Cancellation 
        { 
            get; 
            private set; 
        }

        public void StartTalking()
        {
           _workerServiceClient.Talk(_callOptions);
        }
    }
}
