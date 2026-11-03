using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace II.BrowserAutomation
{
    internal class ServiceConnection : PersistentConnection
    {

        protected override Task OnConnected(IRequest request, string connectionId)
        {
            Log.Info($"{request.QueryString["clientId"]} Connected   {connectionId}");
            return base.OnConnected(request, connectionId);
        }
        protected override Task OnDisconnected(IRequest request, string connectionId, bool stopCalled)
        {
            Log.Info($"{request.QueryString["clientId"]} Disconnected   {connectionId}");
            if (!string.IsNullOrEmpty(request.QueryString["clientId"]) && request.QueryString["clientId"].Equals("Sogou",System.StringComparison.OrdinalIgnoreCase))
            {
                Environment.Exit(0);
            }
            return base.OnDisconnected(request, connectionId, stopCalled);
        }
        protected override Task OnReconnected(IRequest request, string connectionId)
        {
            Log.Info($"{request.QueryString["clientId"]} Reconnected   {connectionId}");
            return base.OnReconnected(request, connectionId);
        }
        protected override Task OnReceived(IRequest request, string connectionId, string data)
        {
            return Connection.Broadcast(data, connectionId);
        }
    }
}
