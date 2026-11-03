using Newtonsoft.Json;

namespace II.RPA.Core.Contracts.Talker.Engine.Message.Action;

public class ParamAiEnhancedMessage
{
    [JsonProperty("elementId")]
    public string ElementId { get; set; } = string.Empty;

    [JsonProperty("retrieveSelector")]
    public string RetrieveSelector { get; set; } = string.Empty;

    [JsonProperty("retrieveTime")]
    public string RetrieveTime { get; set; } = string.Empty;

    [JsonProperty("version")]
    public string Version { get; set; } = string.Empty;

    [JsonProperty("repairSource")]
    public string RepairSource { get; set; } = string.Empty;
}
