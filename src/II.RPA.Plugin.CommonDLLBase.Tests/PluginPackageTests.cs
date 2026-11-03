using II.RPA.Plugin.Abstractions;
using Xunit;

namespace II.RPA.Plugin.CommonDLLBase.Tests;

public sealed class PluginPackageTests
{
    [Fact]
    public async Task Fixed_entry_is_async_CommonDLLBase_package_and_initializes_without_abilities()
    {
        IPluginPackage package = new II.RPA.Plugin.CommonDLLBase.PluginPackage();

        Assert.Equal("CommonDLLBase", package.PackageName);
        await package.InitializeAsync(new NullInvoker());

        var result = await package.RunAsync(new PluginExecutionContext(
            new PluginRoute("CommonDLLBase", "Missing"),
            "{}",
            Guid.NewGuid().ToString("N")));
        Assert.NotNull(result);
    }

    [Fact]
    public void Production_package_no_longer_references_InstanceManager()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "II.RPA.Plugin.CommonDLLBase",
            "PluginPackage.cs"));

        Assert.DoesNotContain("InstanceManager", source, StringComparison.Ordinal);
        Assert.Contains("Assembly.LoadFrom", source, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, ".git"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }

    private sealed class NullInvoker : IPluginInvoker
    {
        public Task<string> InvokeAsync(PluginRoute targetRoute, string jsonParam) => Task.FromResult("{}");
    }
}
