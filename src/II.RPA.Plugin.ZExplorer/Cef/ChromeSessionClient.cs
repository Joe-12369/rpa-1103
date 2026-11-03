using II.RPA.Logging;
using MasterDevs.ChromeDevTools.Protocol.Chrome.Runtime;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using WebSocket4Net;

namespace II.RPA.Plugin.ZExplorer.Cef
{
    public class ChromeSessionClient : IDisposable
    {
        private ILogger log = Logger.OfContext();
        public ChromeSessionClient(TabPage tabPage)
        {
            this.tabConnection = new ChromeSessionConnection(tabPage.WebSocketDebuggerUrl);
            this.tabPage = tabPage;
        }
        public bool Connected { get { return this.tabConnection.Connected == null || tabConnection.Connected == true; } }

        public object Id { get; internal set; }

        public void Dispose()
        {
            this.tabConnection.Dispose();
        }

        internal void Update(TabPage tab)
        {
            this.tabPage = tab;
        }

        public CefResult<JToken> Evaluate(string request, int timeOut = 5000)
        {
            Stopwatch sw = Stopwatch.StartNew();
            SyncResponseTimeout = TimeSpan.FromMilliseconds(timeOut);
            var task = tabConnection.Send(request);
            if (task.Wait(SyncResponseTimeout))
            {
                var remoteObject = task.Result.Result.Result;
                log.LogInformation($"cdp evaluate 耗时：{sw.ElapsedMilliseconds}");
                if (remoteObject?.Subtype?.Contains("error") == true)
                {
                    return CefResult.Error<JToken>(remoteObject.Description);
                }
                else
                {
                    return CefResult.Ok<JToken>(remoteObject.Value.ToString());
                }
            }
            return CefResult.Error<JToken>("(timed out)");
        }

        private TimeSpan SyncResponseTimeout;

        private readonly ChromeSessionConnection tabConnection;

        private readonly ConcurrentDictionary<long, TaskCompletionSource<bool>> outstandingChildResolveRequests;

        private TabPage tabPage;
    }
}
