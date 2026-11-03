using II.RPA.Plugin.Abstractions;
using Xunit;

namespace II.RPA.Plugin.Abstractions.Tests;

public sealed class PluginRouteTests
{
    [Fact]
    public void Parse_SplitsAtFirstSeparator()
    {
        var route = PluginRoute.Parse("Element.Capture");
        Assert.Equal("Element", route.PackageName);
        Assert.Equal("Capture", route.AbilityName);
        Assert.Equal("Element.Capture", route.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Element")]
    [InlineData(".Capture")]
    [InlineData("Element.")]
    [InlineData("Element.Capture.More")]
    [InlineData(" Element.Capture")]
    [InlineData("Element.Capture ")]
    public void Parse_RejectsInvalidFullRoute(string? value)
    {
        Assert.Throws<ArgumentException>(() => PluginRoute.Parse(value!));
    }

    [Theory]
    [InlineData(null, "Capture")]
    [InlineData("", "Capture")]
    [InlineData(" Element", "Capture")]
    [InlineData("Element ", "Capture")]
    [InlineData("Element.Sub", "Capture")]
    [InlineData("Element", null)]
    [InlineData("Element", "")]
    [InlineData("Element", " Capture")]
    [InlineData("Element", "Capture ")]
    [InlineData("Element", "Capture.More")]
    public void Constructor_RejectsInvalidSegments(string? packageName, string? abilityName)
    {
        Assert.Throws<ArgumentException>(() => new PluginRoute(packageName!, abilityName!));
    }

    [Fact]
    public void EqualityAndHashCode_IgnoreCase()
    {
        var canonical = new PluginRoute("Element", "Capture");
        var differentCase = new PluginRoute("element", "capture");
        Assert.Equal(canonical, differentCase);
        Assert.True(canonical == differentCase);
        Assert.False(canonical != differentCase);
        Assert.Equal(canonical.GetHashCode(), differentCase.GetHashCode());
    }

    [Fact]
    public void Equality_DistinguishesDifferentSegments()
    {
        var route = new PluginRoute("Element", "Capture");
        Assert.NotEqual(route, new PluginRoute("Element", "SetText"));
        Assert.NotEqual(route, new PluginRoute("Mouse", "Capture"));
    }
}
