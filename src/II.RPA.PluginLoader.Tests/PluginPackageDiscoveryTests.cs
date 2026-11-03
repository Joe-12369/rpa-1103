using II.RPA.PluginLoader;
using Xunit;

namespace II.RPA.PluginLoader.Tests;

public sealed class PluginPackageDiscoveryTests : IDisposable
{
    private readonly string runtimeDirectory = Path.Combine(
        Path.GetTempPath(),
        "II.RPA.PluginLoader.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void Discover_reads_only_configured_first_level_package_directories()
    {
        CreatePackage("Element", "II.RPA.Plugin.GUI.dll");
        Directory.CreateDirectory(Path.Combine(runtimeDirectory, "Dependencies"));
        CreatePackage(Path.Combine("Container", "Nested"), "Nested.Plugin.dll");

        var package = Assert.Single(new PluginPackageDiscovery().Discover(runtimeDirectory));

        Assert.Equal("Element", package.PackageName);
        Assert.Equal(Path.Combine(runtimeDirectory, "Element"), package.PackageDirectory);
        Assert.Equal(
            Path.Combine(runtimeDirectory, "Element", "II.RPA.Plugin.GUI.dll"),
            package.AssemblyFilePath);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"fileName\":\"\"}")]
    [InlineData("not-json")]
    public void Discover_rejects_invalid_config(string config)
    {
        CreateConfiguredDirectory("Element", config);

        Assert.ThrowsAny<Exception>(() => new PluginPackageDiscovery().Discover(runtimeDirectory));
    }

    [Theory]
    [InlineData("../Outside.dll")]
    [InlineData("sub/Plugin.dll")]
    [InlineData("C:\\Plugin.dll")]
    public void Discover_rejects_file_name_that_is_not_a_simple_file_name(string fileName)
    {
        CreateConfiguredDirectory("Element", $"{{\"fileName\":\"{fileName.Replace("\\", "\\\\")}\"}}");

        Assert.ThrowsAny<Exception>(() => new PluginPackageDiscovery().Discover(runtimeDirectory));
    }

    [Fact]
    public void Discover_rejects_missing_main_assembly()
    {
        CreateConfiguredDirectory("Element", "{\"fileName\":\"Missing.dll\"}");

        Assert.ThrowsAny<Exception>(() => new PluginPackageDiscovery().Discover(runtimeDirectory));
    }

    [Fact]
    public void LoadPackages_uses_exact_assembly_root_PluginPackage_entry()
    {
        var assemblyPath = typeof(PluginPackageDiscoveryTests).Assembly.Location;
        var fileName = Path.GetFileName(assemblyPath);
        var directory = CreateConfiguredDirectory("Element", $"{{\"fileName\":\"{fileName}\"}}");
        File.Copy(assemblyPath, Path.Combine(directory, fileName));

        var package = Assert.Single(new PluginPackageDiscovery().LoadPackages(runtimeDirectory));

        Assert.Equal("Element", package.PackageName);
        Assert.IsType<PluginPackage>(package);
    }

    [Fact]
    public void LoadPackages_rejects_dll_file_name_that_differs_from_assembly_name()
    {
        var assemblyPath = typeof(PluginPackageDiscoveryTests).Assembly.Location;
        var directory = CreateConfiguredDirectory("Element", "{\"fileName\":\"Renamed.dll\"}");
        File.Copy(assemblyPath, Path.Combine(directory, "Renamed.dll"));

        Assert.ThrowsAny<Exception>(() => new PluginPackageDiscovery().LoadPackages(runtimeDirectory));
    }

    [Fact]
    public void LoadPackages_discovers_every_non_web_operator_module_as_a_package()
    {
        var modules = new Dictionary<string, Type>
        {
            ["GUICV"] = typeof(II.RPA.Plugin.CV.PluginPackage),
            ["GUIJava"] = typeof(II.RPA.Plugin.Java.PluginPackage),
            ["GUIJavaAlter"] = typeof(II.RPA.Plugin.JavaAlter.PluginPackage),
            ["GUISAP"] = typeof(II.RPA.Plugin.SAP.PluginPackage),
            ["WdtErp"] = typeof(II.RPA.Plugin.WdtErp.PluginPackage),
            ["GUIWindows"] = typeof(II.RPA.Plugin.Windows.PluginPackage)
        };
        foreach (var module in modules)
        {
            var assemblyPath = module.Value.Assembly.Location;
            var fileName = Path.GetFileName(assemblyPath);
            var directory = CreateConfiguredDirectory(module.Key, $"{{\"fileName\":\"{fileName}\"}}");
            File.Copy(assemblyPath, Path.Combine(directory, fileName));
        }

        var packages = new PluginPackageDiscovery().LoadPackages(runtimeDirectory);

        Assert.Equal(modules.Keys.OrderBy(name => name), packages.Select(package => package.PackageName).OrderBy(name => name));
    }

    [Fact]
    public async Task Runtime_from_directory_loads_initializes_and_dispatches_fixed_entry_package()
    {
        var assemblyPath = typeof(PluginPackageDiscoveryTests).Assembly.Location;
        var fileName = Path.GetFileName(assemblyPath);
        var directory = CreateConfiguredDirectory("Element", $"{{\"fileName\":\"{fileName}\"}}");
        File.Copy(assemblyPath, Path.Combine(directory, fileName));

        var runtime = AsyncPluginRuntime.FromDirectory(runtimeDirectory);
        await runtime.InitAsync();

        Assert.Equal("loaded", await runtime.RunAsync("Element.Capture", "Excute", "{}"));
    }

    public void Dispose()
    {
        if (Directory.Exists(runtimeDirectory)) Directory.Delete(runtimeDirectory, true);
    }

    private void CreatePackage(string relativeDirectory, string fileName)
    {
        var directory = CreateConfiguredDirectory(relativeDirectory, $"{{\"fileName\":\"{fileName}\"}}");
        File.WriteAllBytes(Path.Combine(directory, fileName), Array.Empty<byte>());
    }

    private string CreateConfiguredDirectory(string relativeDirectory, string config)
    {
        var directory = Path.Combine(runtimeDirectory, relativeDirectory);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "config.json"), config);
        return directory;
    }
}
