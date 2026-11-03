using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace II.RPA.Plugin.ZExplorer.Cef
{
    public sealed class ChromeDebugProtocolClientConnector : IDisposable
    {
        public CefResult<CefUnit> ConnectionStartupResult
        {
            get
            {
                return this.connectionStartupResult;
            }
        }

        public CefFramework? Framework
        {
            get
            {
                return this.framework;
            }
        }

        public int Port
        {
            get
            {
                return this.port;
            }
        }      
        public TabPage[] TabPages { get; set; }

        public ChromeDebugProtocolClientConnector(int port)
        {
            this.port = port;
            this.tabDiscoverer = new HttpTabDiscoverer(port);
            this.coalescingExecutor = new CoalescingExecutor(new Action(this.UpdateTabEndpointsUnprotected));
            this.endpoints = new ConcurrentDictionary<string, Lazy<ChromeSessionClient>>(StringComparer.OrdinalIgnoreCase);
            UpdateTabEndpoints();
            //GetFrameworkName();
        }

        public IEnumerable<ChromeSessionClient> Endpoints
        {
            get
            {
                return (from x in this.endpoints.Values
                        select x.Value).Distinct();
            }
        }

        public ChromeSessionClient GetEndpointById(string id)
        {
            this.endpoints.TryGetValue(id, out Lazy<ChromeSessionClient> lazy);
            if (lazy == null)
            {
                return null;
            }
            return lazy.Value;
        }

        public ChromeVersion Version { get; private set; }

        public void Dispose()
        {
            this.coalescingExecutor.Close();
            IDisposable disposable = this.tabDiscoverer as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
            List<ChromeSessionClient> list = (from x in this.endpoints.Values
                                              where x.IsValueCreated
                                              select x.Value).ToList();
            this.endpoints.Clear();
            foreach (ChromeSessionClient chromeDebugProtocolClient in list)
            {
                chromeDebugProtocolClient.Dispose();
            }
        }


        internal void UpdateTabEndpoints()
        {
            this.coalescingExecutor.TryExecute();
            this.connectionStartupResult = CefResult.Unit();
        }

        internal void ReverseKnownEndpoints()
        {
            List<KeyValuePair<string, Lazy<ChromeSessionClient>>> list = this.endpoints.Reverse().ToList();
            this.endpoints.Clear();
            list.ForEach(delegate (KeyValuePair<string, Lazy<ChromeSessionClient>> x)
            {
                this.endpoints.TryAdd(x.Key, x.Value);
            });
        }

        //internal bool HasOccupiedEndpoints()
        //{
        //    IEnumerable<string> first = (from e in this.tabDiscoverer.DiscoverEndpoints()
        //                                 where e.WebSocketDebuggerUrl == null
        //                                 select e.Id).ToArray<string>();
        //    string[] second = (from e in this.Endpoints
        //                       where e.Connected
        //                       select e.Id).ToArray<string>();
        //    return first.Except(second).Any<string>();
        //}

        private void GetFrameworkName()
        {
            CefFramework result = CefFramework.Generic;
            var endpoints = this.endpoints.FirstOrDefault().Value.Value;

            if (IsElectronEndpoint(endpoints))
            {
                result = CefFramework.Electron;
            }
            CefResult<JToken> cefResult = endpoints.Evaluate("typeof require('nw.gui').Window !== 'undefined'");
            if (cefResult.IsSuccess && cefResult.Success.Value<bool>() && cefResult.Success != null && !cefResult.Success.Value<bool>("wasThrown") && cefResult.Success.Value<JObject>("result").Value<bool>("value"))
            {
                result = CefFramework.NWJs;
            }
            CefResult<JToken> cefResult2 = endpoints.Evaluate("typeof window.fin !== 'undefined' && typeof window.fin.desktop !== 'undefined' && typeof window.fin.desktop.getVersion === 'function' && typeof window.fin.desktop.getVersion() == 'string'");
            if (cefResult2.IsSuccess && cefResult2.Success != null && !cefResult2.Success.Value<bool>("wasThrown") && cefResult2.Success.Value<JObject>("result").Value<bool>("value"))
            {
                result = CefFramework.OpenFin;
            }
            CefResult<JToken> cefResult3 = endpoints.Evaluate("typeof window.cefsharp_CreatePromise === 'function'");
            if (cefResult3.IsSuccess && cefResult3.Success != null && !cefResult3.Success.Value<bool>("wasThrown") && cefResult3.Success.Value<JObject>("result").Value<bool>("value"))
            {
                result = CefFramework.CefSharp;
            }
            CefResult<JToken> cefResult4 = endpoints.Evaluate("typeof window.cefSharp !== 'undefined'");
            if (cefResult4.IsSuccess && cefResult4.Success != null && !cefResult4.Success.Value<bool>("wasThrown") && cefResult4.Success.Value<JObject>("result").Value<bool>("value"))
            {
                result = CefFramework.CefSharp;
            }
            CefResult<JToken> cefResult5 = endpoints.Evaluate("typeof window.cefQuery === 'function'");
            if (cefResult5.IsSuccess && cefResult5.Success != null && !cefResult5.Success.Value<bool>("wasThrown") && cefResult5.Success.Value<JObject>("result").Value<bool>("value"))
            {
                result = CefFramework.JCEF;
            }
            CefResult<JToken> cefResult6 = endpoints.Evaluate("typeof window.chrome.webview.postMessage === 'function'");
            if (cefResult6.IsSuccess && cefResult6.Success != null && !cefResult6.Success.Value<bool>("wasThrown") && cefResult6.Success.Value<JObject>("result").Value<bool>("value"))
            {
                result = CefFramework.WebView2;
            }
            CefResult<JToken> cefResult7 = endpoints.Evaluate("typeof gws_wizbind.bind === 'function'");
            if (cefResult7.IsSuccess && cefResult7.Success != null && !cefResult7.Success.Value<bool>("wasThrown") && cefResult7.Success.Value<JObject>("result").Value<bool>("value"))
            {
                result = CefFramework.DotNetBrowser;
            }

            framework = result;
        }

        private static bool IsElectronEndpoint(ChromeSessionClient endpoint)
        {
            CefResult<JToken> cefResult = endpoint.Evaluate("typeof require('electron').screen !== 'undefined'");
            bool flag = cefResult.IsSuccess && cefResult.Success != null && !cefResult.Success.Value<bool>("wasThrown") && cefResult.Success.Value<JObject>("result").Value<bool>("value");
            if (flag)
            {
                return flag;
            }
            CefResult<JToken> cefResult2 = endpoint.Evaluate("typeof process.versions !== 'undefined' && typeof process.versions.electron !== 'undefined'");
            bool flag2 = cefResult2.IsSuccess && cefResult2.Success != null && !cefResult2.Success.Value<bool>("wasThrown") && cefResult2.Success.Value<JObject>("result").Value<bool>("value");
            if (flag2)
            {
                return flag2;
            }
            CefResult<JToken> cefResult3 = endpoint.Evaluate("typeof navigator === 'object' && typeof navigator.userAgent === 'string' && navigator.userAgent.indexOf('Electron') >= 0");
            return cefResult3.IsSuccess && cefResult3.Success.Value<bool>() && !cefResult3.Success.Value<bool>("wasThrown") && cefResult3.Success.Value<JObject>("result").Value<bool>("value");
        }

        private void UpdateTabEndpointsUnprotected()
        {
            DiscoverDebuggerProtocolVersion();

            TabPage[] tabs = this.tabDiscoverer.DiscoverEndpoints().ToArray();
            TabPages = tabs;
            //FilterTabEndpoints(ref tabs);

            foreach (var tabPage in tabs.Where(t => t.WebSocketDebuggerUrl != null))
            {
                var lazyClient = endpoints.GetOrAdd(tabPage.Id, id =>
                {
                    logger.LogDebug("Adding new endpoint: {0}", id);
                    return new Lazy<ChromeSessionClient>(() => new ChromeSessionClient(tabPage), LazyThreadSafetyMode.ExecutionAndPublication);
                });

                if (!lazyClient.IsValueCreated)
                {
                    logger.LogTrace("New endpoint created for: {0}", tabPage.Id);
                }
            }

            foreach (var tabPage in tabs)
            {
                if (endpoints.TryGetValue(tabPage.Id, out var lazyClient) && lazyClient.IsValueCreated)
                {
                    lazyClient.Value.Update(tabPage);
                }
            }

            foreach (var endpointId in endpoints.Keys.Except(tabs.Select(t => t.Id)))
            {
                if (endpoints.TryRemove(endpointId, out var lazyClient))
                {
                    string status = lazyClient.IsValueCreated ? "unused" : "active";
                    logger.LogDebug($"Removing {status} endpoint: {endpointId}");

                    if (lazyClient.IsValueCreated)
                    {
                        lazyClient.Value.Dispose();
                    }
                }
            }
        }

        private void DiscoverDebuggerProtocolVersion()
        {
            if (this.Version == null)
            {
                this.Version = this.tabDiscoverer.DiscoverChromeVersion();
                if (this.Version != null)
                {
                    logger.LogInformation("Protocol Version: " + this.Version.ProtocolVersion);
                }
            }
        }

        private void FilterTabEndpoints(ref TabPage[] tabs)
        {
            bool devToolsTabsFound = false;

            foreach (TabPage tabPage in tabs.Where(tab => tab.PageUrl?.StartsWith("chrome-devtools://") == true))
            {
                devToolsTabsFound = true;
                logger.LogDebug($"DevTools connected to tab {tabPage.Id}, trying to connect and close");

                // 尝试关闭 DevTools 标签页
                using (var client = new ChromeDebugProtocolClient(tabPage))
                {
                    var result = client.Evaluate("window.close()");
                    if (result.IsError)
                    {
                        logger.LogDebug($"Failed to close: {result.Error}");
                    }
                }
            }
            if (devToolsTabsFound)
            {
                tabs = this.tabDiscoverer.DiscoverEndpoints().ToArray();
            }
        }

        private static readonly ILogger logger = Logger.OfContext();

        private readonly HttpTabDiscoverer tabDiscoverer;

        private readonly ConcurrentDictionary<string, Lazy<ChromeSessionClient>> endpoints;

        private CefFramework? framework;

        private readonly CoalescingExecutor coalescingExecutor;

        private CefResult<CefUnit> connectionStartupResult;

        private int port;
    }
}
