namespace II.RPA.Core.Contracts.Talker;

public class FrameMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ReplyId { get; set; } = Guid.Empty;

    public FrameType Type { get; set; }

    public int Code { get; set; }

    public string Message { get; set; } = string.Empty;

    public bool Buffer { get; set; }

    public IPayload? AnonymousPayload { get; set; }

    public bool Is<T>() where T : IPayload
    {
        return AnonymousPayload is T;
    }
}
