using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core;
using II.RPA.PluginLoader;
using Xunit;

namespace II.RPA.PluginLoader.Tests;

public sealed class PluginLoaderFacadeTests
{
    [Fact]
    public void Public_execution_api_exposes_only_async_init_and_run()
    {
        var publicMethods = typeof(global::II.RPA.PluginLoader.PluginLoader)
            .GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
            .Select(method => method.Name)
            .ToArray();

        Assert.Contains("InitAsync", publicMethods);
        Assert.Contains("RunAsync", publicMethods);
        Assert.DoesNotContain("Init", publicMethods);
        Assert.DoesNotContain("Run", publicMethods);
    }

    [Fact]
    public async Task RunAsync_dispatches_through_injected_async_runtime()
    {
        var package = new StubPackage();
        var runtime = new AsyncPluginRuntime(new[] { package });
        await runtime.InitAsync();
        var loader = new global::II.RPA.PluginLoader.PluginLoader(runtime);

        var result = await loader.RunAsync("Database.Query", "Excute", "{\"value\":1}");

        Assert.Equal("runtime-result", result);
        Assert.Equal("Database.Query", package.Context?.Route.ToString());
        Assert.Equal("{\"value\":1}", package.Context?.JsonParam);
    }

    [Fact]
    public async Task InitAsync_sets_runtime_plugin_path_before_package_initialization()
    {
        var runtime = new AsyncPluginRuntime(Array.Empty<IPluginPackage>());
        var loader = new global::II.RPA.PluginLoader.PluginLoader(runtime);
        GlobleConfigurations.PluginPath = null;

        await loader.InitAsync();

        Assert.Equal(
            Path.Combine(
                Directory.GetParent(typeof(global::II.RPA.PluginLoader.PluginLoader).Assembly.Location)!.FullName,
                "runtime"),
            GlobleConfigurations.PluginPath);
    }

    private sealed class StubPackage : IPluginPackage
    {
        public string PackageName => "Database";
        public IReadOnlyCollection<string> RoutePackageNames => new[] { PackageName };
        public PluginExecutionContext? Context { get; private set; }

        public Task InitializeAsync(IPluginInvoker invoker) => Task.CompletedTask;

        public Task<string> RunAsync(PluginExecutionContext context)
        {
            Context = context;
            return Task.FromResult("runtime-result");
        }
    }
}
