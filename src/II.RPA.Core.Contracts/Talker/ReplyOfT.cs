namespace II.RPA.Core.Contracts.Talker;

public class Reply<T> : Reply
{
    public T? Body { get; set; }
}
