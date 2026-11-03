using Newtonsoft.Json;
using System.Collections.Generic;

namespace II.RPA.Core.Server.Models
{
    public class BootSetting
    {
        [JsonProperty("client")]
        public string Client {  get; set; }
        [JsonProperty("boots")]
        public List<Boot> Boots { get; set; }
    }
    public class Boot
    {
        [JsonProperty("boot")]
        public string BootName {  get; set; }
        [JsonProperty("ip")]
        public string IP { get; set; }
        [JsonProperty("port")]
        public int Port { get; set; }
    }
}
