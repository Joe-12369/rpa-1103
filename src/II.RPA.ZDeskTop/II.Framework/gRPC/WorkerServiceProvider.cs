using Grpc.Core;
using II.Framework.Utility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Rpa.Core.Worker;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using static Rpa.Core.Worker.WorkerService;

namespace II.Framework.gRPC
{
    public class WorkerServiceProvider
    {
        private readonly WorkerServiceClient _workerServiceClient;
        private readonly ILogger _logger;
        private readonly AsyncDuplexStreamingCall<FrameStream, FrameStream> _call;
        private readonly WorkerConnection _workerConnection;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly int _pid;
        public WorkerServiceProvider(IConfiguration configuration, 
            IServiceProvider serviceProvider,
            ILogger<WorkerServiceProvider> logger)
        {
            _logger = logger;
            _pid = EnvironmentHelper.CurrentProcessId;
            var port = configuration.GetValue<int>("p");
            _logger.LogInformation($"server port:{port}");
            if (port <= 0)
            {
                throw new ArgumentException("从命令行参数获取参数 \'--p\' 的值的失败");
            }
            var channel = new Channel($"127.0.0.1:{port}", ChannelCredentials.Insecure);
            _workerServiceClient = new WorkerServiceClient(channel);
            _workerConnection = new WorkerConnection(serviceProvider);
            _cancellationTokenSource = new CancellationTokenSource();
        }
        public WorkerConnection Connection => _workerConnection;
        public async Task Connect()
        {
            var headers = new Metadata
            {
                { "pid", $"{_pid}" },
                { "role", "zdesktop" },
                { "name", "zdesktop" }
            };
            var callOptions = new CallOptions(headers, cancellationToken: _cancellationTokenSource.Token);
            _workerConnection.Streamings = _workerServiceClient.Talk(callOptions);
            await _workerConnection.Connect();
            _workerConnection.Meta = await _workerConnection.Streamings.ResponseHeadersAsync;
        }
        public void DisConnect()
        {
            if(!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }
            _workerConnection.Cancel();
            _workerConnection?.Dispose();
        }
    }
}
