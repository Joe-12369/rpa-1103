using Xunit;

namespace II.RPA.Plugin.Core.Tests;

public sealed class LegacyPluginRuntimeRemovalTests
{
    private static readonly string[] RemovedTypeNames =
    {
        "II.RPA.Plugin.Core.InstanceManager",
        "II.RPA.Plugin.Core.ActionType",
        "II.RPA.Plugin.Core.PluginBase.AbilityBase`1",
        "II.RPA.Plugin.Core.ComponentBase`1",
        "II.RPA.Plugin.Core.IPlugin",
        "II.RPA.Plugin.Core.PluginBase.IPluginPackage"
    };

    [Fact]
    public void Legacy_plugin_runtime_types_are_not_exposed_by_core()
    {
        var coreAssembly = typeof(global::II.RPA.Plugin.Core.PluginBase.PluginPackageBase).Assembly;

        Assert.All(RemovedTypeNames, typeName => Assert.Null(coreAssembly.GetType(typeName)));
    }
}
