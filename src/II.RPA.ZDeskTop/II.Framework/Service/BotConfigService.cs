using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace II.Framework.Service
{
    public class BotConfigService
    {
        private readonly HttpClient _httpClient;
        private readonly int _botServicePort = 15000;
        private readonly ILogger _logger;

        public BotConfigService(ILogger<BotConfigService> logger)
        {
            _logger = logger;
            _logger.LogInformation("Resolve BotConfigService");
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(1)
            };
            var portFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Z-Bot", "HostPort.json");
            if (File.Exists(portFile))
            {
                try
                {
                    var stringResult = File.ReadAllText(portFile);
                    var node = JObject.Parse(stringResult);
                    if (node != null && node["port"] != null)
                    {
                        _botServicePort = node["port"].Value<int>();
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggerExtensions.LogError(_logger, ex, portFile);
                }
            }
        }
        public async Task<bool> GetConfigOfAutoClose()
        {
            try
            {
                using (var response = await _httpClient.GetAsync($"http://127.0.0.1:{_botServicePort}/v1/settings/get"))
                {
                    using (var responseOK = response.EnsureSuccessStatusCode())
                    {
                        var data = await responseOK.Content.ReadAsStringAsync();
                        var dataJson = JObject.Parse(data);
                        var dataNode = dataJson["data"];
                        var KeepVirtualDesktopNode = dataNode["keepVirtualDesktop"];
                        var ret = KeepVirtualDesktopNode.Value<bool>();
                        return ret;
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggerExtensions.LogError(_logger, ex, "GetConfigOfAutoClose");
                return false;
            }

        }
    }
}

