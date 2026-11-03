using System.Reflection;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;
using Xunit;

namespace II.RPA.Plugin.Dialog.Tests;

public sealed class PluginPackageTests
{
    private static readonly string[] AbilityNames = { "ConfigDialog", "ConfigInfo", "ConfigInput" };

    [Fact]
    public void Fixed_entry_registers_existing_dialog_abilities()
    {
        II.RPA.Plugin.Abstractions.IPluginPackage package = new global::II.RPA.Plugin.Dialog.PluginPackage();
        Assert.Equal("II.RPA.Plugin.Dialog.PluginPackage", package.GetType().FullName);
        Assert.Equal("Dialog", package.PackageName);

        var builder = new PluginPackageBuilder();
        package.GetType().GetMethod("RegisterAbilities", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { builder });
        var registrations = (IReadOnlyDictionary<string, AbilityRegistration>)typeof(PluginPackageBuilder)
            .GetProperty("Registrations", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(builder)!;

        Assert.Equal(AbilityNames, registrations.Keys.OrderBy(name => name));
        Assert.All(registrations.Values, registration =>
        {
            Assert.Equal(AbilityLifetime.Transient, registration.Lifetime);
            Assert.Equal(AbilityConcurrencyMode.RejectWhenBusy, registration.ConcurrencyMode);
            Assert.True(typeof(IPluginAbility).IsAssignableFrom(registration.AbilityType));
        });
    }

    [Fact]
    public void Dialog_entry_parameters_use_shared_parameter_contract()
    {
        Type[] parameterTypes =
        {
            typeof(global::II.RPA.Plugin.Dialog.Params.ParamConfigDialogModel),
            typeof(global::II.RPA.Plugin.Dialog.Params.ParamInfoDialogModel)
        };

        Assert.All(parameterTypes, type =>
            Assert.True(typeof(PluginParameterBase).IsAssignableFrom(type), type.FullName));
    }

    [Fact]
    public async Task Invalid_config_dialog_parameter_returns_existing_json_error_contract()
    {
        II.RPA.Plugin.Abstractions.IPluginPackage package = new global::II.RPA.Plugin.Dialog.PluginPackage();
        await package.InitializeAsync(new NoOpInvoker());

        var result = await package.RunAsync(new PluginExecutionContext(
            new PluginRoute("Dialog", "ConfigDialog"),
            "{invalid-json",
            "dialog-invalid"));

        Assert.Contains("\"code\"", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"code\":0", result, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class NoOpInvoker : IPluginInvoker
    {
        public Task<string> InvokeAsync(PluginRoute targetRoute, string jsonParam) =>
            Task.FromResult("{\"code\":0}");
    }
}
