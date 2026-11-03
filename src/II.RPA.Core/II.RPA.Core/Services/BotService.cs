using II.RPA.Core.Log;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Services
{
    public class BotService
    {
        private readonly HttpClient _httpClient;
        private readonly int _botServicePort = 15000;
        private readonly ILogger _logger;

        public static BotService Singleton 
        { 
            get; 
            private set; 
        }
        public static void BuildSingleton(ILogger logger)
        {
            Singleton ??= new BotService(logger);
        }
        private BotService(ILogger logger)
        {
            _logger = logger;
            _logger.LogInfo("Resolve BotService");
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
                    _logger.LogError(portFile, ex);
                }
            }
        }
        public async Task<bool> GetConfigOfAutoClose()
        {
            try
            {
                using var response = await _httpClient.GetAsync($"http://127.0.0.1:{_botServicePort}/v1/settings/get");
                using var responseOK = response.EnsureSuccessStatusCode();
                var data = await responseOK.Content.ReadAsStringAsync();
                var dataJson = JObject.Parse(data);
                var dataNode = dataJson["data"];
                var KeepVirtualDesktopNode = dataNode["keepVirtualDesktop"];
                var ret = KeepVirtualDesktopNode.Value<bool>();
                return ret;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetConfigOfAutoClose", ex);
                return false;
            }

        }
        public async Task<string> GetConfigOfStopKey()
        {
            try
            {
                using var response = await _httpClient.GetAsync($"http://127.0.0.1:{_botServicePort}/v1/settings/get");
                using var responseOK = response.EnsureSuccessStatusCode();
                var data = await responseOK.Content.ReadAsStringAsync();
                var dataJson = JObject.Parse(data);
                var dataNode = dataJson["data"];
                var KeepVirtualDesktopNode = dataNode["jobTernimateShortcut"];
                var ret = KeepVirtualDesktopNode.Value<string>();
                return ret;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetConfigOfStopKey", ex);
                return "SHIFT + CTRL + X";
            }

        }
    }
}

