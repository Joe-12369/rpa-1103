using Newtonsoft.Json;

namespace II.RPA.Core.Contracts.Talker.Engine.Message.Action;

public class PauseMessage : EngineMessage<Pause>
{
    public PauseMessage()
        : base(string.Empty, new Pause())
    {
    }

    public PauseMessage(string action, Pause content)
        : base(action, content)
    {
    }
}

public class Pause
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("packagePath")]
    public string? PackagePath { get; set; }
}
