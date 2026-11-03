using Xunit;

namespace II.RPA.WebBrowser.Common.Tests;

public sealed class BrowserPackageTests
{
    [Fact]
    public void Every_browser_technology_package_uses_its_logical_directory_name()
    {
        II.RPA.Plugin.Abstractions.IPluginPackage[] packages =
        {
            new II.RPA.Plugin.Browsers.PluginPackage(),
            new II.RPA.Plugin.IE.PluginPackage(),
            new II.RPA.Plugin.ZExplorer.PluginPackage()
        };
        var expectedNames = new[]
        {
            "Browsers", "GUIIE", "GUIZExplorer"
        };

        Assert.Equal(expectedNames, packages.Select(package => package.PackageName));
    }

    [Fact]
    public void Browser_initialization_is_owned_by_the_public_package_entry_without_a_module_entry()
    {
        var packageTypes = new[]
        {
            typeof(II.RPA.Plugin.Browsers.PluginPackage),
            typeof(II.RPA.Plugin.IE.PluginPackage),
            typeof(II.RPA.Plugin.ZExplorer.PluginPackage)
        };
        Assert.All(packageTypes, type =>
        {
            Assert.Null(type.Assembly.GetType(type.Assembly.GetName().Name + ".BrowserModule"));
            Assert.NotNull(type.GetMethod(
                "InitializePackageAsync",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.DeclaredOnly));
        });
    }
}
