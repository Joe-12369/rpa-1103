namespace II.RPA.Core.Contracts.Talker.ZBrain;

public class FrameRequest : FrameMessage<ZBrainRequest>
{
    public FrameRequest()
    {
        Type = FrameType.REQUEST;
    }
}
