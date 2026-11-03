using System.Reflection;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;
using Xunit;

namespace II.RPA.Plugin.System.Tests;

public sealed class PluginPackageTests
{
    private static readonly string[] AbilityNames =
    {
        "AddCredential", "CheckScreenState", "CpCheckPlug", "DeleteCredential", "GetApplicationPath",
        "HIDDriveCheck", "HIDDrivePreCheck", "LocalRemoteConnection", "LocalRemoteOff", "RDPLock",
        "RDPUnlock", "ReadCredential", "RemoteCancel", "RemoteUnLock"
    };

    [Fact]
    public void Fixed_entry_registers_existing_system_abilities()
    {
        II.RPA.Plugin.Abstractions.IPluginPackage package = new global::II.RPA.Plugin.RPASystem.PluginPackage();
        Assert.Equal("RPASystem", package.PackageName);
        var builder = new PluginPackageBuilder();
        package.GetType().GetMethod("RegisterAbilities", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { builder });
        var registrations = (IReadOnlyDictionary<string, AbilityRegistration>)typeof(PluginPackageBuilder)
            .GetProperty("Registrations", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(builder)!;
        Assert.Equal(AbilityNames.OrderBy(name => name), registrations.Keys.OrderBy(name => name));
        Assert.All(registrations.Values, registration =>
        {
            Assert.Equal(AbilityLifetime.Transient, registration.Lifetime);
            Assert.Equal(AbilityConcurrencyMode.RejectWhenBusy, registration.ConcurrencyMode);
            Assert.True(IsSubclassOfRawGeneric(typeof(PluginAbilityBase<>), registration.AbilityType));
        });
    }

    [Fact]
    public void Every_system_parameter_uses_shared_parameter_contract()
    {
        var parameterTypes = typeof(global::II.RPA.Plugin.RPASystem.Params.ParamCredential).Assembly.GetTypes()
            .Where(type => type.IsClass && type.Namespace == "II.RPA.Plugin.RPASystem.Params" && type.Name.StartsWith("Param", StringComparison.Ordinal));
        Assert.NotEmpty(parameterTypes);
        Assert.All(parameterTypes, type => Assert.True(typeof(PluginParameterBase).IsAssignableFrom(type), type.FullName));
    }

    [Fact]
    public async Task Invalid_add_credential_parameter_returns_existing_json_error_contract()
    {
        II.RPA.Plugin.Abstractions.IPluginPackage package = new global::II.RPA.Plugin.RPASystem.PluginPackage();
        await package.InitializeAsync(new NoOpInvoker());
        var result = await package.RunAsync(new PluginExecutionContext(new PluginRoute("RPASystem", "AddCredential"), "{}", "system-invalid"));
        Assert.Contains("\"code\"", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"code\":0", result, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSubclassOfRawGeneric(Type generic, Type candidate)
    {
        for (var current = candidate; current != null && current != typeof(object); current = current.BaseType)
            if ((current.IsGenericType ? current.GetGenericTypeDefinition() : current) == generic) return true;
        return false;
    }

    private sealed class NoOpInvoker : IPluginInvoker
    {
        public Task<string> InvokeAsync(PluginRoute targetRoute, string jsonParam) => Task.FromResult("{\"code\":0}");
    }
}
