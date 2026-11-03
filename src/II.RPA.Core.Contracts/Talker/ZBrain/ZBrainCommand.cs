namespace II.RPA.Core.Contracts.Talker.ZBrain;

public class ZBrainCommand : IPayload
{
    public ZBrainCommandType Type { get; set; }

    public IEnumerable<string> Arguments { get; set; } = Array.Empty<string>();
}
