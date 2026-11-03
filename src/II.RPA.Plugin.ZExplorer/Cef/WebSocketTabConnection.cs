using II.RPA.Logging;
using II.RPA.Plugin.ZExplorer.Cef.EventArgs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using WebSocket4Net;

namespace II.RPA.Plugin.ZExplorer.Cef
{
    public sealed class WebSocketTabConnection : IDisposable
    {
        public event EventHandler<CommandEventArgs> CommandReceived;

        public WebSocketTabConnection(string id, WebSocketClient webSocket)
        {
            this.id = id;
            this.cancellationTokenSource = new CancellationTokenSource();
            this.responseEvents = new ConcurrentDictionary<long, TaskCompletionSource<CefResult<JObject>>>();
            this.receivedCommands = new BlockingCollection<CommandEventArgs>(new ConcurrentQueue<CommandEventArgs>());
            this.commandNotificationDispatcher = Task.Factory.StartNew(delegate ()
            {
                try
                {
                    while (!this.receivedCommands.IsCompleted)
                    {
                        CommandEventArgs e;
                        if (this.receivedCommands.TryTake(out e, -1, this.cancellationTokenSource.Token))
                        {
                            EventHandler<CommandEventArgs> commandReceived = this.CommandReceived;
                            if (commandReceived != null)
                            {
                                commandReceived(this, e);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
                log.LogDebug("Notification dispatching for tab " + this.id + " completed");
            }, this.cancellationTokenSource.Token, TaskCreationOptions.None, TaskScheduler.Default);
            this.webSocket = webSocket;
            this.webSocket.OnMessage += this.WebSocket_OnMessage;
            this.webSocket.OnOpen += this.WebSocket_OnOpen;
            this.webSocket.OnError += this.WebSocket_OnError;
            this.webSocket.OnClose += this.WebSocket_OnClose;
            log.LogDebug("WebSocket Connect (tab " + this.id + ")");
            this.webSocket.Connect();
        }

        public bool IsAlive
        {
            get
            {
                return this.webSocket.IsAlive;
            }
        }

        public WebSocketState State
        {
            get
            {
                return this.webSocket.ReadyState;
            }
        }

        public void Dispose()
        {
            try
            {
                this.webSocket.Close();
                this.receivedCommands.CompleteAdding();
                this.cancellationTokenSource.Cancel();
            }
            catch (Exception)
            {
            }
        }

        public Task<CefResult<JObject>> Send(string method, params object[] parameters)
        {
            long id = Interlocked.Increment(ref this.commandIdCounter);
            TaskCompletionSource<CefResult<JObject>> taskCompletionSource = new TaskCompletionSource<CefResult<JObject>>();
            bool success = this.responseEvents.TryAdd(id, taskCompletionSource);
            if (!success)
            {
                log.LogDebug(string.Format("Cannot add RequestState {0}:{1}", id, method));
                throw new InvalidOperationException("Command ID already in use. (Bug)");
            }
            string text = this.CreateCommandData(id, method, parameters);
            if (this.webSocket.ReadyState != WebSocketState.Open)
            {
                log.LogDebug(string.Format("WebSocket is {0} (tab {1})", this.webSocket.ReadyState, this.id));
                taskCompletionSource.SetResult(CefResult.Error<JObject>(CefErrorType.NotConnected));
            }
            else
            {
                log.LogDebug("Send " + method);
                this.webSocket.Send(text);
            }
            return taskCompletionSource.Task.ContinueWith(delegate (Task<CefResult<JObject>> task)
            {
                TaskCompletionSource<CefResult<JObject>> completionSource;
                success = this.responseEvents.TryRemove(id, out completionSource);
                if (!success)
                {
                    log.LogDebug(string.Format("Cannot remove RequestState {0}:{1}", id, method));
                    throw new InvalidOperationException("Command ID unknown. (Bug)");
                }
                return task.Result;
            }, TaskScheduler.Default);
        }

        public CefResult<CefUnit> Connect()
        {
            this.webSocket.Connect();
            if (this.webSocket.ReadyState != WebSocketState.Open)
            {
                return CefResult.Error<CefUnit>(string.Format("WebSocket is {0}", this.webSocket.ReadyState));
            }
            return CefResult.Unit();
        }

        internal bool CloseAndCompleteCommandNotification(TimeSpan timeout)
        {
            this.webSocket.Close();
            this.receivedCommands.CompleteAdding();
            return this.commandNotificationDispatcher.Wait(timeout);
        }

        private string CreateCommandData(long id, string method, params object[] parameters)
        {
            JObject jobject = new JObject();
            jobject.Add("id", id);
            jobject.Add("method", method);
            if (parameters != null && parameters.Length != 0)
            {
                JObject jobject2 = new JObject();
                for (int i = 0; i < parameters.Length; i += 2)
                {
                    string propertyName = parameters[i].ToString();
                    jobject2.Add(propertyName, JToken.FromObject(parameters[i + 1]));
                }
                jobject.Add("params", jobject2);
            }
            return jobject.ToString(Formatting.None, Array.Empty<JsonConverter>());
        }

        private void WebSocket_OnClose(object sender, CloseEventArgs e)
        {
            if (!e.WasClean)
            {
                log.LogDebug("Unclean websocket close (tab " + this.id + "): " + e.Reason);
            }
        }

        private void WebSocket_OnError(object sender, Plugin.ZExplorer.Cef.EventArgs.ErrorEventArgs e)
        {
            log.LogDebug(string.Format("Websocket ERROR (tab {0}): {1}", this.id, e.Exception));
        }

        private void WebSocket_OnOpen(object sender, System.EventArgs e)
        {
            log.LogDebug("Websocket connected (tab " + this.id + ")");
        }

        private void WebSocket_OnMessage(object sender, Plugin.ZExplorer.Cef.EventArgs.MessageEventArgs e)
        {
            JObject jobject = (JObject)JsonConvert.DeserializeObject(e.Data);
            JValue jvalue = (JValue)jobject["id"];
            if (jvalue == null)
            {
                string method = jobject.Value<string>("method");
                JObject msgparams = (JObject)jobject["params"];
                this.TryAddReceivedCommands(method, msgparams);
                return;
            }
            long num = jvalue.Value<long>();
            this.responseEvents.TryGetValue(num, out TaskCompletionSource<CefResult<JObject>> taskCompletionSource);
            if (taskCompletionSource == null)
            {
                log.LogDebug(string.Format("Response with no open request received id: {0} (tab {1})", num, this.id));
                return;
            }
            JObject jobject2 = (JObject)jobject["result"];
            if (jobject2 != null)
            {
                taskCompletionSource.SetResult(CefResult.Ok<JObject>(jobject2));
                return;
            }
            JObject jobject3 = (JObject)jobject["error"];
            taskCompletionSource.SetResult(CefResult.Error<JObject>((jobject3 != null) ? jobject3.ToString() : null));
        }

        private void TryAddReceivedCommands(string method, JObject msgparams)
        {
            try
            {
                this.receivedCommands.Add(new CommandEventArgs
                {
                    Method = method,
                    Parameters = msgparams
                });
            }
            catch (InvalidOperationException arg)
            {
                log.LogDebug(string.Format("WebSocket_OnMessage add received command to blocking collection error: {0}", arg));
            }
        }

        private static readonly ILogger log = Logger.OfContext();

        private readonly string id;

        private readonly CancellationTokenSource cancellationTokenSource;

        private readonly ConcurrentDictionary<long, TaskCompletionSource<CefResult<JObject>>> responseEvents;

        private readonly BlockingCollection<CommandEventArgs> receivedCommands;

        private readonly Task commandNotificationDispatcher;

        private readonly WebSocketClient webSocket;

        private long commandIdCounter;
    }
}
