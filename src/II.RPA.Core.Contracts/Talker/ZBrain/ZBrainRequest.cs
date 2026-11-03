using Google.Protobuf.WellKnownTypes;

namespace II.RPA.Core.Contracts.Talker.ZBrain;

public class ZBrainRequest : IPayload
{
    public string Method { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public Dictionary<string, string> Headers { get; set; } = new();

    public string Body { get; set; } = string.Empty;
}

public class ZBrainProxyRequest : ZBrainRequest
{
    public ZBrainProxyRequest(Any frame)
    {
        Proxy = frame;
    }

    public Any Proxy { get; }
}
