namespace II.RPA.Communication.Options;

public sealed class TimeoutOptions
{
    public TimeSpan DefaultRequestTimeout { get; set; } = System.Threading.Timeout.InfiniteTimeSpan;
}
