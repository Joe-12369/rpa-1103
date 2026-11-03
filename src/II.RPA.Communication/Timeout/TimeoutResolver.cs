using II.RPA.Communication.Options;

namespace II.RPA.Communication.Timeout;

public sealed class TimeoutResolver
{
    private readonly TimeSpan _defaultTimeout;

    public TimeoutResolver(TimeoutOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _defaultTimeout = Validate(options.DefaultRequestTimeout, nameof(options.DefaultRequestTimeout));
    }

    public TimeSpan Resolve(TimeSpan? overrideTimeout)
    {
        return overrideTimeout.HasValue
            ? Validate(overrideTimeout.Value, nameof(overrideTimeout))
            : _defaultTimeout;
    }

    private static TimeSpan Validate(TimeSpan timeout, string parameterName)
    {
        if (timeout != System.Threading.Timeout.InfiniteTimeSpan && timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Timeout must be greater than zero or InfiniteTimeSpan.");
        }

        return timeout;
    }
}
