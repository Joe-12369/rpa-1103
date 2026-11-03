using Newtonsoft.Json;

namespace II.RPA.Core.Contracts.Talker.Zdesktop.Message;

public class ZdesktopMessage<T> : ZdesktopMessage
{
    [JsonProperty("payload")]
    public T Content { get; set; }

    public ZdesktopMessage(string action, T content)
        : base(action)
    {
        Content = content;
    }
}
