using Newtonsoft.Json;
using System.Collections.Generic;

namespace II.RPA.RuntimeHost.Configs
{
    public class UiContextActionsConfig
    {
        [JsonProperty("RequireUiContextActions")]
        public List<string> RequireUiContextActions { get; set; } = new List<string>();
    }
}
