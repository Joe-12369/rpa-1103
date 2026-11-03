using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.GUI.Commons;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace II.RPA.Plugin.GUI.Commons.Tests;

public sealed class GuiModuleBuilderTests
{
    [Fact]
    public void Module_registers_shared_operator_and_runtime_node_factories_in_host_collection()
    {
        var services = new ServiceCollection();
        var builder = new GuiModuleBuilder(services);

        new TestGuiModule().Configure(builder);
        using var provider = services.BuildServiceProvider();

        Assert.Same(provider.GetRequiredService<IGuiOperator>(), provider.GetRequiredService<ITestOperator>());
        Assert.NotNull(provider.GetRequiredService<IGuiNodeFactory>());
    }

    [Fact]
    public void Fixed_CV_and_Windows_modules_register_distinct_factories_and_shared_specialized_operators()
    {
        var services = new ServiceCollection();
        new II.RPA.Plugin.CV.GuiModule().Configure(new GuiModuleBuilder(services));
        new II.RPA.Plugin.Windows.GuiModule().Configure(new GuiModuleBuilder(services));
        using var provider = services.BuildServiceProvider();

        var cvOperator = provider.GetRequiredService<II.RPA.Plugin.GUI.Commons.IGuiCVOperator650>();
        Assert.Same(cvOperator, provider.GetServices<IGuiOperator>().Single(value => value.GuiType == II.RPA.WinAutomation.CommonUtility.eGuiType.CV));
        Assert.NotNull(provider.GetRequiredService<ICVGuiNodeFactory>());
        Assert.NotNull(provider.GetRequiredService<IWindowsGuiNodeFactory>());
    }

