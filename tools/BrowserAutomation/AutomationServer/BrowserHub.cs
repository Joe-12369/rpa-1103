using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace II.BrowserAutomation
{
    public class BrowserHub : Hub
    {
        public async Task SendToBrowser(string command, object args)
        {
            Log.Info($"rpa call:    {command} {args}");
            await Clients.Others.SendAsync("Excute", command, args);
        }
        public async Task SendToRPA(string response)
        {
            Log.Info($"browser response:    {response}");
            await Clients.Others.SendAsync("Response", response);
        }

        public override async Task OnConnectedAsync()
        {
            Log.Info($"Ononnected: {this.Context.ConnectionId}");
            await base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Log.Info($"OnDisconnected: {this.Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }
    }
}
