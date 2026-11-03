using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core;
using II.RPA.WinAutomation.CommonUtility;
using Xunit;

namespace II.RPA.PluginLoader.Tests;

public sealed class PluginRuntimeTests
{
    [Fact]
    public async Task RunAsync_dispatches_to_registered_package()
    {
        var package = new RecordingPackage("Element", "result");
        var runtime = new AsyncPluginRuntime(new[] { package });
        await runtime.InitAsync();
        Assert.Equal("result", await runtime.RunAsync("Element.Capture", "Excute", "{}"));
        Assert.Equal("Element.Capture", package.Context!.Route.ToString());
    }

    [Fact]
    public async Task Invoker_creates_an_independent_execution_context_for_the_target()
    {
        var target = new RecordingPackage("Database", "target");
        var caller = new InvokingPackage("Element", new PluginRoute("Database", "Query"));
        var runtime = new AsyncPluginRuntime(new IPluginPackage[] { caller, target });
        await runtime.InitAsync();
        await runtime.RunAsync("Element.Capture", "Excute", "{}");
        Assert.False(string.IsNullOrWhiteSpace(target.Context!.InvocationId));
        Assert.NotEqual(caller.Context!.InvocationId, target.Context.InvocationId);
    }

    [Fact]
    public async Task InitAsync_propagates_failure_and_stays_uninitialized()
    {
        var runtime = new AsyncPluginRuntime(new[] { new RecordingPackage("Element", "", new InvalidOperationException("failed")) });
        await Assert.ThrowsAsync<InvalidOperationException>(() => runtime.InitAsync());
        Assert.False(runtime.IsInitialized);
    }

    [Fact]
    public async Task InitAsync_initializes_existing_base_packages_in_fixed_order_and_leaves_other_packages_lazy()
    {
        var order = new List<string>();
        var normal = new RecordingPackage("Database", "result", onInitialize: () => order.Add("Database"));
        var element = new RecordingPackage("Element", "result", onInitialize: () => order.Add("Element"));
        var browsers = new RecordingPackage("Browsers", "result", onInitialize: () => order.Add("Browsers"));
        var oldChrome = new RecordingPackage("GUIChrome", "result", onInitialize: () => order.Add("GUIChrome"));
        var oldChromium = new RecordingPackage("Chromium", "result", onInitialize: () => order.Add("Chromium"));
        var ie = new RecordingPackage("GUIIE", "result", onInitialize: () => order.Add("GUIIE"));
        var common = new RecordingPackage("CommonDLLBase", "result", onInitialize: () => order.Add("CommonDLLBase"));
        var runtime = new AsyncPluginRuntime(new IPluginPackage[] { normal, element, browsers, oldChrome, oldChromium, ie, common });

        await runtime.InitAsync();

        Assert.Equal(new[] { "CommonDLLBase", "Browsers", "GUIIE", "Element" }, order);
        Assert.Equal(0, oldChrome.InitializeCount);
        Assert.Equal(0, oldChromium.InitializeCount);
        Assert.Equal(0, normal.InitializeCount);
    }

    [Fact]
    public async Task RunAsync_initializes_normal_package_once_on_first_use()
    {
        var package = new RecordingPackage("Database", "result");
        var runtime = new AsyncPluginRuntime(new[] { package });
        await runtime.InitAsync();

        await runtime.RunAsync("Database.Query", "Excute", "{}");
        await runtime.RunAsync("Database.Query", "Excute", "{}");

        Assert.Equal(1, package.InitializeCount);
    }

    [Fact]
    public async Task Route_domains_target_one_package_and_preserve_original_route()
    {
        var package = new MultiRouteRecordingPackage(new[] { "Mouse", "InputMethod" });
        var runtime = new AsyncPluginRuntime(new IPluginPackage[] { package });
        await runtime.InitAsync();

        Assert.Equal("Mouse.Move", await runtime.RunAsync("Mouse.Move", "Excute", "{}"));
        Assert.Equal("InputMethod.GetState", await runtime.RunAsync("InputMethod.GetState", "Excute", "{}"));
        Assert.Equal(1, package.InitializeCount);
    }

    [Fact]
    public void Route_domains_cannot_shadow_another_real_package()
    {
        Assert.Throws<ArgumentException>(() => new AsyncPluginRuntime(new IPluginPackage[]
        {
            new MultiRouteRecordingPackage(new[] { "Mouse", "InputMethod" }), new RecordingPackage("Mouse", "other")
        }));
    }

    [Fact]
    public void Route_domains_must_be_nonempty_unique_and_valid()
    {
        Assert.Throws<ArgumentException>(() => new AsyncPluginRuntime(new IPluginPackage[] { new MultiRouteRecordingPackage(null) }));
        Assert.Throws<ArgumentException>(() => new AsyncPluginRuntime(new IPluginPackage[] { new MultiRouteRecordingPackage(Array.Empty<string>()) }));
        Assert.Throws<ArgumentException>(() => new AsyncPluginRuntime(new IPluginPackage[] { new MultiRouteRecordingPackage(new[] { "Mouse", "mouse" }) }));
        Assert.Throws<ArgumentException>(() => new AsyncPluginRuntime(new IPluginPackage[] { new MultiRouteRecordingPackage(new[] { "Mouse.Invalid" }) }));
    }