    [Fact]
    public void Loader_uses_configured_logical_directories_and_exact_GuiModule_entries()
    {
        var root = Path.Combine(Path.GetTempPath(), "II.RPA.GuiModules", Guid.NewGuid().ToString("N"));
        try
        {
            CopyModule(root, "GUICV", typeof(II.RPA.Plugin.CV.GuiModule).Assembly.Location);
            CopyModule(root, "GUIWindows", typeof(II.RPA.Plugin.Windows.GuiModule).Assembly.Location);
            var services = new ServiceCollection();

            new GuiModuleLoader().ConfigureModules(root, new[] { "GUICV", "GUIWindows" }, new GuiModuleBuilder(services));
            using var provider = services.BuildServiceProvider();

            Assert.Equal(2, provider.GetServices<IGuiOperator>().Count());
            Assert.NotNull(provider.GetRequiredService<ICVGuiNodeFactory>());
            Assert.NotNull(provider.GetRequiredService<IWindowsGuiNodeFactory>());
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Every_configured_non_web_operator_module_has_a_fixed_entry()
    {
        IGuiModule[] modules =
        {
            new II.RPA.Plugin.Java.GuiModule(),
            new II.RPA.Plugin.JavaAlter.GuiModule(),
            new II.RPA.Plugin.SAP.GuiModule(),
            new II.RPA.Plugin.WdtErp.GuiModule()
        };
        var services = new ServiceCollection();
        foreach (var module in modules) module.Configure(new GuiModuleBuilder(services));
        using var provider = services.BuildServiceProvider();
        Assert.Equal(4, provider.GetServices<IGuiOperator>().Count());
    }

    [Fact]
    public async Task Every_non_web_operator_module_also_has_an_empty_fixed_plugin_package_entry()
    {
        var packages = new Dictionary<string, IPluginPackage>
        {
            ["GUICV"] = new II.RPA.Plugin.CV.PluginPackage(),
            ["GUIJava"] = new II.RPA.Plugin.Java.PluginPackage(),
            ["GUIJavaAlter"] = new II.RPA.Plugin.JavaAlter.PluginPackage(),
            ["GUISAP"] = new II.RPA.Plugin.SAP.PluginPackage(),
            ["WdtErp"] = new II.RPA.Plugin.WdtErp.PluginPackage(),
            ["GUIWindows"] = new II.RPA.Plugin.Windows.PluginPackage()
        };

        Assert.All(packages, pair =>
        {
            Assert.Equal(pair.Key, pair.Value.PackageName);
            Assert.Equal(
                pair.Value.GetType().Assembly.GetName().Name + ".PluginPackage",
                pair.Value.GetType().FullName);
        });

        foreach (var pair in packages)
        {
            await pair.Value.InitializeAsync(new RecordingPluginInvoker());
            var result = await pair.Value.RunAsync(new PluginExecutionContext(
                new PluginRoute(pair.Key, "UnknownAbility"),
                "{}",
                "empty-package-entry"));
            Assert.DoesNotContain("\"code\":0", result, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void GuiRuntimeServices_exposes_sorted_operators_and_specialized_factories()
    {
        var services = new ServiceCollection();
        new II.RPA.Plugin.CV.GuiModule().Configure(new GuiModuleBuilder(services));
        new II.RPA.Plugin.Windows.GuiModule().Configure(new GuiModuleBuilder(services));
        var invoker = new RecordingPluginInvoker();
        services.AddSingleton<IPluginInvoker>(invoker);
        services.AddSingleton<GuiRuntimeServices>();
        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<GuiRuntimeServices>();

        Assert.Equal(2, runtime.Operators.Count);
        Assert.IsAssignableFrom<IGuiCVOperator650>(runtime.CVOperator);
        Assert.IsAssignableFrom<IGuiWindowsOperator>(runtime.WindowsOperator);
        Assert.Same(invoker, runtime.PluginInvoker);
        Assert.NotNull(runtime.CVNodeFactory);
        Assert.NotNull(runtime.WindowsNodeFactory);
    }

    [Fact]
    public void GuiRuntime_reuses_the_fixed_operator_instances_created_by_the_package_container()
    {
        var services = new ServiceCollection();
        new II.RPA.Plugin.CV.GuiModule().Configure(new GuiModuleBuilder(services));
        new II.RPA.Plugin.Windows.GuiModule().Configure(new GuiModuleBuilder(services));
        services.AddSingleton<IPluginInvoker>(new RecordingPluginInvoker());
        services.AddSingleton<GuiRuntimeServices>();
        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<GuiRuntimeServices>();

        GuiRuntime.Initialize(runtime);

        Assert.Same(runtime, GuiRuntime.Services);
        Assert.Same(runtime.WindowsOperator, GuiRuntime.WindowsOperator);
        Assert.Same(
            provider.GetRequiredService<II.RPA.Plugin.Windows.GuiOperator>(),
            GuiRuntime.WindowsOperator);
        var createWindowsOperator = typeof(II.RPA.Plugin.CV.GuiOperator).GetMethod(
            "CreateGuiWindowsOperator",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        Assert.Same(GuiRuntime.WindowsOperator, createWindowsOperator.Invoke(null, null));
    }

    [Fact]
    public async Task Plugin_action_helper_invokes_web_browser_route()
    {
        var invoker = new RecordingPluginInvoker();
        await PluginActionHelper.WebBrowserActiveAsync(invoker, "Chromium", "tab-data");

        Assert.Equal("WebBrowser.Active", invoker.Route!.ToString());
        Assert.Contains("Chromium", invoker.JsonParameter);
        Assert.Contains("tab-data", invoker.JsonParameter);
    }

    [Fact]
    public void Gui_runtime_services_synchronously_invokes_the_requested_plugin_route()
    {
        var services = new ServiceCollection();
        new II.RPA.Plugin.CV.GuiModule().Configure(new GuiModuleBuilder(services));
        new II.RPA.Plugin.Windows.GuiModule().Configure(new GuiModuleBuilder(services));
        var invoker = new RecordingPluginInvoker();
        services.AddSingleton<IPluginInvoker>(invoker);
        services.AddSingleton<GuiRuntimeServices>();
        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<GuiRuntimeServices>();

        var result = runtime.InvokePlugin(new PluginRoute("DesktopInput", "GetState"), string.Empty);

        Assert.Equal("{\"Code\":0}", result);
        Assert.Equal("DesktopInput.GetState", invoker.Route!.ToString());
        Assert.Equal(string.Empty, invoker.JsonParameter);
    }

    private static void CopyModule(string root, string logicalName, string sourceAssembly)
    {
        var directory = Directory.CreateDirectory(Path.Combine(root, logicalName)).FullName;
        var fileName = Path.GetFileName(sourceAssembly);
        File.Copy(sourceAssembly, Path.Combine(directory, fileName));
        File.WriteAllText(Path.Combine(directory, "config.json"), $"{{\"fileName\":\"{fileName}\"}}");
    }

    private interface ITestOperator { }

    private sealed class TestGuiModule : IGuiModule
    {
        public void Configure(GuiModuleBuilder builder)
        {
            builder.AddOperator<TestOperator, ITestOperator>();
            builder.AddNodeFactory<TestNodeFactory>();
        }
    }

    private sealed class TestOperator : IGuiOperator, ITestOperator
    {
        public int Sort { get; set; }
        public II.RPA.WinAutomation.CommonUtility.eGuiType GuiType => II.RPA.WinAutomation.CommonUtility.eGuiType.None;
        public void ReadyToCapture() { }
        public void Release() { }
        public bool IsCanUse(System.Drawing.Point screenPoint, AutomationElement rootElement, AutomationElement pointElement) => false;
        public bool InspectNode(System.Drawing.Point screenPoint, AutomationElement rootElement, AutomationElement pointElement, string selector, out IGuiNode inspectNode) { inspectNode = null!; return false; }
        public IGuiNode GetElementFromSelector(GetElementFromSelectorParams args) => null!;
        public string GetElementFromSelectorAll(GetElementFromSelectorParams args) => string.Empty;
    }

    private sealed class TestNodeFactory : IGuiNodeFactory
    {
        public IGuiNode Create(AutomationElement element) => null!;
    }

    private sealed class RecordingPluginInvoker : IPluginInvoker
    {
        public PluginRoute? Route { get; private set; }
        public string JsonParameter { get; private set; } = string.Empty;

        public Task<string> InvokeAsync(PluginRoute targetRoute, string jsonParam)
        {
            Route = targetRoute;
            JsonParameter = jsonParam;
            return Task.FromResult("{\"Code\":0}");
        }
    }
}
