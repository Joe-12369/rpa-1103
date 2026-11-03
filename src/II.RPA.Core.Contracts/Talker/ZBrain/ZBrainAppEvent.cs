namespace II.RPA.Core.Contracts.Talker.ZBrain;

public enum ZBrainAppEventType
{
    Unknown = 0,
    Loading = 1,
    Ready = 2,
    Broken = 3,
}

public class ZBrainAppEvent : IPayload
{
    public ZBrainAppEventType Type { get; set; }
}
