using II.RPA.Plugin.Abstractions;
using Xunit;

namespace II.RPA.Plugin.Abstractions.Tests;

public sealed class PluginExecutionContextTests
{
    [Fact]
    public void Constructor_PreservesProvidedValues()
    {
        var route = new PluginRoute("Element", "Capture");
        var context = new PluginExecutionContext(route, "{}", "invocation-1");
        Assert.Same(route, context.Route);
        Assert.Equal("{}", context.JsonParam);
        Assert.Equal("invocation-1", context.InvocationId);
    }

    [Fact]
    public void Constructor_AllowsEmptyJsonParameter()
    {
        var context = new PluginExecutionContext(new PluginRoute("Element", "Capture"), string.Empty, "invocation-1");
        Assert.Equal(string.Empty, context.JsonParam);
    }

    [Fact]
    public void Constructor_RejectsNullRoute()
    {
        Assert.Throws<ArgumentNullException>(() => new PluginExecutionContext(null!, "{}", "invocation-1"));
    }

    [Fact]
    public void Constructor_RejectsNullJsonParameter()
    {
        Assert.Throws<ArgumentNullException>(() => new PluginExecutionContext(new PluginRoute("Element", "Capture"), null!, "invocation-1"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" invocation-1")]
    [InlineData("invocation-1 ")]
    public void Constructor_RejectsInvalidInvocationId(string? invocationId)
    {
        Assert.Throws<ArgumentException>(() => new PluginExecutionContext(new PluginRoute("Element", "Capture"), "{}", invocationId!));
    }

    [Fact]
    public void PublicState_IsReadOnly()
    {
        Assert.False(typeof(PluginExecutionContext).GetProperty(nameof(PluginExecutionContext.Route))!.CanWrite);
        Assert.False(typeof(PluginExecutionContext).GetProperty(nameof(PluginExecutionContext.JsonParam))!.CanWrite);
        Assert.False(typeof(PluginExecutionContext).GetProperty(nameof(PluginExecutionContext.InvocationId))!.CanWrite);
    }
}
