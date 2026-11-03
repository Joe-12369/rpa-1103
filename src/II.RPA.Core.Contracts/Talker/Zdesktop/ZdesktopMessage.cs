using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace II.RPA.Core.Contracts.Talker.Zdesktop;

public class ZdesktopMessage
{
    [JsonProperty("action")]
    public virtual string Action { get; set; } = string.Empty;

    [JsonProperty("requestId")]
    public virtual string RequestId
    {
        get => ResponseFor ?? string.Empty;
        set => ResponseFor = value;
    }

    [JsonIgnore]
    public string? ResponseFor { get; set; }

    [JsonProperty("payload")]
    public JObject PayloadJobject { get; set; } = new();

    public static ZdesktopMessage Parse(string rawData)
    {
        return JsonConvert.DeserializeObject<ZdesktopMessage>(rawData)!;
    }

    public static string Serialize(ZdesktopMessage message)
    {
        return JsonConvert.SerializeObject(
            message,
            new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore,
            });
    }
}
