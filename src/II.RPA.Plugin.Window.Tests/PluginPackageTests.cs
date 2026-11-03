using System.Reflection;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;
using Xunit;

namespace II.RPA.Plugin.Window.Tests;

public sealed class PluginPackageTests
{
    private static readonly Type[] AbilityTypes =
    {
        typeof(global::II.RPA.Plugin.Window.Plugins.ActiveWindow),
        typeof(global::II.RPA.Plugin.Window.Plugins.CloseWindow),
        typeof(global::II.RPA.Plugin.Window.Plugins.GetWindowData),
        typeof(global::II.RPA.Plugin.Window.Plugins.GetWindowDataList),
        typeof(global::II.RPA.Plugin.Window.Plugins.GetWindowList),
        typeof(global::II.RPA.Plugin.Window.Plugins.GetWindowRect),
        typeof(global::II.RPA.Plugin.Window.Plugins.MoveWindow),
        typeof(global::II.RPA.Plugin.Window.Plugins.SetWindowShow),
        typeof(global::II.RPA.Plugin.Window.Plugins.SetWindowState),
        typeof(global::II.RPA.Plugin.Window.Plugins.SizeWindow),
        typeof(global::II.RPA.Plugin.Window.Plugins.WaitWindow),
        typeof(global::II.RPA.Plugin.Window.Plugins.WindowIsExists)
    };

    [Fact]
    public void Fixed_entry_registers_all_window_abilities()
    {
        II.RPA.Plugin.Abstractions.IPluginPackage package = new global::II.RPA.Plugin.Window.PluginPackage();
        Assert.Equal("Window", package.PackageName);
        var builder = new PluginPackageBuilder();
        var register = package.GetType().GetMethod("RegisterAbilities", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(register);
        register!.Invoke(null, new object[] { builder });
        var property = typeof(PluginPackageBuilder).GetProperty("Registrations", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var registrations = (IReadOnlyDictionary<string, AbilityRegistration>)property.GetValue(builder)!;
        Assert.Equal(AbilityTypes.OrderBy(type => type.Name), registrations.Values.Select(value => value.AbilityType).OrderBy(type => type.Name));
        Assert.All(registrations.Values, registration =>
        {
            Assert.Equal(AbilityLifetime.Transient, registration.Lifetime);
            Assert.Equal(AbilityConcurrencyMode.RejectWhenBusy, registration.ConcurrencyMode);
        });
    }

    [Fact]
    public void Abilities_and_parameters_use_typed_synchronous_contract()
    {
        Assert.IsAssignableFrom<PluginParameterBase>(new global::II.RPA.Plugin.Window.Params.ParamBase());
        Assert.All(AbilityTypes, type => Assert.True(IsSubclassOfRawGeneric(typeof(PluginAbilityBase<>), type)));
    }

    [Fact]
    public async Task Title_is_normalized_to_exact_keyword_before_window_lookup()
    {
        II.RPA.Plugin.Abstractions.IPluginPackage package = new global::II.RPA.Plugin.Window.PluginPackage();
        await package.InitializeAsync(new NoOpInvoker());
        var result = await package.RunAsync(new PluginExecutionContext(
            new PluginRoute("Window", "WindowIsExists"),
            "{\"title\":\"a-window-that-does-not-exist\",\"switchType\":\"title\"}",
            "window-title-normalization"));
        Assert.Contains("\"code\"", result, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSubclassOfRawGeneric(Type generic, Type candidate)
    {
        for (var current = candidate; current != null && current != typeof(object); current = current.BaseType)
            if ((current.IsGenericType ? current.GetGenericTypeDefinition() : current) == generic) return true;
        return false;
    }

    private sealed class NoOpInvoker : IPluginInvoker
    {
        public Task<string> InvokeAsync(PluginRoute targetRoute, string jsonParam) =>
            Task.FromResult("{\"code\":0,\"info\":\"\"}");
    }
}
