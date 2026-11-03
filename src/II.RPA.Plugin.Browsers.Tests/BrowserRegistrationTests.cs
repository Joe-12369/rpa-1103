using II.RPA.Plugin.Browsers;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.Plugin.GUI.Commons.WindowMatch.Entities;
using II.RPA.WebBrowser.Common.MultiInstance;
using System.Collections.Generic;
using Xunit;

namespace II.RPA.Plugin.Browsers.Tests;

public sealed class BrowserRegistrationTests
{
    [Fact]
    public void Additional_static_variant_uses_its_window_type_not_its_dictionary_name()
    {
        var config = new MatchConfig
        {
            WindowType = WindowType.FireFox,
            ConfigType = MatchConfigType.Normal,
            ProductName = "custom-firefox",
            BrowserWndClassNames = new[] { "CustomMozillaClass" },
            TitleFeatures = System.Array.Empty<string>(),
            ProcessFeatures = new[] { "custom-firefox" }
        };

        PluginPackage.RegisterConfiguredBrowsers(new Dictionary<string, MatchConfig>
        {
            ["CustomerVariantNotListedInCode"] = config
        });

        var spec = BrowserSpecRepository.GetSpec(config.ConfigKey);
        Assert.NotNull(spec);
        Assert.Equal(WindowType.FireFox, spec.WindowType);
    }

    [Fact]
    public void Static_registration_does_not_claim_generic_chromium()
    {
        var config = new MatchConfig
        {
            WindowType = WindowType.Chromium,
            ConfigType = MatchConfigType.Normal,
            ProductName = "unconfigured-chromium",
            BrowserWndClassNames = new[] { "CustomChromium" },
            TitleFeatures = System.Array.Empty<string>(),
            ProcessFeatures = new[] { "unconfigured-chromium" }
        };

        PluginPackage.RegisterConfiguredBrowsers(new Dictionary<string, MatchConfig>
        {
            ["DynamicOnly"] = config
        });

        Assert.Null(BrowserSpecRepository.GetSpec(config.ConfigKey));
    }

    [Fact]
    public void Existing_generic_chromium_configuration_keeps_edge_host_fallback()
    {
        var config = CreateChromiumConfiguration("existing-chromium");

        PluginPackage.ActivateConfiguredChromiumBrowser(config);

        var spec = BrowserSpecRepository.GetSpec(config.ConfigKey);
        Assert.Equal(
            new[] { "II.RPA.NativeMessagingHost", "II.RPA.NativeMessagingHostEdge" },
            spec.NMHProcessNames);
    }

    [Fact]
    public void Newly_discovered_chromium_configuration_uses_generic_host_only()
    {
        var config = CreateChromiumConfiguration("discovered-chromium");

        PluginPackage.ActivateDiscoveredBrowser(config);

        var spec = BrowserSpecRepository.GetSpec(config.ConfigKey);
        Assert.Equal(new[] { "II.RPA.NativeMessagingHost" }, spec.NMHProcessNames);
    }

    private static MatchConfig CreateChromiumConfiguration(string productName) => new()
    {
        ConfigType = MatchConfigType.Normal,
        ProductName = productName,
        BrowserWndClassNames = new[] { "ChromiumWindow" },
        TitleFeatures = System.Array.Empty<string>(),
        ProcessFeatures = new[] { productName }
    };
}
