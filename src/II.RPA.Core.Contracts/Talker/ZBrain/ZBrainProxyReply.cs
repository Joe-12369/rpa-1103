using Google.Protobuf.WellKnownTypes;

namespace II.RPA.Core.Contracts.Talker.ZBrain;

public class ZBrainProxyReply : IPayload
{
    public ZBrainProxyReply(Any frame)
    {
        Proxy = frame;
    }

    public Any Proxy { get; }
}
