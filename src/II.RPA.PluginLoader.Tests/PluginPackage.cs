using II.RPA.Plugin.Abstractions;

namespace II.RPA.PluginLoader.Tests;

public sealed class PluginPackage : IPluginPackage
{
    public string PackageName => "Element";

    public IReadOnlyCollection<string> RoutePackageNames => new[] { PackageName };

    public Task InitializeAsync(IPluginInvoker invoker) => Task.CompletedTask;

    public Task<string> RunAsync(PluginExecutionContext context) => Task.FromResult("loaded");
}
