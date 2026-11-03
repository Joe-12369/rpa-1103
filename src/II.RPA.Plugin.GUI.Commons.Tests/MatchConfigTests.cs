using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.Plugin.GUI.Commons.WindowMatch.Entities;
using Newtonsoft.Json;
using Xunit;

namespace II.RPA.Plugin.GUI.Commons.Tests;

public class MatchConfigTests
{
    [Fact]
    public void WindowType_deserializes_from_enum_name()
    {
        var configuration = JsonConvert.DeserializeObject<MatchConfig>(
            "{\"windowType\":\"FireFox\",\"productName\":\"firefox\"}");

        Assert.NotNull(configuration);
        Assert.Equal(WindowType.FireFox, configuration.WindowType);
    }

    [Fact]
    public void WindowType_serializes_as_enum_name()
    {
        var configuration = new MatchConfig
        {
            WindowType = WindowType.Web360X,
            ProductName = "360ChromeX"
        };

        var json = JsonConvert.SerializeObject(configuration);

        Assert.Contains("\"windowType\":\"Web360X\"", json);
    }
}
