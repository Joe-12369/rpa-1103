using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using WebSocket4Net;

namespace II.RPA.Plugin.ZExplorer.Cef
{
    /// <summary>
    /// ZExplorer 背景页的 CDP（Chrome DevTools Protocol）连接，只承载 Runtime.evaluate。
    ///
    /// 不再使用第三方 MasterDevs.ChromeDevTools 的 ChromeSession：该实现在等待 WebSocket Open 时
    /// 执行无超时的 WaitOne，且不向外暴露 Error/Closed 事件。一旦握手没有完成（例如浏览器刚启动、
    /// 目标页尚未就绪），调用线程会永久阻塞：日志停在 discover tabs 之后没有任何输出，
    /// 组件最终只能因为外层 30s 超时被取消（引擎侧表现为 "A task was canceled."）。
    ///
    /// 这里改用 WebSocket4Net：
    /// 1) 建连带超时（默认 5s），失败立即返回并记录 state/error，而不是挂死；
    /// 2) Opened/Error/Closed 全部落日志，可以直接看到失败原因（握手被拒、连接被重置等）；
    /// 3) 单次 Runtime.evaluate 同样带超时，超时后清理等待项。
    /// 对外契约保持不变：JS 返回值仍以字符串形式交给 CdpClientConnector.Evaluate。
    /// </summary>
    internal class CdpConnection : IDisposable
    {
        private static readonly ILogger logger = Logger.OfContext();

        /// <summary>建连超时，超过按失败处理并记录现场。</summary>
        private const int ConnectTimeoutMs = 5000;

        private readonly string socketUrl;
        private readonly WebSocket webSocket;
        private readonly AutoResetEvent connectSignal = new AutoResetEvent(false);
        private readonly ConcurrentDictionary<long, TaskCompletionSource<JObject>> pendingRequests =
            new ConcurrentDictionary<long, TaskCompletionSource<JObject>>();

        private volatile bool opened;
        private volatile string lastError;
        private bool disposed;
        private long commandIdCounter;

        public CdpConnection(string socketUrl)
        {
            this.socketUrl = socketUrl;
            this.webSocket = new WebSocket(socketUrl);
            this.webSocket.EnableAutoSendPing = false;
            this.webSocket.Opened += (sender, args) =>
            {
                this.opened = true;
                this.lastError = null;
                this.connectSignal.Set();
            };
            this.webSocket.Error += (sender, args) =>
            {
                this.lastError = args.Exception?.Message ?? "unknown";
                logger.LogError($"CDP WebSocket 出错：{this.socketUrl}，error={args.Exception}");
                // 握手失败时也唤醒建连等待方，避免出现无超时等待
                this.connectSignal.Set();
            };
            this.webSocket.Closed += (sender, args) =>
            {
                logger.LogWarning($"CDP WebSocket 已关闭：{this.socketUrl}，state={this.webSocket.State}");
                this.connectSignal.Set();
                this.CompletePendingRequests($"CDP 连接已关闭：{this.socketUrl}");
            };
            this.webSocket.MessageReceived += (sender, args) => this.OnMessageReceived(args.Message);

            this.Connect();
        }

        public bool Connected
        {
            get { return !this.disposed && this.webSocket.State == WebSocketState.Open; }
        }

        /// <summary>
        /// 发送一次 Runtime.evaluate 并等待响应。
        /// 成功返回 CDP 响应中的 result 对象（形如 {"result":{...}}）；
        /// 未连接 / 发送失败 / 等待超时返回 null，失败原因均已记录日志。
        /// </summary>
        public JObject Send(string script, int timeoutMs)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(nameof(CdpConnection));
            }

            if (!this.Connected)
            {
                logger.LogWarning($"CDP 未连接，跳过 Runtime.evaluate：{this.socketUrl}，state={this.webSocket.State}，error={this.lastError ?? "<none>"}");
                return null;
            }

            long requestId = Interlocked.Increment(ref this.commandIdCounter);
            var completion = new TaskCompletionSource<JObject>(TaskCreationOptions.RunContinuationsAsynchronously);
            this.pendingRequests[requestId] = completion;

            try
            {
                var request = new JObject
                {
                    { "id", requestId },
                    { "method", "Runtime.evaluate" },
                    {
                        "params", new JObject
                        {
                            { "expression", script },
                            { "returnByValue", true },
                            { "awaitPromise", true }
                        }
                    }
                };
                this.webSocket.Send(request.ToString(Formatting.None));
            }
            catch (Exception ex)
            {
                this.pendingRequests.TryRemove(requestId, out _);
                logger.LogError($"CDP Runtime.evaluate 发送失败：{this.socketUrl}，error={ex.Message}");
                return null;
            }

            int waitMs = timeoutMs > 0 ? timeoutMs : ConnectTimeoutMs;
            if (!completion.Task.Wait(waitMs))
            {
                this.pendingRequests.TryRemove(requestId, out _);
                logger.LogWarning($"CDP Runtime.evaluate 等待响应超时（{waitMs}ms）：{this.socketUrl}");
                return null;
            }

            if (completion.Task.IsFaulted)
            {
                logger.LogError($"CDP Runtime.evaluate 异常：{this.socketUrl}，error={completion.Task.Exception?.GetBaseException().Message}");
                return null;
            }

            return completion.Task.Result;
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            try
            {
                this.webSocket.Close();
            }
            catch (Exception ex)
            {
                logger.LogDebug($"CDP WebSocket 关闭异常：{ex.Message}");
            }

            this.CompletePendingRequests($"CDP 连接已释放：{this.socketUrl}");
            this.connectSignal.Dispose();
        }

        private void Connect()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                this.webSocket.Open();
            }
            catch (Exception ex)
            {
                this.lastError = ex.Message;
                logger.LogError($"CDP WebSocket 建连异常：{this.socketUrl}，error={ex.Message}");
                return;
            }

            if (this.connectSignal.WaitOne(ConnectTimeoutMs) && this.opened)
            {
                logger.LogInformation($"CDP WebSocket 建连成功，耗时 {stopwatch.ElapsedMilliseconds}ms：{this.socketUrl}");
                return;
            }

            logger.LogError(
                $"CDP WebSocket 建连失败（等待 {ConnectTimeoutMs}ms）：{this.socketUrl}；state={this.webSocket.State}；error={this.lastError ?? "<none>"}");
        }

        private void OnMessageReceived(string message)
        {
            try
            {
                var frame = JObject.Parse(message);
                var idToken = frame["id"];
                if (idToken == null)
                {
                    // Runtime.evaluate 场景没有需要处理的订阅事件，仅保留排障信息
                    logger.LogDebug($"CDP 收到通知消息：{message}");
                    return;
                }

                long requestId = idToken.Value<long>();
                if (this.pendingRequests.TryRemove(requestId, out var completion))
                {
                    completion.TrySetResult(frame);
                }
                else
                {
                    logger.LogDebug($"CDP 收到无对应请求的响应 id={requestId}：{message}");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"CDP 响应解析失败：{ex.Message}，raw={message}");
            }
        }

        private void CompletePendingRequests(string reason)
        {
            if (this.pendingRequests.IsEmpty)
            {
                return;
            }

            foreach (var requestId in this.pendingRequests.Keys)
            {
                if (this.pendingRequests.TryRemove(requestId, out var completion))
                {
                    // 以 null 结束等待，Send 侧按“未拿到响应”处理，避免调用方继续等待
                    completion.TrySetResult(null);
                }
            }

            logger.LogDebug($"{reason}，已清理未完成的 CDP 请求");
        }
    }
}
