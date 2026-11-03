using II.RPA.Logging;
using II.RPA.Plugin.ZExplorer.Cef.EventArgs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebSocket4Net;

namespace II.RPA.Plugin.ZExplorer.Cef
{
    public sealed class ChromeDebugProtocolClient : IDisposable
    {
        private ILogger log = Logger.OfContext();
        public ChromeDebugProtocolClient(TabPage tabPage) : this(tabPage, new WebSocketTabConnection(tabPage.Id, new WebSocketClient(tabPage.WebSocketDebuggerUrl.Replace("localhost", "127.0.0.1"))))
        {
        }

        public ChromeDebugProtocolClient(TabPage tabPage, WebSocketTabConnection connection)
        {
            this.tabConnection = connection;
            this.tabConnection.CommandReceived += this.ProcessNotification;
            this.outstandingChildResolveRequests = new ConcurrentDictionary<long, TaskCompletionSource<bool>>();
            this.tabPage = tabPage;
            // this.StartupDebugDomains();
        }

        private void ProcessNotification(object sender, CommandEventArgs e)
        {
            log.LogDebug(e.Method, e.Parameters);
        }

        public bool IsAlive
        {
            get
            {
                return this.tabConnection.IsAlive;
            }
        }

        public WebSocketState State
        {
            get
            {
                return this.tabConnection.State;
            }
        }

        public TabPage ChromiumTabPage
        {
            get
            {
                return this.tabPage;
            }
        }

        internal string Id
        {
            get
            {
                return this.tabPage.Id;
            }
        }

        public void Dispose()
        {
            this.tabConnection.CommandReceived -= this.ProcessNotification;
            this.tabConnection.Dispose();
        }

        internal void Update(TabPage tab)
        {
            this.tabPage = tab;
        }

        public CefResult<JToken> Evaluate(string expression, int timeOut = 5000)
        {
            SyncResponseTimeout = TimeSpan.FromSeconds(timeOut / 1000);
            return this.Send("Runtime.evaluate", new object[]
            {
                "expression",
                 expression,
                 "awaitPromise",
                 true,
                 "timeout",
                 timeOut
            }
            );
        }

        public CefResult<JToken> Send(string method, params object[] parameters)
        {
            Task<CefResult<JObject>> task = tabConnection.Send(method, parameters);
            if (task.Wait(SyncResponseTimeout))
            {
                if (task.Result.IsError)
                {
                    CefResult<JObject> result = task.Result;
                    CefError? error1 = result.Error;
                    log.LogDebug($"CDP Response Error :{error1}");
                    result = task.Result;
                    CefError? error2 = result.Error;
                    ref CefError? local = ref error2;
                    if ((local.HasValue ? (local.GetValueOrDefault().Error == CefErrorType.NotConnected ? 1 : 0) : 0) != 0 && tabConnection.Connect().IsSuccess)
                    {
                        return CefResult.Error<JToken>("(reconnected)");
                    }
                    result = task.Result;
                    return (CefResult<JToken>)result.ToErrorOf<JToken>();
                }
                CefResult<JObject> result1;
                log.LogDebug($"CDP Response to {method} received");
                result1 = task.Result;
                return CefResult.Ok<JToken>(result1.Success);
            }
            log.LogDebug($"CDP Response to {method} (timed out)");
            return CefResult.Error<JToken>("(timed out)");
        }

        internal void Post(string method, params object[] parameters)
        {
            this.tabConnection.Send(method, parameters).ContinueWith(delegate (Task<CefResult<JObject>> t)
            {
                if (t.IsCompleted)
                {
                    log.LogDebug("CDP Response Error (not awaited) received");
                }
            });
        }

        private static Dictionary<string, string> GetAttributes(JToken nodeData)
        {
            JArray jarray = nodeData.Value<JArray>("attributes");
            if (jarray == null || jarray.Count == 0)
            {
                return null;
            }
            Dictionary<string, string> dictionary = new Dictionary<string, string>(jarray.Count / 2, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < jarray.Count - 1; i += 2)
            {
                string key = jarray[i].ToString();
                string value = jarray[i + 1].ToString();
                dictionary.Add(key, value);
            }
            return dictionary;
        }


        private void StartupDebugDomains()
        {
            try
            {
                this.Send("DOM.enable", Array.Empty<object>());
                this.Send("CSS.enable", Array.Empty<object>());
            }
            catch
            {
            }
        }


        private TimeSpan SyncResponseTimeout;

        private readonly WebSocketTabConnection tabConnection;

        private readonly ConcurrentDictionary<long, TaskCompletionSource<bool>> outstandingChildResolveRequests;

        private TabPage tabPage;
    }
}
