using Newtonsoft.Json;

namespace II.RPA.Core.Contracts.Talker.Engine.Message;

public class EngineMessage<T> : EngineMessage
{
    [JsonProperty("payload")]
    public T Content { get; set; }

    public EngineMessage(string action, T content)
        : base(action)
    {
        Content = content;
    }
}
