using II.RPA.Communication.Hosting;
using II.RPA.Communication.Options;
using II.RPA.Communication.Timeout;
using Xunit;

namespace II.RPA.Communication.Tests;

public class TimeoutResolverTests
{
    [Fact]
    public void DefaultTimeout_IsInfinite()
    {
        var resolver = new TimeoutResolver(new TimeoutOptions());
        Assert.Equal(System.Threading.Timeout.InfiniteTimeSpan, resolver.Resolve(null));
    }

    [Fact]
    public void ExplicitInfiniteTimeout_OverridesFiniteDefault()
    {
        var resolver = new TimeoutResolver(new TimeoutOptions { DefaultRequestTimeout = TimeSpan.FromSeconds(30) });
        Assert.Equal(System.Threading.Timeout.InfiniteTimeSpan,
            resolver.Resolve(System.Threading.Timeout.InfiniteTimeSpan));
        Assert.Equal(TimeSpan.FromSeconds(2), resolver.Resolve(TimeSpan.FromSeconds(2)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void InvalidTimeout_IsRejected(int milliseconds)
    {
        var resolver = new TimeoutResolver(new TimeoutOptions());
        Assert.Throws<ArgumentOutOfRangeException>(() => resolver.Resolve(TimeSpan.FromMilliseconds(milliseconds)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimeoutResolver(
            new TimeoutOptions { DefaultRequestTimeout = TimeSpan.FromMilliseconds(milliseconds) }));
    }

    [Fact]
    public async Task CreateCallOptions_HandlesInfiniteAndFiniteTimeouts()
    {
        await using var client = new GrpcClient(new ClientOptions());
        using var cancellation = new CancellationTokenSource();
        var options = client.CreateCallOptions(cancellationToken: cancellation.Token);
        Assert.Null(options.Deadline);
        Assert.Equal(cancellation.Token, options.CancellationToken);
        Assert.Null(client.CreateCallOptions(System.Threading.Timeout.InfiniteTimeSpan).Deadline);
        var before = DateTime.UtcNow;
        var finite = client.CreateCallOptions(TimeSpan.FromSeconds(2));
        Assert.InRange(finite.Deadline!.Value, before.AddSeconds(2), DateTime.UtcNow.AddSeconds(2));
    }
}
