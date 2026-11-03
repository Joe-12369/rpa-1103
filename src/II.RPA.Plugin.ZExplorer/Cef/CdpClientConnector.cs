using II.RPA.Logging;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace II.RPA.Plugin.ZExplorer.Cef
{
    public sealed class CdpClientConnector : IDisposable
    {
        private TabPage BackgroundPage { get; set; }
        public CdpClientConnector()
        {
            this.tabDiscoverer = new TabDiscoverer();
        }
        public void Connect(int port)
        {
            BackgroundPage = null;
            tabConnection?.Dispose();
            BackgroundPage = tabDiscoverer.DiscoverBackgroundPage(port);
            if (BackgroundPage == null)
            {
                throw new ZExplorerException(eErrorMessageType.Error_ZEXPLORERNOTFOUND);
            }
            logger.LogInformation($"连接 ZExplorer 背景页 CDP：port={port}，url={BackgroundPage.WebSocketDebuggerUrl}");
            this.tabConnection = new CdpConnection(BackgroundPage.WebSocketDebuggerUrl);
        }
        public bool Connected
        {
            get { return tabConnection?.Connected == true; }
        }
        public void Dispose()
        {
            tabDiscoverer?.Dispose();
            tabConnection?.Dispose();
        }

        private TabDiscoverer tabDiscoverer;
        private CdpConnection tabConnection;

        private static readonly ILogger logger = Logger.OfContext();
        public CefResult<JToken> Evaluate(string request, int timeOut = 5000)
        {
            Stopwatch sw = Stopwatch.StartNew();
            if (tabConnection == null)
            {
                logger.LogWarning("CDP 连接尚未建立，Runtime.evaluate 直接返回未连接");
                return CefResult.Error<JToken>("(not connected)");
            }

            var response = tabConnection.Send(request, timeOut);
            if (response == null)
            {
                // 具体原因（未连接/发送失败/等待响应超时）已在 CdpConnection 内记录日志
                return CefResult.Error<JToken>("(timed out)");
            }

            // CDP 错误响应形如 {"id":1,"error":{"code":-32601,"message":"..."}}
            var cdpError = response["error"];
            if (cdpError != null)
            {
                var errorMessage = cdpError["message"]?.ToString();
                logger.LogWarning($"CDP 返回错误：{errorMessage}，request={request}");
                return CefResult.Error<JToken>(errorMessage);
            }

            // 原实现是 MasterDevs 的 CommandResponse.Result.Result，对应 ["result"]["result"]
            var remoteObject = response["result"]?["result"];
            if (remoteObject == null)
            {
                logger.LogWarning($"CDP 响应缺少结果对象：{response.ToString(Newtonsoft.Json.Formatting.None)}");
                return CefResult.Error<JToken>("(no cdp result)");
            }

            logger.LogInformation($"cdp evaluate 耗时：{sw.ElapsedMilliseconds}");
            if (remoteObject?["subtype"]?.ToString().Contains("error") == true)
            {
                return CefResult.Error<JToken>(remoteObject["description"]?.ToString());
            }

            var value = remoteObject["value"];
            if (value == null)
            {
                // 原实现在这里会 NullReferenceException，改为返回明确错误
                logger.LogWarning($"CDP 响应没有返回值：{response.ToString(Newtonsoft.Json.Formatting.None)}");
                return CefResult.Error<JToken>("(no cdp value)");
            }

            return CefResult.Ok<JToken>(value.ToString());
        }
    }
}
