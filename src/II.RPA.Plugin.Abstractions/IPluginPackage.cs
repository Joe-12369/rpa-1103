namespace II.RPA.Plugin.Abstractions;

public interface IPluginPackage
{
    string PackageName { get; }

    IReadOnlyCollection<string> RoutePackageNames { get; }

    Task InitializeAsync(IPluginInvoker pluginInvoker);

    Task<string> RunAsync(PluginExecutionContext context);
}
