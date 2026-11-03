using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace II.RPA.Core.Contracts.Talker.Engine.Message;

public class EngineMessage : IWebSocketMessage
{
    private object? _payload;

    public const string BotSource = "Bot";

    [JsonProperty("action")]
    public virtual string Action { get; set; } = string.Empty;

    [JsonProperty("requestId")]
    public virtual string RequestId
    {
        get => ResponseFor ?? string.Empty;
        set => ResponseFor = value;
    }

    [JsonProperty("payload")]
    public JObject PayloadJobject { get; set; } = new();

    [JsonIgnore]
    public string? ResponseFor { get; set; }

    [JsonIgnore]
    public string Raw { get; set; } = string.Empty;

    [JsonIgnore]
    public string MessageType
    {
        get => Action;
        set => Action = value;
    }

    public EngineMessage(string action)
    {
        RequestId = NewRequestId();
        Action = action;
    }

    public T Payload<T>()
    {
        if (_payload is T payload)
        {
            return payload;
        }

        var result = PayloadJobject.ToObject<T>()!;
        _payload = result;
        return result;
    }

    public static string Serialize(EngineMessage message)
    {
        return JsonConvert.SerializeObject(
            message,
            new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore,
            });
    }

    public static EngineMessage Parse(string rawJson)
    {
        var result = JsonConvert.DeserializeObject<EngineMessage>(rawJson)!;
        result.Raw = rawJson;
        return result;
    }

    private static string NewRequestId()
    {
        var ticksOffset = DateTime.Now.Ticks - DateTime.MinValue.Ticks;
        var bytes = BitConverter.GetBytes(ticksOffset);
        Thread.Sleep(0);
        return BitConverter.ToString(bytes.Reverse().ToArray()).Replace("-", string.Empty);
    }
}
