using Newtonsoft.Json.Linq;

namespace II.RPA.Core.Contracts.Talker.Engine;

public class EngineCommand : IPayload
{
    public EngineCommandType Type { get; set; }

    public JArray? Argumets { get; set; }

    public JObject? KeyValueArgumets { get; set; }
}