    [Fact]
    public async Task Lazy_initialization_failure_is_not_cached_and_next_run_retries()
    {
        var package = new RecordingPackage("Database", "result", failuresBeforeSuccess: 1);
        var runtime = new AsyncPluginRuntime(new[] { package });
        await runtime.InitAsync();

        var first = await runtime.RunAsync("Database.Query", "Excute", "{}");
        var second = await runtime.RunAsync("Database.Query", "Excute", "{}");

        Assert.NotEqual(0, first.ToObject<JMessage>().Code);
        Assert.Equal("result", second);
        Assert.Equal(2, package.InitializeCount);
    }

    [Fact]
    public async Task RunAsync_before_initialization_returns_uninitialized_error_without_dispatching()
    {
        var package = new RecordingPackage("Element", "result");
        var runtime = new AsyncPluginRuntime(new[] { package });

        var result = await runtime.RunAsync("Element.Capture", "Excute", "{}");

        Assert.Equal(
            new RPAException(eErrorMessageType.Error_PluginRun_ServerStatusUninit).ErrorCode,
            result.ToObject<JMessage>().Code);
        Assert.Null(package.Context);
    }

    [Theory]
    [InlineData("Config")]
    [InlineData("Other")]
    public async Task RunAsync_with_unsupported_action_returns_error_without_dispatching(string action)
    {
        var package = new RecordingPackage("Element", "result");
        var runtime = new AsyncPluginRuntime(new[] { package });
        await runtime.InitAsync();

        var result = await runtime.RunAsync("Element.Capture", action, "{}");

        Assert.NotEqual(0, result.ToObject<JMessage>().Code);
        Assert.Null(package.Context);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Excute")]
    [InlineData("eXcUtE")]
    public async Task RunAsync_accepts_compatible_action_values(string action)
    {
        var package = new RecordingPackage("Element", "result");
        var runtime = new AsyncPluginRuntime(new[] { package });
        await runtime.InitAsync();

        Assert.Equal("result", await runtime.RunAsync("Element.Capture", action, "{}"));
    }

    [Fact]
    public async Task RunAsync_for_unknown_package_returns_error_json()
    {
        var runtime = new AsyncPluginRuntime(Array.Empty<IPluginPackage>());
        await runtime.InitAsync();

        var result = await runtime.RunAsync("Missing.Capture", "Excute", "{}");

        Assert.Equal(
            new RPAException(eErrorMessageType.Error_PluginAbilityNotFoundError).ErrorCode,
            result.ToObject<JMessage>().Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" Element")]
    [InlineData("Element ")]
    [InlineData("Element.GUI")]
    public void Constructor_rejects_invalid_package_names(string packageName)
    {
        Assert.Throws<ArgumentException>(() =>
            new AsyncPluginRuntime(new[] { new RecordingPackage(packageName, "result") }));
    }

    private sealed class RecordingPackage : IPluginPackage
    {
        private readonly string result; private readonly Exception? error; private readonly Action? onInitialize; private int failuresBeforeSuccess;
        public RecordingPackage(string name, string result, Exception? error = null, Action? onInitialize = null, int failuresBeforeSuccess = 0) { PackageName = name; this.result = result; this.error = error; this.onInitialize = onInitialize; this.failuresBeforeSuccess = failuresBeforeSuccess; }
        public string PackageName { get; } public PluginExecutionContext? Context { get; private set; }
        public IReadOnlyCollection<string> RoutePackageNames => new[] { PackageName };
        public int InitializeCount { get; private set; }
        public Task InitializeAsync(IPluginInvoker invoker) { InitializeCount++; onInitialize?.Invoke(); if (failuresBeforeSuccess-- > 0) return Task.FromException(new InvalidOperationException("failed")); return error == null ? Task.CompletedTask : Task.FromException(error); }
        public Task<string> RunAsync(PluginExecutionContext context) { Context = context; return Task.FromResult(result); }
    }

    private sealed class MultiRouteRecordingPackage : IPluginPackage
    {
        private readonly IReadOnlyCollection<string> _routePackageNames;
        public MultiRouteRecordingPackage(IReadOnlyCollection<string> routePackageNames) => _routePackageNames = routePackageNames;
        public string PackageName => "DesktopInput";
        public IReadOnlyCollection<string> RoutePackageNames => _routePackageNames;
        public int InitializeCount { get; private set; }
        public Task InitializeAsync(IPluginInvoker invoker) { InitializeCount++; return Task.CompletedTask; }
        public Task<string> RunAsync(PluginExecutionContext context) => Task.FromResult(context.Route.ToString());
    }

    private sealed class InvokingPackage : IPluginPackage
    {
        private readonly PluginRoute target; private IPluginInvoker? invoker;
        public InvokingPackage(string name, PluginRoute target) { PackageName = name; this.target = target; }
        public string PackageName { get; } public PluginExecutionContext? Context { get; private set; }
        public IReadOnlyCollection<string> RoutePackageNames => new[] { PackageName };
        public Task InitializeAsync(IPluginInvoker value) { invoker = value; return Task.CompletedTask; }
        public async Task<string> RunAsync(PluginExecutionContext context) { Context = context; return await invoker!.InvokeAsync(target, "{}"); }
    }
}
