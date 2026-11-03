using II.RPA.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace II.RPA.ServiceHub.Transfer
{
    public class TransferHub : Hub
    {
        private readonly TransferMessageExecutor _executor;
        private ILogger logger = Logger.OfContext();

        public TransferHub(TransferMessageExecutor executor)
        {
            _executor = executor;
        }

        public async Task SendTransferMessage(string message)
        {
            await Task.Run(() =>
            {
                _executor.QueuingMessage(this.Context.ConnectionId, message);
            });
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                logger.LogInformation($"new client Connection:{this.Context.ConnectionId}");
                var httpContext = this.Context.GetHttpContext();
                var remoteIpAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
                var remotePort = httpContext?.Connection.RemotePort.ToString();
                var clientFlag = httpContext?.Request.Query["ClientFlag"].ToString();
                var clientType = httpContext?.Request.Query["ClientType"].ToString();

                logger.LogDebug($"ClientFlag:{clientFlag},ClientType:{clientType}");
                logger.LogDebug($"remote：{remoteIpAddress}:{remotePort}");

                ConnectionInfo connInfo = new ConnectionInfo
                {
                    ConnectionId = this.Context.ConnectionId,
                    Port = int.TryParse(remotePort, out var p) ? p : 0,
                    ClientFlag = clientFlag,
                    ClientType = int.TryParse(clientType, out var ct) ? (SignalRClientType)ct : SignalRClientType.Unknown
                };
                ClientConnections.AddConnectionInfo(connInfo);
                logger.LogInformation("Connected");
            }
            catch (Exception ex)
            {
                logger.LogError($"Connected Fail,{ex}");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            logger.LogDebug($"OnDisconnected: {this.Context.ConnectionId}");
            ClientConnections.RemoveConnectionInfo(this.Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
