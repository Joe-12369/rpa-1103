namespace II.RPA.Core.Contracts.Talker;

public class FrameMessage<T> : FrameMessage where T : class, IPayload
{
    public T? Payload
    {
        get => AnonymousPayload as T;
        set => AnonymousPayload = value;
    }
}
