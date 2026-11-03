using Grpc.Core;
using II.RPA.Communication.Endpoints;
using II.RPA.Communication.Hosting;
using II.RPA.Communication.Options;
using II.RPA.Core.Common;
using II.RPA.Core.Log;
using Rpa.Core.Plugin;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Core.GRPCServiceClient.ClientProvider
{
    /// <summary>
    /// PluginServiceClientProvider
    /// </summary>
    public class PluginServiceClientProvider : IDisposable
    {
        private readonly PluginService.PluginServiceClient _pluginServiceClient;
        private readonly GrpcClient _client;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="aLogger"></param>
        public PluginServiceClientProvider()
        {
            _client = new GrpcClient(new ClientOptions
            {
                Endpoint = new TcpEndpoint("127.0.0.1", Global.ServerPort)
            });
            _pluginServiceClient = _client.CreateClient(invoker => new PluginService.PluginServiceClient(invoker));
        }

        /// <summary>
        /// SetParamsUnaryAsync
        /// </summary>
        /// <param name="aPackagePath"></param>
        /// <param name="aParamBase64"></param>
        /// <param name="aCancellationToken"></param>
        /// <returns></returns>
        public async Task SetParamsUnaryAsync(string aPackagePath, string aParamBase64, CancellationToken aCancellationToken = default)
        {
            var setParamsRequest = new SetParamPluginRequest
            {
                ClientId = Global.ClientIDFormatString,
                Payload = new PayloadSetParam
                {
                    PackagePath = aPackagePath,
                    ParamBase64 = aParamBase64,
                },
            };
            await _pluginServiceClient.PluginSetParamUnaryAsync(setParamsRequest, cancellationToken: aCancellationToken);
        }

        /// <summary>
        /// PluginDisposeUnary
        /// </summary>
        /// <param name="aPackagePath"></param>
        /// <param name="aWaitReady"></param>
        /// <param name="aReuseRuntime"></param>
        /// <param name="aCancellationToken"></param>
        /// <returns></returns>
        public async Task PluginDisposeUnary(string aPackagePath, bool aWaitReady = true, bool aReuseRuntime = false, CancellationToken aCancellationToken = default)
        {
            var disposeRequest = new DisposePluginRequest
            {
                ClientId = "rpa-engine.exe",
                Payload = new PayloadDispose
                {
                    PackagePath = aPackagePath,
                    WaitReady = aWaitReady,
                    ReuseRuntime = aReuseRuntime,
                },
            };
            await _pluginServiceClient.PluginDisposeUnaryAsync(disposeRequest, cancellationToken: aCancellationToken);
        }

        public void PluginStopUnary(string aPackagePath, bool aWaitReady = true, bool aReuseRuntime = false, CancellationToken aCancellationToken = default)
        {
            var disposeRequest = new StopPluginRequest
            {
                ClientId = "self",
                PackagePath = aPackagePath,
            };
            _pluginServiceClient.PluginStopUnary(disposeRequest, cancellationToken: aCancellationToken);
        }

        public void Dispose()
        {
            _client.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
