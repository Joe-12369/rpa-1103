namespace II.RPA.Core.Contracts.Talker.ZBrain;

public class FrameCommand : FrameMessage<ZBrainCommand>
{
    public FrameCommand()
    {
        Type = FrameType.COMMAND;
    }
}
