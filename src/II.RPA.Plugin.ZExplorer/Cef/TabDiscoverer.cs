using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using II.RPA.WinAutomation.CommonUtility;

namespace II.RPA.Plugin.ZExplorer.Cef
{
    internal sealed class TabDiscoverer : IDisposable
    {
        /// <summary>等待背景页目标出现的上限。</summary>
        private const int DiscoverTimeoutMs = 15000;

        private ILogger logger = Logger.OfContext();
        public TabDiscoverer()
        {
            this.client = new WebClient();

        }
        /// <summary>
        /// 等待 /json/list 中出现 zexplorer_background 目标。
        /// 原实现是 do/while 无限轮询：背景页一直不出现时会持续下载，调用方永久阻塞；
        /// 这里加上等待上限，超时抛出明确异常并记录已等待时长与轮询次数。
        /// </summary>
        public TabPage DiscoverBackgroundPage(int port, string host = "127.0.0.1")
        {
            string value;
            var webUrl = new Uri(string.Format("http://{0}:{1}/json/list", host, port));
            var stopwatch = Stopwatch.StartNew();
            int round = 0;
            try
            {
                do
                {
                    if (stopwatch.ElapsedMilliseconds > DiscoverTimeoutMs)
                    {
                        logger.LogError($"等待 ZExplorer 背景页目标超时（{DiscoverTimeoutMs}ms，已轮询 {round} 次）：{webUrl}");
                        throw new ZExplorerException(eErrorMessageType.Error_ZEXPLORERNOTFOUND);
                    }

                    var byteData = this.client.DownloadData(webUrl);
                    value = Encoding.UTF8.GetString(byteData);
                    round++;
                    if (round == 1)
                    {
                        logger.LogInformation($"discover tabs:{value}");
                    }
                    else
                    {
                        logger.LogDebug($"discover tabs(第{round}次):{value}");
                    }
                }
                while (!value.Contains("zexplorer_background"));
            }
            catch (WebException innerException)
            {
                throw new Exception(string.Format("Failed to download {0}", webUrl), innerException);
            }
            var pages = JsonConvert.DeserializeObject<List<TabPage>>(value);
            //var backgroundPage = pages.FirstOrDefault(p =>
            //{
            //    return p.PageUrl?.EndsWith("zexplorer_background.html") == true || p.Title?.Equals("zexplorer_background") == true || p.FaviconUrl?.EndsWith("zexplorer_background.ico") == true;
            //});
            return pages.FirstOrDefault();//已经隔离背景页，只会获取一个tabpage
        }
        public void Dispose()
        {
            this.client.Dispose();
        }

        private readonly WebClient client;
    }
}
