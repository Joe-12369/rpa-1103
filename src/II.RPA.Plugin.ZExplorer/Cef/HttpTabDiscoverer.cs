using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace II.RPA.Plugin.ZExplorer.Cef
{
    internal sealed class HttpTabDiscoverer : IDisposable
    {
        private ILogger logger = Logger.OfContext();
        public HttpTabDiscoverer(int port, string host = "127.0.0.1")
        {
            this.webUrl = new Uri(string.Format("http://{0}:{1}/json/list", host, port));
            this.client = new WebClient();

        }

        public IEnumerable<TabPage> DiscoverEndpoints()
        {
            string value;
            try
            {
                do
                {
                    Thread.Sleep(500);
                    var byteData = this.client.DownloadData(this.webUrl);
                    value = Encoding.UTF8.GetString(byteData);
                    logger.LogInformation($"discover tabs:{value}");
                }
                while (!value.Contains("zexplorer_background"));
            }
            catch (WebException innerException)
            {
                throw new Exception(string.Format("Failed to download {0}", this.webUrl), innerException);
            }
            return from e in JsonConvert.DeserializeObject<TabPage[]>(value)
                   where !NonTabTypes.Contains(e.Type)
                   select e;
        }

        public ChromeVersion DiscoverChromeVersion()
        {
            ChromeVersion result;
            try
            {
                Uri address = new Uri(this.webUrl, "/json/version");
                result = JsonConvert.DeserializeObject<ChromeVersion>(this.client.DownloadString(address));
            }
            catch (WebException)
            {
                result = null;
            }
            catch (JsonSerializationException)
            {
                result = new ChromeVersion();
            }
            return result;
        }

        public void Dispose()
        {
            this.client.Dispose();
        }

        private static string[] NonTabTypes = new string[]
        {
              "service_worker",
              "shared_worker"
        };

        private readonly WebClient client;

        private readonly Uri webUrl;

    }
}
