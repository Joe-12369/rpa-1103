using System.Reflection;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;
using Xunit;

namespace II.RPA.Plugin.UsbHub.Tests;

public sealed class PluginPackageTests
{
    private static readonly Type[] AbilityTypes =
    {
        typeof(global::II.RPA.Plugin.UsbHub.Plugins.LocalClose),
        typeof(global::II.RPA.Plugin.UsbHub.Plugins.LocalGetComs),
        typeof(global::II.RPA.Plugin.UsbHub.Plugins.LocalOpen),
        typeof(global::II.RPA.Plugin.UsbHub.Plugins.NetworkClose),
        typeof(global::II.RPA.Plugin.UsbHub.Plugins.NetworkOpen)
    };

    private static readonly Type[] ParameterTypes =
    {
        typeof(global::II.RPA.Plugin.UsbHub.Params.ParamLocalCloseModel),
        typeof(global::II.RPA.Plugin.UsbHub.Params.ParamLocalGetComsModel),
        typeof(global::II.RPA.Plugin.UsbHub.Params.ParamLocalOpenModel),
        typeof(global::II.RPA.Plugin.UsbHub.Params.ParamNetworkCloseModel),
        typeof(global::II.RPA.Plugin.UsbHub.Params.ParamNetworkOpenModel)
    };

    [Fact]
    public void Fixed_entry_registers_all_usb_hub_abilities()
    {
        II.RPA.Plugin.Abstractions.IPluginPackage package = new global::II.RPA.Plugin.UsbHub.PluginPackage();
        Assert.Equal("UsbHub", package.PackageName);
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
    public void Abilities_and_parameters_use_synchronous_typed_contract_without_legacy_hooks()
    {
        Assert.All(ParameterTypes, type => Assert.True(typeof(PluginParameterBase).IsAssignableFrom(type)));
        Assert.All(AbilityTypes, type =>
        {
            Assert.True(IsSubclassOfRawGeneric(typeof(PluginAbilityBase<>), type));
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            Assert.DoesNotContain(methods, method => method.Name == "ParamVerification" || method.Name == "Excute" || method.Name == "Config");
        });
    }

    [Fact]
    public async Task Invalid_local_open_parameter_is_returned_as_existing_json_error_contract()
    {
        II.RPA.Plugin.Abstractions.IPluginPackage package = new global::II.RPA.Plugin.UsbHub.PluginPackage();
        await package.InitializeAsync(new NullInvoker());
        var result = await package.RunAsync(new PluginExecutionContext(
            new PluginRoute("UsbHub", "LocalOpen"),
            "{}",
            "usb-invalid-parameter"));
        Assert.Contains("\"code\"", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"code\":0", result, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSubclassOfRawGeneric(Type generic, Type candidate)
    {
        for (var current = candidate; current != null && current != typeof(object); current = current.BaseType)
        {
            if ((current.IsGenericType ? current.GetGenericTypeDefinition() : current) == generic) return true;
        }
        return false;
    }

    private sealed class NullInvoker : IPluginInvoker
    {
        public Task<string> InvokeAsync(PluginRoute targetRoute, string jsonParam) => Task.FromResult("{}");
    }
}
